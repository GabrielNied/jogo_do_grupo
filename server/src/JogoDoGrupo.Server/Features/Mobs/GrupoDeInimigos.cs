using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>Um jogador que está na área do grupo, com a posição que o servidor aceitou.</summary>
public readonly record struct JogadorNoCampo(long PersonagemId, double X, double Z);

/// <summary>
/// Um <see cref="InimigoMudou"/> a enviar. <paramref name="SoPosicao"/> diz que só a posição
/// ou a direção mudaram (pode ir pela entrega que aceita perda).
/// </summary>
public readonly record struct MudancaDeInimigo(InimigoMudou Mensagem, bool SoPosicao);

/// <summary>
/// O que aconteceu com o grupo desde o passo anterior: as mensagens para quem está na área e
/// os eventos para as outras funcionalidades.
/// </summary>
public sealed class PassoDosInimigos
{
    public List<MudancaDeInimigo> Mudancas { get; } = [];
    public List<InimigoPreparaAtaque> Preparos { get; } = [];
    public List<InimigoMorreu> Mortes { get; } = [];
    public List<InimigoApareceu> Aparicoes { get; } = [];
    public List<GolpeDeInimigoAcertou> Golpes { get; } = [];
    public List<InimigoDerrotado> Derrotas { get; } = [];

    /// <summary>Se não há nada a enviar nem a publicar.</summary>
    public bool Vazio =>
        Mudancas.Count == 0
        && Preparos.Count == 0
        && Mortes.Count == 0
        && Aparicoes.Count == 0
        && Golpes.Count == 0
        && Derrotas.Count == 0;
}

/// <summary>
/// Um grupo de inimigos comuns num ponto do mundo, com a IA autoritativa
/// (docs/features/mobs.md, "Inimigo comum da fatia"): repouso, alerta, perseguição, ataque
/// telegrafado, retorno, morte e volta. Não fala com a rede: <see cref="Passo"/> devolve o que
/// mudou, e quem chama (o <see cref="ServicoDeInimigos"/>) envia e publica. Os números são de
/// <see cref="RegrasInimigo"/>, da ficha da criatura e de <see cref="ConfigInimigosDoCampo"/>.
/// <para>
/// O grupo divide o que vê: o jogador que um inimigo viu vale para todos, então eles acordam
/// juntos. Quando um inimigo desiste da perseguição, o grupo perde o alvo dele de vista até
/// alguém o ver de novo; e quando ninguém do grupo está em luta, ele esquece todos.
/// </para>
/// <para>
/// É também o alvo dos golpes dos jogadores (<see cref="IAlvosDeCombate"/>). Seguro para
/// várias threads: o passo vem do laço de IA e o dano vem das tarefas do Combate.
/// </para>
/// </summary>
public sealed class GrupoDeInimigos : IAlvosDeCombate
{
    private static readonly long AlertaMs = EmMs(RegrasInimigo.AlertaSegundos);
    private static readonly long PreparoMs = EmMs(RegrasInimigo.PreparoDoAtaqueSegundos);
    private static readonly long RecuperacaoMs = EmMs(RegrasInimigo.RecuperacaoDoAtaqueSegundos);

    private readonly Lock _trava = new();
    private readonly DefinicaoDeCriatura _definicao;
    private readonly ISorteadorDeCriaturas _sorteador;
    private readonly Func<long> _relogioMs;
    private readonly Inimigo[] _inimigos;

    /// <summary>Jogadores que o grupo viu e que continuam na área.</summary>
    private readonly HashSet<long> _vistos = [];
    private PassoDosInimigos _pendente = new();
    private long _proximoId;
    private long? _ultimoPassoMs;

