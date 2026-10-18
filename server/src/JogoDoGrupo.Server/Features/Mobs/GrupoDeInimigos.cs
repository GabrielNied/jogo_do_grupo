using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mobs;

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
public sealed partial class GrupoDeInimigos : IAlvosDeCombate, IInimigosSimulados
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
    private readonly IHistoricoDePosicoesDoMundo? _historico;
    private Inimigo[] _inimigos;

    /// <summary>Só nas manadas: a rota que o grupo segue em repouso; nulo nos outros grupos.</summary>
    private readonly RotaDaManada? _rota;
    private readonly IEstadoDeQueda _queda;
    private readonly bool _semReposicao;
    private readonly double _multiplicadorVida;
    private readonly double _multiplicadorDano;
    private readonly PeriodoDeAparicao _periodo;
    private readonly IRelogioDoMundo? _relogioDoMundo;

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

    /// <summary>Quem atacou um vizinho da mesma espécie e deve ser visto por este grupo (#915).</summary>
    private readonly HashSet<long> _puxados = [];

    /// <summary>Jogadores perto do ponto no último passo (respawn dinâmico, #897).</summary>
    private int _lotacao;
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
        IEstadoDeQueda? queda = null,
        bool semReposicao = false,
        IHistoricoDePosicoesDoMundo? historico = null,
        PeriodoDeAparicao periodo = PeriodoDeAparicao.Sempre,
        IRelogioDoMundo? relogioDoMundo = null,
        double multiplicadorVida = 1.0,
        double multiplicadorDano = 1.0
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
        _semReposicao = semReposicao;
        _multiplicadorVida = multiplicadorVida;
        _multiplicadorDano = multiplicadorDano;
        _historico = historico;
        _periodo = periodo;
        _relogioDoMundo = relogioDoMundo;
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

    /// <summary>Período em que o grupo é ativo (dia, noite ou sempre).</summary>
    public PeriodoDeAparicao Periodo => _periodo;

    /// <summary>Se o grupo está no período ativo de aparição e reposição.</summary>
    public bool PeriodoAtivo()
    {
        if (_relogioDoMundo == null || _periodo == PeriodoDeAparicao.Sempre)
        {
            return true;
        }

        var ehNoite = _relogioDoMundo.EhNoite();
        return _periodo switch
        {
            PeriodoDeAparicao.Dia => !ehNoite,
            PeriodoDeAparicao.Noite => ehNoite,
            _ => true,
        };
    }

    /// <summary>
    /// Um grupo para o ponto de spawn, com o tamanho e o nível sorteados nas faixas dele.
    /// </summary>
    public static GrupoDeInimigos DoPonto(
        PontoDeSpawn ponto,
        ISorteadorDeCriaturas sorteador,
        LimiteDeInimigosVivos? limite = null,
        Func<long>? relogioMs = null,
        IEstadoDeQueda? queda = null,
        IHistoricoDePosicoesDoMundo? historico = null,
        IRelogioDoMundo? relogioDoMundo = null
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
            queda,
            historico: historico,
            periodo: ponto.Periodo,
            relogioDoMundo: relogioDoMundo
        );

    /// <summary>
    /// Grupo fixo de um salão de instância: sem respawn e sem o teto de vivos do campo. O nível
    /// de cada inimigo é sorteado na faixa do pedido.
    /// </summary>
    public static GrupoDeInimigos ParaInstancia(
        string area,
        GrupoDeInstancia pedido,
        ISorteadorDeCriaturas sorteador,
        long primeiroId,
        IEstadoDeQueda? queda = null,
        Func<long>? relogioMs = null
    ) =>
        new(
            CatalogoCriaturas.Obter(pedido.CriaturaId)
                ?? throw new ArgumentException(
                    $"Criatura {pedido.CriaturaId} não existe no catálogo."
                ),
            sorteador,
            pedido.Quantidade,
            relogioMs,
            area,
            pedido.X,
            pedido.Z,
            primeiroId,
            tamanhoMinimo: pedido.Quantidade,
            nivelMinimo: pedido.NivelMinimo,
            nivelMaximo: pedido.NivelMaximo,
            queda: queda,
            semReposicao: true,
            multiplicadorVida: pedido.MultiplicadorVida,
            multiplicadorDano: pedido.MultiplicadorDano
        );

    /// <summary>Quantos inimigos a mais o comando de teste pode acrescentar neste grupo.</summary>
    public const int AparicoesExtrasDeTeste = 20;

    /// <summary>Área do mundo em que o grupo vive.</summary>
    public string Area { get; }

    /// <summary>Definição da criatura base deste grupo.</summary>
    public DefinicaoDeCriatura Definicao => _definicao;

    /// <summary>Categoria da criatura do grupo.</summary>
    public CategoriaDeCriatura Categoria => _definicao.Categoria;

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
            _lotacao = ContarLotacao(jogadores);

            if (_aparicao != null)
            {
                for (var i = 0; i < _inimigos.Length; i++)
                {
                    var inimigo = _inimigos[i];
                    if (inimigo.Estado == EstadoDeInimigo.Morto)
                    {
                        if (_aparicao.Atualizar(Instante(agora), emCombate: false, out _))
                        {
                            var chefe = Criar(inimigo.OrigemX, inimigo.OrigemZ, false);
                            _inimigos[i] = chefe;
                            passo.Aparicoes.Add(new InimigoApareceu { Inimigo = Retrato(chefe) });
                        }
                    }
                    else
                    {
                        var emCombate = !ForaDeLuta(inimigo);
                        _aparicao.Atualizar(Instante(agora), emCombate, out var sumiu);
                        if (sumiu)
                        {
                            inimigo.Estado = EstadoDeInimigo.Morto;
                            inimigo.Alvo = RegrasInimigo.SemAlvo;
                            inimigo.Preparando = false;
                            inimigo.Ameacas.Limpar();
                            inimigo.Participantes.Clear();
                            passo.Mortes.Add(new InimigoMorreu { InimigoId = inimigo.Id });
                        }
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

            Func<long, bool> deveEsquecer = id =>
                _queda.EstaCaido(id)
                || !presentes.TryGetValue(id, out var visto)
                || NaZonaUrbana(visto.X, visto.Z);

            _vistos.RemoveWhere(deveEsquecer.Invoke);

            foreach (var inimigo in _inimigos)
            {
                inimigo.Ameacas.RemoverOnde(deveEsquecer);
            }
            if (_inimigos.All(ForaDeLuta))
            {
                // Ninguém em alerta, perseguindo ou atacando: o grupo esquece quem viu. Quem
                // continua por perto é visto de novo logo abaixo.
                _vistos.Clear();
            }

            // Puxada em cadeia: quem atacou um vizinho da mesma espécie passa a ser visto.
            foreach (var puxado in _puxados)
            {
                if (
                    presentes.TryGetValue(puxado, out var alvo)
                    && !NaZonaUrbana(alvo.X, alvo.Z)
                    && !_queda.EstaCaido(puxado)
                )
                {
                    _vistos.Add(puxado);
                }
            }

            _puxados.Clear();

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

    // Métodos de IAlvosDeCombate, dano, interrupção e puxada em GrupoDeInimigos.Combate.cs.

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

        if (mudouPosicao)
        {
            RegistrarPosicao(inimigo, agora);
        }
    }

    private void RegistrarPosicao(Inimigo inimigo, long instanteMs)
    {
        _historico?.Registrar(
            inimigo.Id,
            new PosicaoNoMundo(
                Area,
                (float)inimigo.X,
                0f,
                (float)inimigo.Z,
                (float)inimigo.Rotacao
            ),
            instanteMs
        );
    }

    // Rotas de manada, repouso e zona urbana em GrupoDeInimigos.Repouso.cs.

    private Inimigo Criar(double origemX, double origemZ, bool contaNoLimite)
    {
        var nivel =
            _nivelMinimo == _nivelMaximo
                ? _nivelMinimo
                : EscalaPorNivel.Sortear(_nivelMinimo, _nivelMaximo, _sorteador.Proximo());
        var criatura = EscalaPorNivel.Aplicar(_sorteador.Sortear(_definicao), nivel);
        if (_multiplicadorVida != 1.0 || _multiplicadorDano != 1.0)
        {
            criatura = criatura with
            {
                VidaMaxima = (int)Math.Max(1, Math.Round(criatura.VidaMaxima * _multiplicadorVida)),
                Dano = (int)Math.Max(1, Math.Round(criatura.Dano * _multiplicadorDano)),
            };
        }
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

        var escolhido = inimigo.Ameacas.EscolherAlvo(
            inimigo.Alvo,
            candidatos,
            distancias,
            RegrasInimigo.AlcanceDeAtaqueMetros,
            agora
        );
        // Ser o alvo do inimigo conta como participar da luta (#909).
        if (escolhido != RegrasInimigo.SemAlvo)
        {
            inimigo.Participantes.Add(escolhido);
        }

        return escolhido;
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

    // Quem está a até ConfigRespawn.RaioDaLotacaoMetros do ponto do grupo e de pé.
    private int ContarLotacao(IReadOnlyList<JogadorNoCampo> jogadores)
    {
        var total = 0;
        foreach (var jogador in jogadores)
        {
            if (
                jogador.PersonagemId != RegrasInimigo.SemAlvo
                && !_queda.EstaCaido(jogador.PersonagemId)
                && RegrasInimigo.Distancia(_pontoX, _pontoZ, jogador.X, jogador.Z)
                    <= ConfigRespawn.RaioDaLotacaoMetros
            )
            {
                total++;
            }
        }

        return total;
    }

    private static bool ForaDeLuta(Inimigo inimigo) =>
        inimigo.Estado
            is EstadoDeInimigo.Repouso
                or EstadoDeInimigo.Retorno
                or EstadoDeInimigo.Morto;

    private static bool EmRepousoOuMorto(Inimigo inimigo) =>
        inimigo.Estado is EstadoDeInimigo.Repouso or EstadoDeInimigo.Morto;

    // O relógio do grupo é em ms; o controle do chefe raro usa instantes. Só a diferença importa.
    private static DateTimeOffset Instante(long relogioMs) =>
        DateTimeOffset.UnixEpoch.AddMilliseconds(relogioMs);

    private static long EmMs(double segundos) => (long)Math.Round(segundos * 1000.0);

    /// <summary>Queda ausente: testes unitários da IA sem Combate.</summary>
    private sealed class QuedaIgnorada : IEstadoDeQueda
    {
        public static readonly QuedaIgnorada Instancia = new();

        public bool EstaCaido(long personagemId) => false;
    }
}
