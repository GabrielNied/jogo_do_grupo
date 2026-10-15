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
    public List<InimigoAcertou> Acertos { get; } = [];
    public List<InimigoMorreu> Mortes { get; } = [];
    public List<InimigoApareceu> Aparicoes { get; } = [];
    public List<InimigoExposto> Expostos { get; } = [];
    public List<GolpeDeInimigoAcertou> Golpes { get; } = [];
    public List<InimigoDerrotado> Derrotas { get; } = [];

    /// <summary>Se não há nada a enviar nem a publicar.</summary>
    public bool Vazio =>
        Mudancas.Count == 0
        && Preparos.Count == 0
        && Acertos.Count == 0
        && Mortes.Count == 0
        && Aparicoes.Count == 0
        && Expostos.Count == 0
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
public sealed class GrupoDeInimigos : IAlvosDeCombate, IInimigosSimulados
{
    private static readonly long AlertaMs = EmMs(RegrasInimigo.AlertaSegundos);

    private readonly Lock _trava = new();
    private readonly DefinicaoDeCriatura _definicao;
    private readonly ISorteadorDeCriaturas _sorteador;
    private readonly Func<long> _relogioMs;
    private readonly int _tetoDeTeste;
    private readonly ComportamentoDeCriatura _comportamento;
    private readonly int _tamanhoMinimo;
    private readonly int _nivelMinimo;
    private readonly int _nivelMaximo;
    private readonly double _pontoX;
    private readonly double _pontoZ;
    private readonly LimiteDeInimigosVivos? _limite;
    private Inimigo[] _inimigos;

    /// <summary>Só nas manadas: a rota que o grupo segue em repouso; nulo nos outros grupos.</summary>
    private readonly RotaDaManada? _rota;
    private readonly IEstadoDeQueda _queda;

    private int _pontoDaRota;
    private bool _andando;
    private long _esperaAteMs;

    /// <summary>
    /// Quando o grupo todo reaparece (só quando não há nenhum vivo e a criatura volta sozinha);
    /// nulo se não há reaparição marcada.
    /// </summary>
    private long? _ondaEmMs;

    /// <summary>Só no chefe raro: decide quando ele aparece; nulo nas outras categorias.</summary>
    private readonly AparicaoDeChefeRaro? _aparicao;

    /// <summary>Jogadores que o grupo viu e que continuam na área.</summary>
    private readonly HashSet<long> _vistos = [];
    private PassoDosInimigos _pendente = new();
    private long _proximoId;
    private long _proximaMorte = 1;
    private long? _ultimoPassoMs;

    /// <param name="definicao">Ficha da criatura do grupo.</param>
    /// <param name="sorteador">Sorteia a variante de cada inimigo que aparece.</param>
    /// <param name="tamanho">Quantos inimigos o grupo tem, no máximo (pelo menos 1).</param>
    /// <param name="relogioMs">Relógio em milissegundos; os testes trocam por um controlado.</param>
    /// <param name="area">Área do mundo em que o grupo vive.</param>
    /// <param name="pontoX">Centro do grupo, no plano do chão.</param>
    /// <param name="pontoZ">Centro do grupo, no plano do chão.</param>
    /// <param name="primeiroId">Id do primeiro inimigo; os seguintes somam 1.</param>
    /// <param name="tamanhoMinimo">
    /// Menor tamanho do grupo; o tamanho é sorteado de <paramref name="tamanhoMinimo"/> a
    /// <paramref name="tamanho"/> cada vez que o grupo aparece. Nulo: o tamanho é sempre
    /// <paramref name="tamanho"/>.
    /// </param>
    /// <param name="nivelMinimo">
    /// Faixa de nível de cada inimigo da criatura comum, sorteado quando ele aparece. Zero nos
    /// dois: o nível da ficha.
    /// </param>
    /// <param name="nivelMaximo">Veja <paramref name="nivelMinimo"/>.</param>
    /// <param name="limite">Teto de vivos dividido com os outros grupos; nulo: sem teto.</param>
    /// <param name="rota">
    /// Rota de uma manada: em repouso o grupo anda de ponto em ponto, nunca entra na zona
    /// urbana e desiste mais longe que o limite da rota. Nulo: um grupo parado no ponto.
    /// </param>
    /// <param name="queda">
    /// Estado caído (Combate): quem está caído ou sendo levantado não é visto nem acertado.
    /// </param>
    public GrupoDeInimigos(
        DefinicaoDeCriatura definicao,
        ISorteadorDeCriaturas sorteador,
        int tamanho,
        Func<long>? relogioMs = null,
        string area = ConfigInimigosDoCampo.Area,
        double pontoX = ConfigInimigosDoCampo.PontoX,
        double pontoZ = ConfigInimigosDoCampo.PontoZ,
        long primeiroId = ConfigInimigosDoCampo.PrimeiroInimigoId,
        int? tamanhoMinimo = null,
        int nivelMinimo = 0,
        int nivelMaximo = 0,
        LimiteDeInimigosVivos? limite = null,
        RotaDaManada? rota = null,
        IEstadoDeQueda? queda = null
    )
    {
        ArgumentNullException.ThrowIfNull(definicao);
        ArgumentNullException.ThrowIfNull(sorteador);
        ArgumentOutOfRangeException.ThrowIfLessThan(tamanho, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tamanhoMinimo ?? tamanho, tamanho);
        ArgumentOutOfRangeException.ThrowIfLessThan(tamanhoMinimo ?? tamanho, 1);

        _definicao = definicao;
        _sorteador = sorteador;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
        _proximoId = primeiroId;
        _comportamento = ComportamentoDeCriatura.Obter(definicao.Id);
        _tamanhoMinimo = tamanhoMinimo ?? tamanho;
        _nivelMinimo = nivelMinimo > 0 ? nivelMinimo : definicao.Nivel;
        _nivelMaximo = nivelMaximo > 0 ? nivelMaximo : _nivelMinimo;
        ArgumentOutOfRangeException.ThrowIfLessThan(_nivelMaximo, _nivelMinimo);
        _pontoX = pontoX;
        _pontoZ = pontoZ;
        _limite = limite;
        _rota = rota;
        _queda = queda ?? QuedaIgnorada.Instancia;
        Area = area;
        Capacidade = tamanho;

        if (definicao.Categoria == CategoriaDeCriatura.ChefeRaro)
        {
            // Um chefe raro é um só e começa fora do mundo: quem o traz é o sorteio.
            ArgumentOutOfRangeException.ThrowIfNotEqual(tamanho, 1);
            _aparicao = new AparicaoDeChefeRaro(definicao, sorteador, Instante(_relogioMs()));
            _inimigos = [CriarNoCirculo(0, 1, false)];
            _inimigos[0].Estado = EstadoDeInimigo.Morto;
        }
        else
        {
            _inimigos = [];
            MontarGrupo(_relogioMs(), null);
        }

        _tetoDeTeste = tamanho + AparicoesExtrasDeTeste;
    }

