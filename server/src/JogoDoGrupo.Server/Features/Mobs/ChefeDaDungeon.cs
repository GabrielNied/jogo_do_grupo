using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// O boss da primeira dungeon, com a IA autoritativa (docs/features/mobs.md, "Boss da dungeon").
/// Força fixa: a mesma vida e o mesmo dano para 1 a 5 jogadores. Repete, na ordem de
/// <see cref="ConfigChefeDaDungeon.OrdemDosAtaques"/>, três ataques telegrafados (investida,
/// pisada e raios), cada um com o aviso de <see cref="ConfigChefeDaDungeon.AvisoDoAtaqueSegundos"/>:
/// as áreas ficam fixas desde o aviso, e só quem estiver dentro de uma delas no momento do acerto
/// leva o dano (<see cref="GolpeDeInimigoAcertou"/>). Depois de certos ataques abre uma janela
/// exposta, em que o dano que recebe é multiplicado. Ao morrer publica um
/// <see cref="InimigoDerrotado"/> e <b>não volta sozinho</b>: só <see cref="Reiniciar"/> (ou uma
/// instância nova, por <see cref="IChefesDaDungeon"/>) o traz de novo.
/// <para>
/// Sem rede: <see cref="Passo"/> devolve o que mudou e o <see cref="ServicoDeInimigos"/> envia e
/// publica. É também o alvo dos golpes dos jogadores. Seguro para várias threads.
/// </para>
/// </summary>
public sealed class ChefeDaDungeon : IAlvosDeCombate, IInimigosSimulados
{
    private static readonly long AlertaMs = EmMs(ConfigChefeDaDungeon.AlertaSegundos);
    private static readonly long AvisoMs = EmMs(ConfigChefeDaDungeon.AvisoDoAtaqueSegundos);
    private static readonly long PausaMs = EmMs(ConfigChefeDaDungeon.PausaEntreAtaquesSegundos);
    private static readonly long JanelaExpostaMs = EmMs(
        ConfigChefeDaDungeon.DuracaoDaJanelaExpostaSegundos
    );

    /// <summary>Folga, em metros, para dar a volta à origem por chegada.</summary>
    private const double ToleranciaDeChegadaMetros = 0.1;

    private const double PassoMaximoSegundos = 0.5;

    private enum Fase
    {
        Pausa,
        Aviso,
        Recuperacao,
    }

    private readonly record struct AreaDoChefe(double X, double Z, double Raio);

    private readonly Lock _trava = new();
    private readonly DefinicaoDeCriatura _definicao = CatalogoCriaturas.Obter(
        ConfigChefeDaDungeon.CriaturaId
    );
    private readonly Func<long> _proximoId;
    private readonly Func<long> _relogioMs;
    private readonly double _origemX;
    private readonly double _origemZ;
    private readonly HashSet<long> _atacantes = [];
    private readonly List<AreaDoChefe> _areas = [];
    private readonly TabelaDeAmeaca _ameacas = new();

    private PassoDosInimigos _pendente = new();
    private long _id;
    private double _x;
    private double _z;
    private double _rotacao = 180.0;
    private EstadoDeInimigo _estado = EstadoDeInimigo.Repouso;
    private long _alvo = RegrasInimigo.SemAlvo;
    private int _vida;
    private int _sequencia;
    private bool _vidaMudou;
    private long _alertaAteMs;
    private Fase _fase = Fase.Pausa;
    private long _faseAteMs;
    private long _proximoAtaqueMs;
    private TipoDeAtaqueDeInimigo _tipo = TipoDeAtaqueDeInimigo.Golpe;
    private int _indiceDoAtaque;
    private int _rodizioDosRaios;
    private long _expostoAteMs;
    private long? _ultimoPassoMs;
    private long _proximaMorte = 1;