    /// <param name="definicao">Ficha da criatura do grupo.</param>
    /// <param name="sorteador">Sorteia a variante de cada inimigo que aparece.</param>
    /// <param name="tamanho">Quantos inimigos o grupo tem (pelo menos 1).</param>
    /// <param name="relogioMs">Relógio em milissegundos; os testes trocam por um controlado.</param>
    /// <param name="area">Área do mundo em que o grupo vive.</param>
    /// <param name="pontoX">Centro do grupo, no plano do chão.</param>
    /// <param name="pontoZ">Centro do grupo, no plano do chão.</param>
    /// <param name="primeiroId">Id do primeiro inimigo; os seguintes somam 1.</param>
    public GrupoDeInimigos(
        DefinicaoDeCriatura definicao,
        ISorteadorDeCriaturas sorteador,
        int tamanho,
        Func<long>? relogioMs = null,
        string area = ConfigInimigosDoCampo.Area,
        double pontoX = ConfigInimigosDoCampo.PontoX,
        double pontoZ = ConfigInimigosDoCampo.PontoZ,
        long primeiroId = ConfigInimigosDoCampo.PrimeiroInimigoId
    )
    {
        ArgumentNullException.ThrowIfNull(definicao);
        ArgumentNullException.ThrowIfNull(sorteador);
        ArgumentOutOfRangeException.ThrowIfLessThan(tamanho, 1);

        _definicao = definicao;
        _sorteador = sorteador;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
        _proximoId = primeiroId;
        Area = area;

        _inimigos = new Inimigo[tamanho];
        for (var i = 0; i < tamanho; i++)
        {
            var angulo = 2.0 * Math.PI * i / tamanho;
            _inimigos[i] = Criar(
                pontoX + (Math.Sin(angulo) * ConfigInimigosDoCampo.RaioDoEspalhamentoMetros),
                pontoZ + (Math.Cos(angulo) * ConfigInimigosDoCampo.RaioDoEspalhamentoMetros)
            );
        }
    }

    /// <summary>Área do mundo em que o grupo vive.</summary>
    public string Area { get; }

    /// <summary>Quantos inimigos o grupo tem, vivos ou mortos.</summary>
    public int Tamanho => _inimigos.Length;

    /// <summary>Retrato dos inimigos vivos agora (o que vai em <see cref="InimigosDaArea"/>).</summary>
    public InimigoNoMundo[] Vivos()
    {
        lock (_trava)
        {
            return _inimigos
                .Where(i => RegrasInimigo.EstaVivo(i.Estado))
                .Select(Retrato)
                .ToArray();
        }
    }

    /// <summary>
    /// Avança a IA até agora e devolve o que mudou desde o passo anterior, inclusive as mortes
    /// que os golpes dos jogadores causaram no meio tempo. <paramref name="jogadores"/> são os
    /// personagens que estão na área do grupo.
    /// </summary>
    public PassoDosInimigos Passo(IReadOnlyList<JogadorNoCampo> jogadores)
    {
        ArgumentNullException.ThrowIfNull(jogadores);
        lock (_trava)
        {
            var agora = _relogioMs();
            var passo = _pendente;
            _pendente = new PassoDosInimigos();
            var segundos = _ultimoPassoMs is { } ultimo
                ? Math.Clamp(
                    (agora - ultimo) / 1000.0,
                    0.0,
                    ConfigInimigosDoCampo.PassoMaximoSegundos
                )
                : 0.0;
            _ultimoPassoMs = agora;

            for (var i = 0; i < _inimigos.Length; i++)
            {
                var morto = _inimigos[i];
                if (
                    morto.Estado == EstadoDeInimigo.Morto
                    && morto.VoltaEmMs is { } volta
                    && agora >= volta
                )
                {
                    var novo = Criar(morto.OrigemX, morto.OrigemZ);
                    _inimigos[i] = novo;
                    passo.Aparicoes.Add(new InimigoApareceu { Inimigo = Retrato(novo) });
                }
            }

            var presentes = new Dictionary<long, JogadorNoCampo>();
            foreach (var jogador in jogadores)
            {
                if (jogador.PersonagemId != RegrasInimigo.SemAlvo)
                {
                    presentes[jogador.PersonagemId] = jogador;
                }
            }

            _vistos.RemoveWhere(id => !presentes.ContainsKey(id));
            if (_inimigos.All(ForaDeLuta))
            {
                // Ninguém em alerta, perseguindo ou atacando: o grupo esquece quem viu. Quem
                // continua por perto é visto de novo logo abaixo.
                _vistos.Clear();
            }

            if (presentes.Count == 0 && _inimigos.All(EmRepousoOuMorto))
            {
                return passo; // Ninguém por perto e ninguém fora do lugar: nada a decidir.
            }

            var emOrdem = presentes.Values.OrderBy(j => j.PersonagemId).ToArray();
            foreach (var inimigo in _inimigos)
            {
                if (!RegrasInimigo.ProcuraAlvo(inimigo.Estado))
                {
                    continue;
                }

                foreach (var jogador in emOrdem)
                {
                    var distancia = RegrasInimigo.Distancia(
                        inimigo.X,
                        inimigo.Z,
                        jogador.X,
                        jogador.Z
                    );
                    if (RegrasInimigo.Ve(distancia))
                    {
                        _vistos.Add(jogador.PersonagemId);
                    }
                }
            }

            var candidatos = _vistos.Order().ToArray();
            var perdidos = new List<long>();
            foreach (var inimigo in _inimigos)
            {
                if (RegrasInimigo.EstaVivo(inimigo.Estado))
                {
                    Atualizar(
                        inimigo,
                        agora,
                        segundos,
                        presentes,
                        emOrdem,
                        candidatos,
                        perdidos,
                        passo
                    );
                }
            }

            // Só no fim do passo, para todos os inimigos decidirem com os mesmos candidatos.
            foreach (var perdido in perdidos)
            {
                _vistos.Remove(perdido);
            }

            return passo;
        }
    }