    /// <summary>
    /// Um grupo para o ponto de spawn, com o tamanho e o nível sorteados nas faixas dele.
    /// </summary>
    public static GrupoDeInimigos DoPonto(
        PontoDeSpawn ponto,
        ISorteadorDeCriaturas sorteador,
        LimiteDeInimigosVivos? limite = null,
        Func<long>? relogioMs = null,
        IEstadoDeQueda? queda = null
    ) =>
        new(
            CatalogoCriaturas.Obter(ponto.CriaturaId)
                ?? throw new ArgumentException(
                    $"Criatura {ponto.CriaturaId} não existe no catálogo."
                ),
            sorteador,
            ponto.TamanhoMaximo,
            relogioMs,
            ponto.Area,
            ponto.X,
            ponto.Z,
            ponto.PrimeiroId,
            ponto.TamanhoMinimo,
            ponto.NivelMinimo,
            ponto.NivelMaximo,
            limite,
            ponto.Rota,
            queda
        );

    /// <summary>Quantos inimigos a mais o comando de teste pode acrescentar neste grupo.</summary>
    public const int AparicoesExtrasDeTeste = 20;

    /// <summary>Área do mundo em que o grupo vive.</summary>
    public string Area { get; }

    /// <summary>O maior número de inimigos que o grupo pode ter de uma vez.</summary>
    public int Capacidade { get; }

    /// <summary>Quantos inimigos o grupo tem agora, vivos ou mortos.</summary>
    public int Tamanho => _inimigos.Length;

    /// <summary>
    /// Acrescenta um inimigo vivo em (x, z), para o comando de teste. Nulo se a posição não é
    /// finita ou se o grupo já ganhou <see cref="AparicoesExtrasDeTeste"/>.
    /// </summary>
    public InimigoNoMundo? ForcarAparicao(double x, double z)
    {
        if (!double.IsFinite(x) || !double.IsFinite(z))
        {
            return null;
        }

        lock (_trava)
        {
            if (_inimigos.Length >= _tetoDeTeste)
            {
                return null;
            }

            var novo = Criar(x, z, false);
            novo.Segue = false; // o de teste fica onde nasceu: não acompanha a rota
            novo.Reponivel = false; // nem volta depois de morto
            var maior = new Inimigo[_inimigos.Length + 1];
            Array.Copy(_inimigos, maior, _inimigos.Length);
            maior[^1] = novo;
            _inimigos = maior;
            return Retrato(novo);
        }
    }