    /// <param name="area">Área do mundo em que o boss vive.</param>
    /// <param name="origemX">Onde o boss fica, no plano do chão.</param>
    /// <param name="origemZ">Onde o boss fica, no plano do chão.</param>
    /// <param name="proximoId">Dá o id do boss, e um novo a cada <see cref="Reiniciar"/>.</param>
    /// <param name="relogioMs">Relógio em milissegundos; os testes trocam por um controlado.</param>
    public ChefeDaDungeon(
        string area,
        double origemX,
        double origemZ,
        Func<long> proximoId,
        Func<long>? relogioMs = null
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(area);
        ArgumentNullException.ThrowIfNull(proximoId);

        Area = area;
        _origemX = origemX;
        _origemZ = origemZ;
        _proximoId = proximoId;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
        _x = origemX;
        _z = origemZ;
        _id = proximoId();
        _vida = ConfigChefeDaDungeon.VidaMaxima;
    }

    public string Area { get; }

    /// <summary>Id do boss agora (muda a cada <see cref="Reiniciar"/>).</summary>
    public long InimigoId
    {
        get
        {
            lock (_trava)
            {
                return _id;
            }
        }
    }

    /// <summary>Se o boss está morto.</summary>
    public bool Morto
    {
        get
        {
            lock (_trava)
            {
                return _estado == EstadoDeInimigo.Morto;
            }
        }
    }

    /// <summary>Se a janela exposta está aberta agora.</summary>
    public bool Exposto
    {
        get
        {
            lock (_trava)
            {
                return EstaExposto(_relogioMs());
            }
        }
    }

    public InimigoNoMundo[] Vivos()
    {
        lock (_trava)
        {
            return RegrasInimigo.EstaVivo(_estado) ? [Retrato()] : [];
        }
    }

    /// <summary>
    /// Traz o boss de volta: vida cheia, na origem, com um id novo, e avisa a área
    /// (<see cref="InimigoApareceu"/>). Devolve o id novo. Quem estava na luta é esquecido.
    /// </summary>
    public long Reiniciar()
    {
        lock (_trava)
        {
            _id = _proximoId();
            _x = _origemX;
            _z = _origemZ;
            _rotacao = 180.0;
            _estado = EstadoDeInimigo.Repouso;
            _alvo = RegrasInimigo.SemAlvo;
            _vida = ConfigChefeDaDungeon.VidaMaxima;
            _sequencia = 0;
            _vidaMudou = false;
            _fase = Fase.Pausa;
            _areas.Clear();
            _indiceDoAtaque = 0;
            _rodizioDosRaios = 0;
            _expostoAteMs = 0;
            _atacantes.Clear();
            _pendente.Aparicoes.Add(new InimigoApareceu { Inimigo = Retrato() });
            return _id;
        }
    }