    public IReadOnlyList<AlvoDeCombate> AlvosNoVolume(
        string area,
        double origemX,
        double origemZ,
        double direcaoYGraus
    )
    {
        if (area != Area)
        {
            return [];
        }

        lock (_trava)
        {
            var alvos = new List<AlvoDeCombate>();
            foreach (var inimigo in _inimigos)
            {
                if (
                    RegrasInimigo.EstaVivo(inimigo.Estado)
                    && RegrasAtaque.EstaNoVolume(
                        origemX,
                        origemZ,
                        direcaoYGraus,
                        inimigo.X,
                        inimigo.Z,
                        RegrasInimigo.RaioDoCorpoMetros
                    )
                )
                {
                    alvos.Add(
                        new AlvoDeCombate(
                            inimigo.Id,
                            inimigo.X,
                            inimigo.Z,
                            RegrasInimigo.RaioDoCorpoMetros,
                            inimigo.Vida,
                            inimigo.Criatura.VidaMaxima
                        )
                    );
                }
            }

            return alvos;
        }
    }

    public bool TentarAplicarDano(long alvoId, int dano, out ResultadoDeDano resultado)
    {
        resultado = default;
        if (dano <= 0)
        {
            return false;
        }

        lock (_trava)
        {
            var inimigo = Array.Find(_inimigos, i => i.Id == alvoId);
            if (inimigo == null || !RegrasInimigo.EstaVivo(inimigo.Estado))
            {
                return false;
            }

            var aplicado = Math.Min(dano, inimigo.Vida);
            inimigo.Vida -= aplicado;
            inimigo.VidaMudou = true;
            resultado = new ResultadoDeDano(
                inimigo.Id,
                aplicado,
                inimigo.Vida,
                inimigo.Criatura.VidaMaxima
            );
            if (inimigo.Vida <= 0)
            {
                Matar(inimigo);
            }

            return true;
        }
    }

    // Tudo daqui para baixo é chamado com a trava.

    private void Atualizar(
        Inimigo inimigo,
        long agora,
        double segundos,
        Dictionary<long, JogadorNoCampo> presentes,
        JogadorNoCampo[] emOrdem,
        long[] candidatos,
        List<long> perdidos,
        PassoDosInimigos passo
    )
    {
        var antes = (inimigo.X, inimigo.Z, inimigo.Rotacao, inimigo.Estado, inimigo.Alvo);

        switch (inimigo.Estado)
        {
            case EstadoDeInimigo.Repouso:
                if (candidatos.Length > 0)
                {
                    inimigo.Alvo = Escolher(inimigo, candidatos, presentes);
                    inimigo.AlertaAteMs = agora + AlertaMs;
                    Mudar(inimigo, EstadoDeInimigo.Alerta);
                    OlharParaOAlvo(inimigo, presentes);
                }

                break;

            case EstadoDeInimigo.Alerta:
                inimigo.Alvo = Escolher(inimigo, candidatos, presentes);
                if (inimigo.Alvo == RegrasInimigo.SemAlvo)
                {
                    Retornar(inimigo);
                    break;
                }

                OlharParaOAlvo(inimigo, presentes);
                if (agora >= inimigo.AlertaAteMs)
                {
                    Mudar(inimigo, EstadoDeInimigo.Perseguicao);
                }

                break;

            case EstadoDeInimigo.Perseguicao:
                Perseguir(inimigo, agora, segundos, presentes, candidatos, perdidos, passo);
                break;

            case EstadoDeInimigo.Ataque:
                Atacar(inimigo, agora, presentes, emOrdem, candidatos, passo);
                break;

            case EstadoDeInimigo.Retorno:
                Voltar(inimigo, segundos);
                break;
        }

        var mudouPosicao =
            antes.X != inimigo.X || antes.Z != inimigo.Z || antes.Rotacao != inimigo.Rotacao;
        var mudouOResto =
            antes.Estado != inimigo.Estado || antes.Alvo != inimigo.Alvo || inimigo.VidaMudou;
        inimigo.VidaMudou = false;
        if (mudouPosicao || mudouOResto)
        {
            inimigo.Sequencia++;
            passo.Mudancas.Add(new MudancaDeInimigo(Mudanca(inimigo), !mudouOResto));
        }
    }