    /// <summary>Retrato dos inimigos vivos agora (o que vai em <see cref="InimigosDaArea"/>).</summary>
    public InimigoNoMundo[] Vivos()
    {
        lock (_trava)
        {
            return _inimigos.Where(i => RegrasInimigo.EstaVivo(i.Estado)).Select(Retrato).ToArray();
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

            if (_aparicao != null)
            {
                for (var i = 0; i < _inimigos.Length; i++)
                {
                    var morto = _inimigos[i];
                    if (
                        morto.Estado == EstadoDeInimigo.Morto
                        && _aparicao.Atualizar(Instante(agora))
                    )
                    {
                        var chefe = Criar(morto.OrigemX, morto.OrigemZ, false);
                        _inimigos[i] = chefe;
                        passo.Aparicoes.Add(new InimigoApareceu { Inimigo = Retrato(chefe) });
                    }
                }
            }
            else if (_ondaEmMs is { } onda && agora >= onda)
            {
                // O grupo todo reaparece de uma vez, com tamanho e níveis sorteados de novo.
                MontarGrupo(agora, passo.Aparicoes);
            }
            else
            {
                ReporMortos(agora, passo.Aparicoes);
            }

            AtualizarRota(agora);

            var presentes = new Dictionary<long, JogadorNoCampo>();
            foreach (var jogador in jogadores)
            {
                if (jogador.PersonagemId != RegrasInimigo.SemAlvo)
                {
                    presentes[jogador.PersonagemId] = jogador;
                }
            }

            _vistos.RemoveWhere(id =>
                _queda.EstaCaido(id)
                || !presentes.TryGetValue(id, out var visto)
                || NaZonaUrbana(visto.X, visto.Z)
            );

            foreach (var inimigo in _inimigos)
            {
                foreach (var id in inimigo.Ameacas.Personagens.ToArray())
                {
                    if (
                        _queda.EstaCaido(id)
                        || !presentes.TryGetValue(id, out var visto)
                        || NaZonaUrbana(visto.X, visto.Z)
                    )
                    {
                        inimigo.Ameacas.Remover(id);
                    }
                }
            }
            if (_inimigos.All(ForaDeLuta))
            {
                // Ninguém em alerta, perseguindo ou atacando: o grupo esquece quem viu. Quem
                // continua por perto é visto de novo logo abaixo.
                _vistos.Clear();
            }

            if (_rota == null && presentes.Count == 0 && _inimigos.All(EmRepousoOuMorto))
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
                    if (NaZonaUrbana(jogador.X, jogador.Z))
                    {
                        continue; // moradores e visitantes da cidade ficam a salvo da manada
                    }

                    if (_queda.EstaCaido(jogador.PersonagemId))
                    {
                        continue;
                    }

                    var distancia = RegrasInimigo.Distancia(
                        inimigo.X,
                        inimigo.Z,
                        jogador.X,
                        jogador.Z
                    );
                    if (_comportamento.Ve(distancia))
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
            var inimigo = Array.Find(_inimigos, i => i.Id == alvoId);
            // Na volta ele fica imune (Evade): sem isso o jogador bate de graça num inimigo
            // que não revida e que depois sobe de vida cheia (issue #488).
            if (
                inimigo == null
                || !RegrasInimigo.EstaVivo(inimigo.Estado)
                || inimigo.Estado == EstadoDeInimigo.Retorno
            )
            {
                return false;
            }

            var aplicado = Math.Min(dano, inimigo.Vida);
            inimigo.Vida -= aplicado;
            inimigo.Atacantes.Add(atacanteId);
            inimigo.Ameacas.AdicionarDano(atacanteId, aplicado);
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

    /// <summary>
    /// Reação ao acerto (Combate): um acerto no preparo cancela o golpe, que não chega ao
    /// contato, e o inimigo fica sem atacar por <paramref name="duracaoMs"/> (a recuperação
    /// dele passa a ser essa pausa). A recuperação de um golpe que já encostou continua.
    /// </summary>
    public bool Interromper(long alvoId, int duracaoMs)
    {
        lock (_trava)
        {
            var inimigo = Array.Find(_inimigos, i => i.Id == alvoId);
            if (inimigo == null || inimigo.Estado != EstadoDeInimigo.Ataque || !inimigo.Preparando)
            {
                return false;
            }

            inimigo.Preparando = false;
            inimigo.FaseAteMs = _relogioMs() + duracaoMs;
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
                    inimigo.Alvo = Escolher(inimigo, candidatos, presentes, agora);
                    inimigo.AlertaAteMs = agora + AlertaMs;
                    Mudar(inimigo, EstadoDeInimigo.Alerta);
                    OlharParaOAlvo(inimigo, presentes, segundos);
                    inimigo.Vagando = false;
                }
                else if (_rota != null && inimigo.Segue)
                {
                    SeguirRota(inimigo, segundos);
                }
                else
                {
                    VagarEmRepouso(inimigo, agora, segundos);
                }

                break;

            case EstadoDeInimigo.Alerta:
                inimigo.Alvo = Escolher(inimigo, candidatos, presentes, agora);
                if (inimigo.Alvo == RegrasInimigo.SemAlvo)
                {
                    Retornar(inimigo);
                    break;
                }

                OlharParaOAlvo(inimigo, presentes, segundos);
                if (agora >= inimigo.AlertaAteMs)
                {
                    Mudar(inimigo, EstadoDeInimigo.Perseguicao);
                }

                break;

            case EstadoDeInimigo.Perseguicao:
                Perseguir(inimigo, agora, segundos, presentes, candidatos, perdidos, passo);
                break;

            case EstadoDeInimigo.Ataque:
                Atacar(inimigo, agora, segundos, presentes, emOrdem, candidatos, passo);
                break;

            case EstadoDeInimigo.Retorno:
                Voltar(inimigo, agora, segundos);
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
        inimigo.Alvo = Escolher(inimigo, candidatos, presentes, agora);
        if (inimigo.Alvo == RegrasInimigo.SemAlvo)
        {
            Retornar(inimigo);
            return;
        }

        // Vaga de atacante: só RegrasCerco.LimiteDeAtacantesPorAlvo atacam o mesmo alvo ao mesmo
        // tempo. Quem espera ronda no anel de fora, e isso não é kite (não conta no limite).
        var podeAtacar = RegrasCerco.PodeAtacar(AtacantesContra(inimigo));
        if (podeAtacar)
        {
            inimigo.SegundosSemAtacar += segundos;
        }
        else
        {
            inimigo.SegundosSemAtacar = 0.0;
        }

        var daOrigem = RegrasInimigo.Distancia(
            inimigo.X,
            inimigo.Z,
            inimigo.OrigemX,
            inimigo.OrigemZ
        );
        if (inimigo.SegundosSemAtacar >= ConfigInimigosDoCampo.LimiteSemAtacarSegundos)
        {
            // Kite na borda: o alvo nunca fica ao alcance. Desiste como no limite de distância.
            perdidos.Add(inimigo.Alvo);
            Retornar(inimigo);
            return;
        }

        if (
            _rota == null
                ? RegrasInimigo.DeveDesistir(daOrigem)
                : RegrasManada.DeveDesistir(daOrigem, _rota.LimiteDePerseguicaoMetros)
        )
        {
            perdidos.Add(inimigo.Alvo);
            Retornar(inimigo);
            return;
        }

        if (!presentes.TryGetValue(inimigo.Alvo, out var alvo) || _queda.EstaCaido(inimigo.Alvo))
        {
            if (inimigo.Alvo != RegrasInimigo.SemAlvo)
            {
                perdidos.Add(inimigo.Alvo);
            }

            Retornar(inimigo);
            return;
        }

        // Vai ao posto dele no anel em volta do alvo (o de atacante, ou o de ronda se não há
        // vaga), em vez de em linha reta até o alvo, sem entrar no corpo dos outros.
        RegrasCerco.Posto(
            alvo.X,
            alvo.Z,
            Array.IndexOf(_inimigos, inimigo),
            !podeAtacar,
            out var postoX,
            out var postoZ
        );
        RegrasInimigo.Avancar(
            inimigo.X,
            inimigo.Z,
            postoX,
            postoZ,
            _comportamento.VelocidadeDePerseguicaoMetrosPorSegundo,
            segundos,
            0.0,
            out var novoX,
            out var novoZ
        );
        Afastar(
            inimigo,
            novoX,
            novoZ,
            out novoX,
            out novoZ,
            _comportamento.VelocidadeDePerseguicaoMetrosPorSegundo * segundos
        );
        if (NaZonaUrbana(novoX, novoZ))
        {
            // A borda da cidade: a manada não passa. Encerra a perseguição e volta.
            perdidos.Add(inimigo.Alvo);
            Retornar(inimigo);
            return;
        }

        inimigo.X = novoX;
        inimigo.Z = novoZ;

        OlharParaOAlvo(inimigo, presentes, segundos);
        if (podeAtacar && NoAlcance(inimigo, alvo))
        {
            ComecarAtaque(inimigo, agora, passo);
        }
    }

    // Quantos do grupo (fora ele) já atacam o alvo dele: preparando o golpe ou se recuperando.
    private int AtacantesContra(Inimigo inimigo) =>
        _inimigos.Count(i =>
            i != inimigo && i.Estado == EstadoDeInimigo.Ataque && i.Alvo == inimigo.Alvo
        );

    // Empurra o ponto para fora dos corpos dos outros inimigos vivos do grupo.
    private void Afastar(
        Inimigo inimigo,
        double x,
        double z,
        out double novoX,
        out double novoZ,
        double maxDeslocamento = double.MaxValue
    )
    {
        var vizinhos = _inimigos
            .Where(i => i != inimigo && RegrasInimigo.EstaVivo(i.Estado))
            .ToArray();
        RegrasCerco.Afastar(
            x,
            z,
            vizinhos.Select(i => i.X).ToArray(),
            vizinhos.Select(i => i.Z).ToArray(),
            out novoX,
            out novoZ,
            maxDeslocamento
        );
    }

    // A direção trava aqui; o passo do golpe é medido já, até onde o caminho está livre.
    private void ComecarAtaque(Inimigo inimigo, long agora, PassoDosInimigos passo)
    {
        inimigo.SegundosSemAtacar = 0.0;
        inimigo.DirecaoDoGolpe = inimigo.Rotacao;
        inimigo.PassoRestante = PassoLivre(inimigo);
        inimigo.Preparando = true;
        inimigo.FaseAteMs = agora + _comportamento.PreparoMs;
        Mudar(inimigo, EstadoDeInimigo.Ataque);
        // O golpe comum não tem área no chão: o aviso é o preparo do modelo.
        passo.Preparos.Add(
            new InimigoPreparaAtaque
            {
                InimigoId = inimigo.Id,
                PreparoMs = (int)_comportamento.PreparoMs,
                CentroX = (float)inimigo.X,
                CentroZ = (float)inimigo.Z,
                Raio = 0f,
                DirecaoY = (float)inimigo.DirecaoDoGolpe,
                Tipo = TipoDeAtaqueDeInimigo.Golpe,
                Areas = [],
            }
        );
    }

    // Quanto do passo do golpe cabe na direção travada: para antes da borda da cidade (a
    // parede do campo) e antes de chegar a menos de 1,2 m de outro inimigo.
    private double PassoLivre(Inimigo inimigo) =>
        PassoDoGolpe.Livre(
            inimigo.X,
            inimigo.Z,
            inimigo.DirecaoDoGolpe,
            (deX, deZ, x, z) => PodeIrPara(inimigo, deX, deZ, x, z)
        );

    // Se o inimigo pode ir de um ponto a outro: fora da zona urbana e sem se aproximar de outro
    // inimigo vivo a menos da separação mínima.
    private bool PodeIrPara(Inimigo inimigo, double deX, double deZ, double x, double z)
    {
        if (NaZonaUrbana(x, z))
        {
            return false;
        }

        foreach (var outro in _inimigos)
        {
            if (
                outro != inimigo
                && RegrasInimigo.EstaVivo(outro.Estado)
                && !PassoDoGolpe.RespeitaVizinho(deX, deZ, x, z, outro.X, outro.Z)
            )
            {
                return false;
            }
        }

        return true;
    }

    // Durante o preparo ele avança o passo travado, em velocidade constante, até o fim do aviso.
    private void AvancarNoPreparo(Inimigo inimigo, double segundos)
    {
        if (inimigo.PassoRestante <= 0.0)
        {
            return;
        }

        var velocidade =
            RegrasInimigo.PassoDoGolpeMetros / Math.Max(0.001, _comportamento.PreparoMs / 1000.0);
        var andar = Math.Min(velocidade * segundos, inimigo.PassoRestante);
        var radianos = inimigo.DirecaoDoGolpe * Math.PI / 180.0;
        var novoX = inimigo.X + Math.Sin(radianos) * andar;
        var novoZ = inimigo.Z + Math.Cos(radianos) * andar;
        if (!PodeIrPara(inimigo, inimigo.X, inimigo.Z, novoX, novoZ))
        {
            inimigo.PassoRestante = 0.0;
            return;
        }

        inimigo.X = novoX;
        inimigo.Z = novoZ;
        inimigo.PassoRestante -= andar;
    }

    // O ataque, uma vez começado, vai até o fim: aviso, golpe e recuperação.
    private void Atacar(
        Inimigo inimigo,
        long agora,
        double segundos,
        Dictionary<long, JogadorNoCampo> presentes,
        JogadorNoCampo[] emOrdem,
        long[] candidatos,
        PassoDosInimigos passo
    )
    {
        if (agora < inimigo.FaseAteMs)
        {
            if (inimigo.Preparando)
            {
                AvancarNoPreparo(inimigo, segundos);
            }

            return;
        }

        if (inimigo.Preparando)
        {
            AvancarNoPreparo(inimigo, segundos);
            // O golpe: pega quem está no cone agora, de onde ele parou, tenha o inimigo visto ou não.
            foreach (var jogador in emOrdem)
            {
                if (
                    !_queda.EstaCaido(jogador.PersonagemId)
                    && !NaZonaUrbana(jogador.X, jogador.Z)
                    && RegrasInimigo.EstaNoGolpe(
                        inimigo.X,
                        inimigo.Z,
                        inimigo.DirecaoDoGolpe,
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
                    passo.Acertos.Add(
                        new InimigoAcertou { InimigoId = inimigo.Id, AlvoId = jogador.PersonagemId }
                    );
                }
            }

            inimigo.Preparando = false;
            inimigo.FaseAteMs = agora + _comportamento.RecuperacaoMs;
            return;
        }

        inimigo.Alvo = Escolher(inimigo, candidatos, presentes, agora);
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
    private static void Voltar(Inimigo inimigo, long agora, double segundos)
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

        var falta = RegrasInimigo.Distancia(inimigo.X, inimigo.Z, inimigo.OrigemX, inimigo.OrigemZ);
        if (falta <= ConfigInimigosDoCampo.ToleranciaDeChegadaMetros)
        {
            inimigo.X = inimigo.OrigemX;
            inimigo.Z = inimigo.OrigemZ;
            inimigo.Rotacao = ConfigInimigosDoCampo.DirecaoDeRepousoGraus;
            inimigo.Vagando = false;
            inimigo.PausaVagarAteMs = agora + EmMs(RegrasInimigo.TempoParadoVagarMinimoSegundos);
            Mudar(inimigo, EstadoDeInimigo.Repouso);
        }
    }

    // Desiste: larga o alvo e recupera a vida toda. A variante continua a mesma.
    private static void Retornar(Inimigo inimigo)
    {
        inimigo.Alvo = RegrasInimigo.SemAlvo;
        inimigo.Preparando = false;
        inimigo.Vagando = false;
        inimigo.SegundosSemAtacar = 0.0;
        inimigo.Atacantes.Clear();
        inimigo.Ameacas.Limpar();
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
        inimigo.Ameacas.Limpar();

        if (_aparicao != null)
        {
            // O chefe raro não volta por tempo fixo: a espera recomeça e o sorteio decide.
            _aparicao.RegistrarMorte(Instante(_relogioMs()));
        }
        else
        {
            if (inimigo.ContaNoLimite)
            {
                _limite?.Liberar();
            }

            // Depois do intervalo da ficha (com uma folga sorteada, para os grupos não voltarem
            // juntos): sem sobrevivente o grupo todo volta contado da última morte; com
            // sobrevivente, o próprio inimigo volta (veja ReporMortos).
            var intervalo = inimigo.Criatura.Definicao.IntervaloDeAparicaoSegundos;
            if (intervalo > 0)
            {
                var espera = EmMs(
                    intervalo
                        * (
                            1.0
                            + (ConfigReposicaoDeInimigos.VariacaoDoIntervalo * _sorteador.Proximo())
                        )
                );
                if (_inimigos.All(i => i.Estado == EstadoDeInimigo.Morto))
                {
                    _ondaEmMs = _relogioMs() + espera;
                }
                else if (inimigo.Reponivel)
                {
                    inimigo.ReporEmMs = _relogioMs() + espera;
                }
            }
        }

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
                inimigo.Atacantes.Order().ToArray(),
                _proximaMorte++,
                inimigo.Criatura.Nivel
            )
        );
    }

    // Reposição individual: com ao menos um sobrevivente e o grupo todo fora de luta, o morto
    // cujo intervalo passou volta no lugar dele, se houver vaga no teto de vivos. Em luta ou sem
    // vaga ele espera (sem vaga, tenta de novo daqui a pouco).
    private void ReporMortos(long agora, List<InimigoApareceu> aparicoes)
    {
        if (
            _ondaEmMs != null
            || !_inimigos.Any(i => i.ReporEmMs is { } quando && agora >= quando)
            || !_inimigos.Any(i => RegrasInimigo.EstaVivo(i.Estado))
            || !_inimigos.All(ForaDeLuta)
        )
        {
            return;
        }

        for (var i = 0; i < _inimigos.Length; i++)
        {
            var morto = _inimigos[i];
            if (morto.ReporEmMs is not { } quando || agora < quando)
            {
                continue;
            }

            if (_limite != null && _limite.Reservar(1) == 0)
            {
                morto.ReporEmMs = agora + EmMs(ConfigInimigosDoCampo.TentativaDeVagaSegundos);
                continue;
            }

            // Uma manada em marcha repõe o membro no ponto da rota para onde ela vai.
            var lugarX = morto.OrigemX;
            var lugarZ = morto.OrigemZ;
            if (_rota != null)
            {
                lugarX = _rota.Pontos[_pontoDaRota].X + morto.DesvioX;
                lugarZ = _rota.Pontos[_pontoDaRota].Z + morto.DesvioZ;
            }

            var novo = Criar(lugarX, lugarZ, _limite != null);
            novo.DesvioX = morto.DesvioX;
            novo.DesvioZ = morto.DesvioZ;
            _inimigos[i] = novo;
            aparicoes.Add(new InimigoApareceu { Inimigo = Retrato(novo) });
        }
    }

    // Sorteia o tamanho do grupo e o põe no mundo, respeitando o teto de vivos. Sem vaga, tenta
    // de novo daqui a pouco.
    private void MontarGrupo(long agora, List<InimigoApareceu>? aparicoes)
    {
        var desejado =
            _tamanhoMinimo == Capacidade
                ? Capacidade
                : EscalaPorNivel.Sortear(_tamanhoMinimo, Capacidade, _sorteador.Proximo());
        var vagas = _limite?.Reservar(desejado) ?? desejado;
        if (vagas == 0)
        {
            _inimigos = [];
            _ondaEmMs = agora + EmMs(ConfigInimigosDoCampo.TentativaDeVagaSegundos);
            return;
        }

        var novos = new Inimigo[vagas];
        for (var i = 0; i < vagas; i++)
        {
            novos[i] = CriarNoCirculo(i, vagas, _limite != null);
        }

        _inimigos = novos;
        _ondaEmMs = null;
        _pontoDaRota = 0;
        _andando = false;
        _esperaAteMs = agora + EmMs(_rota?.Pontos[0].EsperaSegundos ?? 0.0);
        aparicoes?.AddRange(novos.Select(n => new InimigoApareceu { Inimigo = Retrato(n) }));
    }

    // Os pontos de origem ficam num círculo em volta do centro, a distâncias iguais.
    private Inimigo CriarNoCirculo(int indice, int total, bool contaNoLimite)
    {
        var angulo = 2.0 * Math.PI * indice / total;
        return Criar(
            _pontoX + (Math.Sin(angulo) * ConfigInimigosDoCampo.RaioDoEspalhamentoMetros),
            _pontoZ + (Math.Cos(angulo) * ConfigInimigosDoCampo.RaioDoEspalhamentoMetros),
            contaNoLimite
        );
    }

    // A manada segue a rota: espera no ponto e, quando todos estão em repouso, parte para o
    // próximo, cada um para o seu lugar (o mesmo desvio em relação ao ponto de antes).
    private void AtualizarRota(long agora)
    {
        if (_rota == null)
        {
            return;
        }

        var seguidores = _inimigos
            .Where(i => i.Segue && RegrasInimigo.EstaVivo(i.Estado))
            .ToArray();
        if (seguidores.Length == 0)
        {
            return;
        }

        var todosParados = seguidores.All(i => i.Estado == EstadoDeInimigo.Repouso);
        if (_andando)
        {
            if (todosParados && seguidores.All(ChegouAoLugar))
            {
                _andando = false;
                _esperaAteMs = agora + EmMs(_rota.Pontos[_pontoDaRota].EsperaSegundos);
            }

            return;
        }

        if (agora < _esperaAteMs || !todosParados)
        {
            return;
        }

        _pontoDaRota = (_pontoDaRota + 1) % _rota.Pontos.Count;
        var ponto = _rota.Pontos[_pontoDaRota];
        foreach (var inimigo in seguidores)
        {
            inimigo.OrigemX = ponto.X + inimigo.DesvioX;
            inimigo.OrigemZ = ponto.Z + inimigo.DesvioZ;
        }

        _andando = true;
    }

    private static bool ChegouAoLugar(Inimigo inimigo) =>
        RegrasInimigo.Distancia(inimigo.X, inimigo.Z, inimigo.OrigemX, inimigo.OrigemZ)
        <= ConfigInimigosDoCampo.ToleranciaDeChegadaMetros;

    // Anda em direção ao lugar na rota, devagar, sem olhar para os jogadores.
    private void SeguirRota(Inimigo inimigo, double segundos)
    {
        if (ChegouAoLugar(inimigo))
        {
            return;
        }

        var direcaoDesejada = RegrasInimigo.DirecaoPara(
            inimigo.X,
            inimigo.Z,
            inimigo.OrigemX,
            inimigo.OrigemZ,
            inimigo.Rotacao
        );
        inimigo.Rotacao = RegrasInimigo.GirarPara(
            inimigo.Rotacao,
            direcaoDesejada,
            RegrasInimigo.VelocidadeDeGiroGrausPorSegundo,
            segundos
        );
        RegrasInimigo.Avancar(
            inimigo.X,
            inimigo.Z,
            inimigo.OrigemX,
            inimigo.OrigemZ,
            _rota!.VelocidadeMetrosPorSegundo,
            segundos,
            0.0,
            out var novoX,
            out var novoZ
        );
        inimigo.X = novoX;
        inimigo.Z = novoZ;
        if (ChegouAoLugar(inimigo))
        {
            inimigo.X = inimigo.OrigemX;
            inimigo.Z = inimigo.OrigemZ;
        }
    }

    // Fora de combate, a criatura comum vaga devagar num raio curto ao redor da sua origem.
    private void VagarEmRepouso(Inimigo inimigo, long agora, double segundos)
    {
        if (!inimigo.Vagando)
        {
            if (agora < inimigo.PausaVagarAteMs)
            {
                return;
            }

            var sorteioAngulo = _sorteador.Proximo();
            var sorteioDistancia = _sorteador.Proximo();
            RegrasInimigo.PontoDeVagar(
                inimigo.OrigemX,
                inimigo.OrigemZ,
                sorteioAngulo,
                sorteioDistancia,
                RegrasInimigo.RaioDeVagarEmRepousoMetros,
                out inimigo.DestinoVagarX,
                out inimigo.DestinoVagarZ
            );
            inimigo.Vagando = true;
        }

        var falta = RegrasInimigo.Distancia(
            inimigo.X,
            inimigo.Z,
            inimigo.DestinoVagarX,
            inimigo.DestinoVagarZ
        );
        if (falta <= ConfigInimigosDoCampo.ToleranciaDeChegadaMetros)
        {
            inimigo.X = inimigo.DestinoVagarX;
            inimigo.Z = inimigo.DestinoVagarZ;
            inimigo.Vagando = false;
            var esperaSegundos =
                RegrasInimigo.TempoParadoVagarMinimoSegundos
                + (
                    RegrasInimigo.TempoParadoVagarMaximoSegundos
                    - RegrasInimigo.TempoParadoVagarMinimoSegundos
                ) * _sorteador.Proximo();
            inimigo.PausaVagarAteMs = agora + EmMs(esperaSegundos);
            return;
        }

        var direcaoDesejada = RegrasInimigo.DirecaoPara(
            inimigo.X,
            inimigo.Z,
            inimigo.DestinoVagarX,
            inimigo.DestinoVagarZ,
            inimigo.Rotacao
        );
        inimigo.Rotacao = RegrasInimigo.GirarPara(
            inimigo.Rotacao,
            direcaoDesejada,
            RegrasInimigo.VelocidadeDeGiroGrausPorSegundo,
            segundos
        );

        RegrasInimigo.Avancar(
            inimigo.X,
            inimigo.Z,
            inimigo.DestinoVagarX,
            inimigo.DestinoVagarZ,
            RegrasInimigo.VelocidadeDeVagarEmRepousoMetrosPorSegundo,
            segundos,
            0.0,
            out var novoX,
            out var novoZ
        );

        Afastar(
            inimigo,
            novoX,
            novoZ,
            out novoX,
            out novoZ,
            RegrasInimigo.VelocidadeDeVagarEmRepousoMetrosPorSegundo * segundos
        );
        if (NaZonaUrbana(novoX, novoZ))
        {
            inimigo.Vagando = false;
            inimigo.PausaVagarAteMs = agora + EmMs(RegrasInimigo.TempoParadoVagarMinimoSegundos);
            return;
        }

        inimigo.X = novoX;
        inimigo.Z = novoZ;
    }

    // Só a manada tem zona urbana; os outros grupos ignoram a cidade como sempre.
    private bool NaZonaUrbana(double x, double z) =>
        _rota != null && RegrasManada.EstaNaZonaUrbana(x, z);

    private Inimigo Criar(double origemX, double origemZ, bool contaNoLimite)
    {
        var nivel =
            _nivelMinimo == _nivelMaximo
                ? _nivelMinimo
                : EscalaPorNivel.Sortear(_nivelMinimo, _nivelMaximo, _sorteador.Proximo());
        var criatura = EscalaPorNivel.Aplicar(_sorteador.Sortear(_definicao), nivel);
        var inimigo = new Inimigo(_proximoId++, criatura, origemX, origemZ, contaNoLimite)
        {
            DesvioX = origemX - _pontoX,
            DesvioZ = origemZ - _pontoZ,
        };
        inimigo.PausaVagarAteMs = _relogioMs() + EmMs(RegrasInimigo.TempoParadoVagarMinimoSegundos);
        return inimigo;
    }

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
        Dictionary<long, JogadorNoCampo> presentes,
        long agora
    )
    {
        var distancias = new double[candidatos.Length];
        for (var i = 0; i < candidatos.Length; i++)
        {
            var jogador = presentes[candidatos[i]];
            distancias[i] = RegrasInimigo.Distancia(inimigo.X, inimigo.Z, jogador.X, jogador.Z);
        }

        return inimigo.Ameacas.EscolherAlvo(
            inimigo.Alvo,
            candidatos,
            distancias,
            RegrasInimigo.AlcanceDeAtaqueMetros,
            agora
        );
    }