    public PassoDosInimigos Passo(IReadOnlyList<JogadorNoCampo> jogadores)
    {
        ArgumentNullException.ThrowIfNull(jogadores);
        lock (_trava)
        {
            var agora = _relogioMs();
            var passo = _pendente;
            _pendente = new PassoDosInimigos();
            var segundos = _ultimoPassoMs is { } ultimo
                ? Math.Clamp((agora - ultimo) / 1000.0, 0.0, PassoMaximoSegundos)
                : 0.0;
            _ultimoPassoMs = agora;

            if (_estado == EstadoDeInimigo.Morto)
            {
                return passo;
            }

            if (_expostoAteMs != 0 && agora >= _expostoAteMs)
            {
                _expostoAteMs = 0;
            }

            var presentes = jogadores
                .Where(j => j.PersonagemId != RegrasInimigo.SemAlvo)
                .GroupBy(j => j.PersonagemId)
                .Select(g => g.Last())
                .OrderBy(j => j.PersonagemId)
                .ToArray();
            var engajados = presentes
                .Where(j =>
                    RegrasInimigo.Distancia(_origemX, _origemZ, j.X, j.Z)
                    <= ConfigChefeDaDungeon.DistanciaDeDespertarMetros
                )
                .ToArray();

            foreach (var id in _ameacas.Personagens.ToArray())
            {
                if (!presentes.Any(j => j.PersonagemId == id))
                {
                    _ameacas.Remover(id);
                }
            }

            var antes = (_x, _z, _rotacao, _estado, _alvo);
            switch (_estado)
            {
                case EstadoDeInimigo.Repouso:
                    if (engajados.Length > 0)
                    {
                        _alvo = EscolherAlvo(engajados, agora);
                        _alertaAteMs = agora + AlertaMs;
                        Mudar(EstadoDeInimigo.Alerta);
                        OlharPara(engajados);
                    }

                    break;

                case EstadoDeInimigo.Alerta:
                    if (engajados.Length == 0)
                    {
                        Retornar();
                        break;
                    }

                    OlharPara(engajados);
                    if (agora >= _alertaAteMs)
                    {
                        _proximoAtaqueMs = agora;
                        _fase = Fase.Pausa;
                        Mudar(EstadoDeInimigo.Perseguicao);
                    }

                    break;

                case EstadoDeInimigo.Perseguicao:
                    // Em combate, entre um ataque e outro: ele não persegue, espera a pausa.
                    if (engajados.Length == 0)
                    {
                        Retornar();
                        break;
                    }

                    OlharPara(engajados);
                    if (agora >= _proximoAtaqueMs)
                    {
                        ComecarAtaque(agora, engajados, passo);
                    }

                    break;

                case EstadoDeInimigo.Ataque:
                    Atacar(agora, presentes, passo);
                    break;

                case EstadoDeInimigo.Retorno:
                    Voltar(segundos);
                    break;
            }

            var mudouPosicao = antes._x != _x || antes._z != _z || antes._rotacao != _rotacao;
            var mudouOResto = antes._estado != _estado || antes._alvo != _alvo || _vidaMudou;
            _vidaMudou = false;
            if (mudouPosicao || mudouOResto)
            {
                _sequencia++;
                passo.Mudancas.Add(new MudancaDeInimigo(Mudanca(), !mudouOResto));
            }

            return passo;
        }
    }

    public IReadOnlyList<AlvoDeCombate> AlvosNoVolume(
        string area,
        double origemX,
        double origemZ,
        double direcaoYGraus,
        long instanteReferenciaMs = 0
    )
    {
        if (area != Area)
        {
            return [];
        }

        lock (_trava)
        {
            if (
                RegrasInimigo.EstaVivo(_estado)
                && RegrasAtaque.EstaNoVolume(
                    origemX,
                    origemZ,
                    direcaoYGraus,
                    _x,
                    _z,
                    ConfigChefeDaDungeon.RaioDoCorpoMetros
                )
            )
            {
                return
                [
                    new AlvoDeCombate(
                        _id,
                        _x,
                        _z,
                        ConfigChefeDaDungeon.RaioDoCorpoMetros,
                        _vida,
                        ConfigChefeDaDungeon.VidaMaxima
                    ),
                ];
            }

            return [];
        }
    }

    public IReadOnlyList<AlvoDeCombate> AlvosProximos(
        string area,
        double origemX,
        double origemZ,
        double alcanceMetros,
        long instanteReferenciaMs = 0
    )
    {
        if (area != Area)
        {
            return [];
        }

        lock (_trava)
        {
            var dx = _x - origemX;
            var dz = _z - origemZ;
            if (
                RegrasInimigo.EstaVivo(_estado)
                && Math.Sqrt(dx * dx + dz * dz) - ConfigChefeDaDungeon.RaioDoCorpoMetros
                    <= alcanceMetros
            )
            {
                return
                [
                    new AlvoDeCombate(
                        _id,
                        _x,
                        _z,
                        ConfigChefeDaDungeon.RaioDoCorpoMetros,
                        _vida,
                        ConfigChefeDaDungeon.VidaMaxima
                    ),
                ];
            }

            return [];
        }
    }