    private void Perseguir(
        Inimigo inimigo,
        long agora,
        double segundos,
        Dictionary<long, JogadorNoCampo> presentes,
        long[] candidatos,
        List<long> perdidos,
        PassoDosInimigos passo
    )
    {
        inimigo.Alvo = Escolher(inimigo, candidatos, presentes);
        if (inimigo.Alvo == RegrasInimigo.SemAlvo)
        {
            Retornar(inimigo);
            return;
        }

        var daOrigem = RegrasInimigo.Distancia(
            inimigo.X,
            inimigo.Z,
            inimigo.OrigemX,
            inimigo.OrigemZ
        );
        if (RegrasInimigo.DeveDesistir(daOrigem))
        {
            perdidos.Add(inimigo.Alvo);
            Retornar(inimigo);
            return;
        }

        var alvo = presentes[inimigo.Alvo];
        if (!NoAlcance(inimigo, alvo))
        {
            // Para um pouco antes do alcance, para o arredondamento não o deixar do lado de fora.
            RegrasInimigo.Avancar(
                inimigo.X,
                inimigo.Z,
                alvo.X,
                alvo.Z,
                RegrasInimigo.VelocidadeDePerseguicaoMetrosPorSegundo,
                segundos,
                RegrasInimigo.AlcanceDeAtaqueMetros - ConfigInimigosDoCampo.FolgaDoAlcanceMetros,
                out var novoX,
                out var novoZ
            );
            inimigo.X = novoX;
            inimigo.Z = novoZ;
        }

        OlharParaOAlvo(inimigo, presentes);
        if (NoAlcance(inimigo, alvo))
        {
            ComecarAtaque(inimigo, agora, passo);
        }
    }

    private void ComecarAtaque(Inimigo inimigo, long agora, PassoDosInimigos passo)
    {
        RegrasInimigo.CentroDoGolpe(
            inimigo.X,
            inimigo.Z,
            inimigo.Rotacao,
            out var centroX,
            out var centroZ
        );
        inimigo.GolpeX = centroX;
        inimigo.GolpeZ = centroZ;
        inimigo.Preparando = true;
        inimigo.FaseAteMs = agora + PreparoMs;
        Mudar(inimigo, EstadoDeInimigo.Ataque);
        passo.Preparos.Add(
            new InimigoPreparaAtaque
            {
                InimigoId = inimigo.Id,
                PreparoMs = RegrasInimigo.PreparoDoAtaqueMs,
                CentroX = (float)centroX,
                CentroZ = (float)centroZ,
                Raio = (float)RegrasInimigo.RaioDoGolpeMetros,
                DirecaoY = (float)inimigo.Rotacao,
            }
        );
    }