    /// <summary>
    /// Força o alvo a focar no provocador na hora e iguala sua ameaça ao maior da tabela por 4 s.
    /// </summary>
    public bool Provocar(long alvoId, long provocadorId)
    {
        lock (_trava)
        {
            var inimigo = Array.Find(_inimigos, i => i.Id == alvoId);
            if (
                inimigo == null
                || !RegrasInimigo.EstaVivo(inimigo.Estado)
                || inimigo.Estado == EstadoDeInimigo.Retorno
            )
            {
                return false;
            }

            inimigo.Ameacas.Provocar(provocadorId, _relogioMs());
            inimigo.Alvo = provocadorId;
            return true;
        }
    }

    /// <summary>Retorna a ameaça de um personagem sobre um inimigo (para consultas e testes).</summary>
    public double ObterAmeaca(long alvoId, long personagemId)
    {
        lock (_trava)
        {
            var inimigo = Array.Find(_inimigos, i => i.Id == alvoId);
            return inimigo?.Ameacas.ObterAmeaca(personagemId) ?? 0.0;
        }
    }

    private static void OlharParaOAlvo(
        Inimigo inimigo,
        Dictionary<long, JogadorNoCampo> presentes,
        double segundos = 0.0
    )
    {
        if (presentes.TryGetValue(inimigo.Alvo, out var alvo))
        {
            var direcaoDesejada = RegrasInimigo.DirecaoPara(
                inimigo.X,
                inimigo.Z,
                alvo.X,
                alvo.Z,
                inimigo.Rotacao
            );
            inimigo.Rotacao =
                segundos > 0.0
                    ? RegrasInimigo.GirarPara(
                        inimigo.Rotacao,
                        direcaoDesejada,
                        RegrasInimigo.VelocidadeDeGiroGrausPorSegundo,
                        segundos
                    )
                    : direcaoDesejada;
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
            Nivel = inimigo.Criatura.Nivel,
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

    // O relógio do grupo é em ms; o controle do chefe raro usa instantes. Só a diferença importa.
    private static DateTimeOffset Instante(long relogioMs) =>
        DateTimeOffset.UnixEpoch.AddMilliseconds(relogioMs);

    private static long EmMs(double segundos) => (long)Math.Round(segundos * 1000.0);

    private sealed class Inimigo(
        long id,
        CriaturaSorteada criatura,
        double origemX,
        double origemZ,
        bool contaNoLimite
    )
    {
        public long Id { get; } = id;

        /// <summary>Se ocupa uma vaga do teto de vivos (os de teste e o chefe raro não).</summary>
        public bool ContaNoLimite { get; } = contaNoLimite;
        public CriaturaSorteada Criatura { get; } = criatura;

        /// <summary>Ponto de origem: o lugar dele no ponto da rota onde a manada está.</summary>
        public double OrigemX { get; set; } = origemX;
        public double OrigemZ { get; set; } = origemZ;

        /// <summary>Onde ele fica em relação ao centro da manada, para andar em bando.</summary>
        public double DesvioX { get; set; }
        public double DesvioZ { get; set; }

        /// <summary>Se volta depois de morto (os de teste, não).</summary>
        public bool Reponivel { get; set; } = true;

        /// <summary>
        /// Quando este morto volta no lugar dele, com o grupo vivo; nulo se não há reposição
        /// marcada (o grupo todo morto volta pela onda).
        /// </summary>
        public long? ReporEmMs { get; set; }

        /// <summary>Se acompanha a rota (os de teste ficam onde nasceram).</summary>
        public bool Segue { get; set; } = true;

        public double X = origemX;
        public double Z = origemZ;
        public double Rotacao = ConfigInimigosDoCampo.DirecaoDeRepousoGraus;
        public EstadoDeInimigo Estado = EstadoDeInimigo.Repouso;
        public long Alvo = RegrasInimigo.SemAlvo;
        public int Vida = criatura.VidaMaxima;
        public int Sequencia;

        /// <summary>Destino atual do passo de vagar em repouso.</summary>
        public double DestinoVagarX = origemX;
        public double DestinoVagarZ = origemZ;

        /// <summary>Quando a pausa em repouso acaba e a criatura pode escolher outro ponto para vagar.</summary>
        public long PausaVagarAteMs;

        /// <summary>Se está em movimento em direção ao ponto de vagar.</summary>
        public bool Vagando;

        /// <summary>Quando o alerta acaba e a perseguição começa.</summary>
        public long AlertaAteMs;

        /// <summary>No ataque: verdadeiro durante o aviso, falso na recuperação.</summary>
        public bool Preparando;

        /// <summary>No ataque: quando a fase atual (aviso ou recuperação) acaba.</summary>
        public long FaseAteMs;
        public double SegundosSemAtacar;

        /// <summary>Direção travada no começo do preparo do golpe, em graus.</summary>
        public double DirecaoDoGolpe;

        /// <summary>Quanto do passo do golpe ainda falta andar, em metros.</summary>
        public double PassoRestante;

        /// <summary>Quem causou dano desde que ele apareceu ou voltou da desistência.</summary>
        public readonly HashSet<long> Atacantes = [];

        /// <summary>Tabela de ameaça individual deste inimigo.</summary>
        public readonly TabelaDeAmeaca Ameacas = new();

        /// <summary>A vida mudou desde o último <see cref="InimigoMudou"/>.</summary>
        public bool VidaMudou;
    }

    /// <summary>Queda ausente: testes unitários da IA sem Combate.</summary>
    private sealed class QuedaIgnorada : IEstadoDeQueda
    {
        public static readonly QuedaIgnorada Instancia = new();

        public bool EstaCaido(long personagemId) => false;
    }
}