    public bool TentarAplicarDano(
        long alvoId,
        int dano,
        long atacanteId,
        out ResultadoDeDano resultado
    )
    {
        resultado = default;
        if (dano <= 0)
        {
            return false;
        }

        lock (_trava)
        {
            if (alvoId != _id || !RegrasInimigo.EstaVivo(_estado))
            {
                return false;
            }

            var recebido = EstaExposto(_relogioMs())
                ? (int)
                    Math.Round(
                        dano * ConfigChefeDaDungeon.MultiplicadorDeDanoExposto,
                        MidpointRounding.AwayFromZero
                    )
                : dano;
            var aplicado = Math.Min(recebido, _vida);
            _vida -= aplicado;
            _atacantes.Add(atacanteId);
            _ameacas.AdicionarDano(atacanteId, aplicado);
            _vidaMudou = true;
            resultado = new ResultadoDeDano(_id, aplicado, _vida, ConfigChefeDaDungeon.VidaMaxima);
            if (_vida <= 0)
            {
                Matar();
            }

            return true;
        }
    }

    // Tudo daqui para baixo é chamado com a trava.

    private void ComecarAtaque(long agora, JogadorNoCampo[] engajados, PassoDosInimigos passo)
    {
        var ordem = ConfigChefeDaDungeon.OrdemDosAtaques;
        var tipo = ordem[_indiceDoAtaque % ordem.Length];
        _indiceDoAtaque++;

        var alvoId = EscolherAlvo(engajados, agora);
        var idx = Array.FindIndex(engajados, j => j.PersonagemId == alvoId);
        var alvo = idx >= 0 ? engajados[idx] : MaisProximo(engajados);
        _alvo = alvo.PersonagemId;
        OlharPara(engajados);

        _areas.Clear();
        switch (tipo)
        {
            case TipoDeAtaqueDeInimigo.Investida:
                // Vai até onde o alvo está agora, no máximo até o alcance. O ponto fica fixo.
                var distancia = RegrasInimigo.Distancia(_x, _z, alvo.X, alvo.Z);
                RegrasInimigo.Avancar(
                    _x,
                    _z,
                    alvo.X,
                    alvo.Z,
                    Math.Min(distancia, ConfigChefeDaDungeon.AlcanceDaInvestidaMetros),
                    1.0,
                    0.0,
                    out var destinoX,
                    out var destinoZ
                );
                _areas.Add(
                    new AreaDoChefe(destinoX, destinoZ, ConfigChefeDaDungeon.RaioDaInvestidaMetros)
                );
                break;

            case TipoDeAtaqueDeInimigo.Pisada:
                _areas.Add(new AreaDoChefe(_x, _z, ConfigChefeDaDungeon.RaioDaPisadaMetros));
                break;

            case TipoDeAtaqueDeInimigo.Raios:
                MarcarRaios(engajados);
                break;
        }

        _tipo = tipo;
        _fase = Fase.Aviso;
        _faseAteMs = agora + AvisoMs;
        Mudar(EstadoDeInimigo.Ataque);

        var primeira = _areas[0];
        passo.Preparos.Add(
            new InimigoPreparaAtaque
            {
                InimigoId = _id,
                PreparoMs = (int)AvisoMs,
                CentroX = (float)primeira.X,
                CentroZ = (float)primeira.Z,
                Raio = (float)primeira.Raio,
                DirecaoY = (float)_rotacao,
                Tipo = tipo,
                Areas = _areas
                    .Select(a => new AreaDeAtaque
                    {
                        CentroX = (float)a.X,
                        CentroZ = (float)a.Z,
                        Raio = (float)a.Raio,
                    })
                    .ToArray(),
            }
        );
    }

    // Um raio por jogador, em rodízio; se faltam jogadores, os raios que sobram caem em volta.
    private void MarcarRaios(JogadorNoCampo[] engajados)
    {
        var total = ConfigChefeDaDungeon.RaiosPorAtaque;
        for (var i = 0; i < total; i++)
        {
            var jogador = engajados[(_rodizioDosRaios + i) % engajados.Length];
            var x = jogador.X;
            var z = jogador.Z;
            if (i >= engajados.Length)
            {
                var angulo = 2.0 * Math.PI * i / total;
                x += Math.Sin(angulo) * ConfigChefeDaDungeon.DesvioDosRaiosExtrasMetros;
                z += Math.Cos(angulo) * ConfigChefeDaDungeon.DesvioDosRaiosExtrasMetros;
            }

            _areas.Add(new AreaDoChefe(x, z, ConfigChefeDaDungeon.RaioDoRaioMetros));
        }

        _rodizioDosRaios++;
    }