    // O ataque, uma vez começado, vai até o fim: aviso, golpe e recuperação.
    private void Atacar(
        Inimigo inimigo,
        long agora,
        Dictionary<long, JogadorNoCampo> presentes,
        JogadorNoCampo[] emOrdem,
        long[] candidatos,
        PassoDosInimigos passo
    )
    {
        if (agora < inimigo.FaseAteMs)
        {
            return;
        }

        if (inimigo.Preparando)
        {
            // O golpe: pega quem está na área agora, tenha o inimigo visto ou não.
            foreach (var jogador in emOrdem)
            {
                if (
                    RegrasInimigo.EstaNaAreaDoGolpe(
                        inimigo.GolpeX,
                        inimigo.GolpeZ,
                        jogador.X,
                        jogador.Z
                    )
                )
                {
                    passo.Golpes.Add(
                        new GolpeDeInimigoAcertou(
                            inimigo.Id,
                            inimigo.Criatura.Definicao.Id,
                            jogador.PersonagemId,
                            inimigo.Criatura.Dano,
                            Area
                        )
                    );
                }
            }

            inimigo.Preparando = false;
            inimigo.FaseAteMs = agora + RecuperacaoMs;
            return;
        }

        inimigo.Alvo = Escolher(inimigo, candidatos, presentes);
        if (inimigo.Alvo == RegrasInimigo.SemAlvo)
        {
            Retornar(inimigo);
        }
        else
        {
            Mudar(inimigo, EstadoDeInimigo.Perseguicao);
        }
    }

    // Volta ao ponto de origem sem olhar para os jogadores.
    private static void Voltar(Inimigo inimigo, double segundos)
    {
        inimigo.Rotacao = RegrasInimigo.DirecaoPara(
            inimigo.X,
            inimigo.Z,
            inimigo.OrigemX,
            inimigo.OrigemZ,
            inimigo.Rotacao
        );
        RegrasInimigo.Avancar(
            inimigo.X,
            inimigo.Z,
            inimigo.OrigemX,
            inimigo.OrigemZ,
            RegrasInimigo.VelocidadeDeRetornoMetrosPorSegundo,
            segundos,
            0.0,
            out var novoX,
            out var novoZ
        );
        inimigo.X = novoX;
        inimigo.Z = novoZ;

        var falta = RegrasInimigo.Distancia(
            inimigo.X,
            inimigo.Z,
            inimigo.OrigemX,
            inimigo.OrigemZ
        );
        if (falta <= ConfigInimigosDoCampo.ToleranciaDeChegadaMetros)
        {
            inimigo.X = inimigo.OrigemX;
            inimigo.Z = inimigo.OrigemZ;
            inimigo.Rotacao = ConfigInimigosDoCampo.DirecaoDeRepousoGraus;
            Mudar(inimigo, EstadoDeInimigo.Repouso);
        }
    }

    // Desiste: larga o alvo e recupera a vida toda. A variante continua a mesma.
    private static void Retornar(Inimigo inimigo)
    {
        inimigo.Alvo = RegrasInimigo.SemAlvo;
        inimigo.Preparando = false;
        if (inimigo.Vida != inimigo.Criatura.VidaMaxima)
        {
            inimigo.Vida = inimigo.Criatura.VidaMaxima;
            inimigo.VidaMudou = true;
        }

        Mudar(inimigo, EstadoDeInimigo.Retorno);
    }

    private void Matar(Inimigo inimigo)
    {
        Mudar(inimigo, EstadoDeInimigo.Morto);
        inimigo.Alvo = RegrasInimigo.SemAlvo;
        inimigo.Preparando = false;

        var intervalo = inimigo.Criatura.Definicao.IntervaloDeAparicaoSegundos;
        inimigo.VoltaEmMs = intervalo > 0 ? _relogioMs() + (intervalo * 1000L) : null;

        _pendente.Mortes.Add(new InimigoMorreu { InimigoId = inimigo.Id });
        _pendente.Derrotas.Add(
            new InimigoDerrotado(
                inimigo.Id,
                inimigo.Criatura.Definicao.Id,
                inimigo.Criatura.Variante,
                inimigo.Criatura.Experiencia,
                inimigo.Criatura.MultiplicadorDeRecompensa,
                Area,
                inimigo.X,
                inimigo.Z,
                _vistos.Order().ToArray()
            )
        );
    }

    private Inimigo Criar(double origemX, double origemZ) =>
        new(_proximoId++, _sorteador.Sortear(_definicao), origemX, origemZ);

    private static void Mudar(Inimigo inimigo, EstadoDeInimigo para)
    {
        if (!RegrasInimigo.TransicaoValida(inimigo.Estado, para))
        {
            throw new InvalidOperationException(
                $"Inimigo {inimigo.Id}: passagem inválida de {inimigo.Estado} para {para}."
            );
        }

        inimigo.Estado = para;
    }

    private static long Escolher(
        Inimigo inimigo,
        long[] candidatos,
        Dictionary<long, JogadorNoCampo> presentes
    )
    {
        var distancias = new double[candidatos.Length];
        for (var i = 0; i < candidatos.Length; i++)
        {
            var jogador = presentes[candidatos[i]];
            distancias[i] = RegrasInimigo.Distancia(inimigo.X, inimigo.Z, jogador.X, jogador.Z);
        }

        return RegrasInimigo.EscolherAlvo(inimigo.Alvo, candidatos, distancias);
    }

    private static void OlharParaOAlvo(Inimigo inimigo, Dictionary<long, JogadorNoCampo> presentes)
    {
        if (presentes.TryGetValue(inimigo.Alvo, out var alvo))
        {
            inimigo.Rotacao = RegrasInimigo.DirecaoPara(
                inimigo.X,
                inimigo.Z,
                alvo.X,
                alvo.Z,
                inimigo.Rotacao
            );
        }
    }

    private static bool NoAlcance(Inimigo inimigo, JogadorNoCampo alvo) =>
        RegrasInimigo.NoAlcanceDeAtaque(
            RegrasInimigo.Distancia(inimigo.X, inimigo.Z, alvo.X, alvo.Z)
        );

    private static bool ForaDeLuta(Inimigo inimigo) =>
        inimigo.Estado
            is EstadoDeInimigo.Repouso
                or EstadoDeInimigo.Retorno
                or EstadoDeInimigo.Morto;

    private static bool EmRepousoOuMorto(Inimigo inimigo) =>
        inimigo.Estado is EstadoDeInimigo.Repouso or EstadoDeInimigo.Morto;

    private static InimigoNoMundo Retrato(Inimigo inimigo) =>
        new()
        {
            InimigoId = inimigo.Id,
            CriaturaId = inimigo.Criatura.Definicao.Id,
            Variante = inimigo.Criatura.Variante,
            X = (float)inimigo.X,
            Y = Altura(inimigo),
            Z = (float)inimigo.Z,
            RotacaoY = (float)inimigo.Rotacao,
            Vida = inimigo.Vida,
            VidaMaxima = inimigo.Criatura.VidaMaxima,
            Estado = inimigo.Estado,
            AlvoId = inimigo.Alvo,
        };

    private static InimigoMudou Mudanca(Inimigo inimigo) =>
        new()
        {
            InimigoId = inimigo.Id,
            X = (float)inimigo.X,
            Y = Altura(inimigo),
            Z = (float)inimigo.Z,
            RotacaoY = (float)inimigo.Rotacao,
            Estado = inimigo.Estado,
            AlvoId = inimigo.Alvo,
            Vida = inimigo.Vida,
            Sequencia = inimigo.Sequencia,
        };

    // O inimigo anda no plano; a altura é a do chão do ponto, a mesma que o client desenha.
    private static float Altura(Inimigo inimigo) =>
        MapaDoMundo.AlturaEm((float)inimigo.X, (float)inimigo.Z);

    private static long EmMs(double segundos) => (long)Math.Round(segundos * 1000.0);

    private sealed class Inimigo(long id, CriaturaSorteada criatura, double origemX, double origemZ)
    {
        public long Id { get; } = id;
        public CriaturaSorteada Criatura { get; } = criatura;
        public double OrigemX { get; } = origemX;
        public double OrigemZ { get; } = origemZ;

        public double X = origemX;
        public double Z = origemZ;
        public double Rotacao = ConfigInimigosDoCampo.DirecaoDeRepousoGraus;
        public EstadoDeInimigo Estado = EstadoDeInimigo.Repouso;
        public long Alvo = RegrasInimigo.SemAlvo;
        public int Vida = criatura.VidaMaxima;
        public int Sequencia;

        /// <summary>Quando o alerta acaba e a perseguição começa.</summary>
        public long AlertaAteMs;

        /// <summary>No ataque: verdadeiro durante o aviso, falso na recuperação.</summary>
        public bool Preparando;

        /// <summary>No ataque: quando a fase atual (aviso ou recuperação) acaba.</summary>
        public long FaseAteMs;

        public double GolpeX;
        public double GolpeZ;

        /// <summary>Morto: quando ele volta; nulo se não volta sozinho.</summary>
        public long? VoltaEmMs;

        /// <summary>A vida mudou desde o último <see cref="InimigoMudou"/>.</summary>
        public bool VidaMudou;
    }
}