    // O ataque, uma vez começado, vai até o fim: aviso, acerto e recuperação.
    private void Atacar(long agora, JogadorNoCampo[] presentes, PassoDosInimigos passo)
    {
        if (agora < _faseAteMs)
        {
            return;
        }

        if (_fase == Fase.Aviso)
        {
            Acertar(agora, presentes, passo);
            return;
        }

        _fase = Fase.Pausa;
        _proximoAtaqueMs = agora + PausaMs;
        Mudar(EstadoDeInimigo.Perseguicao);
    }

    // O acerto: pega quem está numa das áreas agora, um evento por personagem.
    private void Acertar(long agora, JogadorNoCampo[] presentes, PassoDosInimigos passo)
    {
        var dano = ConfigChefeDaDungeon.DanoDe(_tipo);
        foreach (var jogador in presentes)
        {
            if (_areas.Any(a => RegrasInimigo.Distancia(a.X, a.Z, jogador.X, jogador.Z) <= a.Raio))
            {
                passo.Golpes.Add(
                    new GolpeDeInimigoAcertou(
                        _id,
                        _definicao.Id,
                        jogador.PersonagemId,
                        dano,
                        Area,
                        _tipo
                    )
                );
            }
        }

        if (_tipo == TipoDeAtaqueDeInimigo.Investida)
        {
            _x = _areas[0].X;
            _z = _areas[0].Z;
        }

        if (ConfigChefeDaDungeon.AtaquesQueExpoem.Contains(_tipo))
        {
            _expostoAteMs = agora + JanelaExpostaMs;
            passo.Expostos.Add(
                new InimigoExposto
                {
                    InimigoId = _id,
                    DuracaoMs = (int)JanelaExpostaMs,
                    Multiplicador = (float)ConfigChefeDaDungeon.MultiplicadorDeDanoExposto,
                }
            );
        }

        _fase = Fase.Recuperacao;
        _faseAteMs = agora + EmMs(ConfigChefeDaDungeon.RecuperacaoDe(_tipo));
    }

    // Sem ninguém na sala: larga a luta, recupera a vida e esquece quem participou.
    private void Retornar()
    {
        _alvo = RegrasInimigo.SemAlvo;
        _atacantes.Clear();
        _ameacas.Limpar();
        _expostoAteMs = 0;
        _areas.Clear();
        if (_vida != ConfigChefeDaDungeon.VidaMaxima)
        {
            _vida = ConfigChefeDaDungeon.VidaMaxima;
            _vidaMudou = true;
        }

        Mudar(EstadoDeInimigo.Retorno);
    }

    private void Voltar(double segundos)
    {
        _rotacao = RegrasInimigo.DirecaoPara(_x, _z, _origemX, _origemZ, _rotacao);
        RegrasInimigo.Avancar(
            _x,
            _z,
            _origemX,
            _origemZ,
            ConfigChefeDaDungeon.VelocidadeDeRetornoMetrosPorSegundo,
            segundos,
            0.0,
            out var novoX,
            out var novoZ
        );
        _x = novoX;
        _z = novoZ;

        if (RegrasInimigo.Distancia(_x, _z, _origemX, _origemZ) <= ToleranciaDeChegadaMetros)
        {
            _x = _origemX;
            _z = _origemZ;
            Mudar(EstadoDeInimigo.Repouso);
        }
    }

    private void Matar()
    {
        Mudar(EstadoDeInimigo.Morto);
        _alvo = RegrasInimigo.SemAlvo;
        _expostoAteMs = 0;
        _areas.Clear();
        _ameacas.Limpar();

        _pendente.Mortes.Add(new InimigoMorreu { InimigoId = _id });
        _pendente.Derrotas.Add(
            new InimigoDerrotado(
                _id,
                _definicao.Id,
                VarianteDeCriatura.Normal,
                _definicao.ExperienciaBase,
                RegrasCriaturas.MultiplicadorDeRecompensa(VarianteDeCriatura.Normal),
                Area,
                _x,
                _z,
                _atacantes.Order().ToArray(),
                _proximaMorte++
            )
        );
    }

    private bool EstaExposto(long agora) => _expostoAteMs != 0 && agora < _expostoAteMs;

    private void Mudar(EstadoDeInimigo para)
    {
        if (!RegrasInimigo.TransicaoValida(_estado, para))
        {
            throw new InvalidOperationException(
                $"Boss {_id}: passagem inválida de {_estado} para {para}."
            );
        }

        _estado = para;
    }

    private JogadorNoCampo MaisProximo(JogadorNoCampo[] candidatos)
    {
        var escolhido = candidatos[0];
        var menor = RegrasInimigo.Distancia(_x, _z, escolhido.X, escolhido.Z);
        foreach (var candidato in candidatos)
        {
            var distancia = RegrasInimigo.Distancia(_x, _z, candidato.X, candidato.Z);
            if (distancia < menor)
            {
                escolhido = candidato;
                menor = distancia;
            }
        }

        return escolhido;
    }

    private long EscolherAlvo(JogadorNoCampo[] engajados, long agora)
    {
        var candidatos = new long[engajados.Length];
        var distancias = new double[engajados.Length];
        for (var i = 0; i < engajados.Length; i++)
        {
            var j = engajados[i];
            candidatos[i] = j.PersonagemId;
            distancias[i] = RegrasInimigo.Distancia(_x, _z, j.X, j.Z);
        }

        var escolhido = _ameacas.EscolherAlvo(
            _alvo,
            candidatos,
            distancias,
            ConfigChefeDaDungeon.RaioDoCorpoMetros,
            agora
        );

        return escolhido != RegrasInimigo.SemAlvo ? escolhido : MaisProximo(engajados).PersonagemId;
    }

    /// <summary>
    /// Força o boss a focar no provocador na hora e iguala sua ameaça ao maior da tabela por 4 s.
    /// </summary>
    public bool Provocar(long alvoId, long provocadorId)
    {
        lock (_trava)
        {
            if (
                alvoId != _id
                || _estado == EstadoDeInimigo.Morto
                || _estado == EstadoDeInimigo.Retorno
            )
            {
                return false;
            }

            _ameacas.Provocar(provocadorId, _relogioMs());
            _alvo = provocadorId;
            return true;
        }
    }

    /// <summary>Retorna a ameaça de um personagem sobre o boss (para consultas e testes).</summary>
    public double ObterAmeaca(long personagemId)
    {
        lock (_trava)
        {
            return _ameacas.ObterAmeaca(personagemId);
        }
    }

    private void OlharPara(JogadorNoCampo[] candidatos)
    {
        var alvo = Array.Find(candidatos, j => j.PersonagemId == _alvo);
        if (alvo.PersonagemId == _alvo && _alvo != RegrasInimigo.SemAlvo)
        {
            _rotacao = RegrasInimigo.DirecaoPara(_x, _z, alvo.X, alvo.Z, _rotacao);
        }
    }

    private InimigoNoMundo Retrato() =>
        new()
        {
            InimigoId = _id,
            CriaturaId = _definicao.Id,
            Variante = VarianteDeCriatura.Normal,
            X = (float)_x,
            Y = MapaDoMundo.AlturaEm((float)_x, (float)_z),
            Z = (float)_z,
            RotacaoY = (float)_rotacao,
            Vida = _vida,
            VidaMaxima = ConfigChefeDaDungeon.VidaMaxima,
            Estado = _estado,
            AlvoId = _alvo,
        };

    private InimigoMudou Mudanca() =>
        new()
        {
            InimigoId = _id,
            X = (float)_x,
            Y = MapaDoMundo.AlturaEm((float)_x, (float)_z),
            Z = (float)_z,
            RotacaoY = (float)_rotacao,
            Estado = _estado,
            AlvoId = _alvo,
            Vida = _vida,
            Sequencia = _sequencia,
        };

    private static long EmMs(double segundos) => (long)Math.Round(segundos * 1000.0);
}
