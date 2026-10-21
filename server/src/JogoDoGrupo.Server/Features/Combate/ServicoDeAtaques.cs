using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// O ataque da primeira arma, do pedido ao acerto (docs/features/fatia-jogavel.md, seção 2).
/// O client só envia a intenção: o servidor confere ação em andamento (fila de 1 pedido),
/// queda e stamina, avisa o preparo a quem está na área, e no momento do acerto decide quem está
/// no arco da lâmina (posição histórica dentro de <see cref="RegrasCompensacaoDeLag.JanelaMs"/>, ver ADR
/// 0039) e quanto cada um leva. O mesmo golpe acerta cada alvo uma vez só.
/// <para>
/// O manipulador roda no laço de rede e não bloqueia: a conferência de recursos (banco) e a
/// espera até o acerto rodam em tarefas, que respondem pelo <see cref="ServidorTempoReal"/>
/// (seguro para qualquer thread). O estado das ações fica em <see cref="ControleDeAcoes"/>.
/// </para>
/// </summary>
public sealed class ServicoDeAtaques : IHostedService, IDisposable
{
    private readonly ServidorTempoReal _tempoReal;
    private readonly IPosicoesDoMundo _posicoes;
    private readonly IReadOnlyList<IAlvosDeCombate> _fontes;
    private readonly IServiceScopeFactory _escopos;
    private readonly ILogger<ServicoDeAtaques> _log;
    private readonly Func<long> _relogioMs;
    private readonly Func<long, CancellationToken, Task> _esperarAte;
    private readonly ControleDeAcoes _controle;
    private readonly ServicoDeQueda? _queda;
    private readonly IAliadosDeCombate _aliados;
    private readonly ILimiteDeVelocidadeDoMundo? _limiteDeVelocidade;
    private readonly GerenciadorDeEfeitosDeStatus? _efeitos;
    private readonly IVizinhanca? _vizinhanca;
    private readonly EventosDoServidor? _eventos;
    private readonly IArmaEquipada? _armas;
    private readonly ServicoDeProjeteis? _projeteis;
    private readonly IEstadoDeMovimento? _movimento;
    private readonly Dictionary<long, SessaoTempoReal> _sessoes = [];
    private readonly Lock _travaSessoes = new();

    // Fim da folga (ms) da reação de cada inimigo interrompido; a de jogador fica no ControleDeAcoes.
    private readonly Dictionary<long, long> _folgaDosAlvos = [];
    private readonly Lock _travaReacoes = new();
    private readonly CancellationTokenSource _parada = new();

    public ServicoDeAtaques(
        ServidorTempoReal tempoReal,
        IPosicoesDoMundo posicoes,
        IEnumerable<IAlvosDeCombate> fontes,
        IServiceScopeFactory escopos,
        ILogger<ServicoDeAtaques> log,
        Func<long>? relogioMs = null,
        ControleDeAcoes? controle = null,
        ServicoDeQueda? queda = null,
        IAliadosDeCombate? aliados = null,
        ILimiteDeVelocidadeDoMundo? limiteDeVelocidade = null,
        GerenciadorDeEfeitosDeStatus? efeitos = null,
        IVizinhanca? vizinhanca = null,
        EventosDoServidor? eventos = null,
        Func<long, CancellationToken, Task>? esperarAte = null,
        IArmaEquipada? armas = null,
        ServicoDeProjeteis? projeteis = null,
        IEstadoDeMovimento? movimento = null
    )
    {
        _movimento = movimento;
        _armas = armas;
        _projeteis = projeteis;
        _eventos = eventos;
        _vizinhanca = vizinhanca;
        _efeitos = efeitos;
        _aliados = aliados ?? new NinguemEAliado();
        _limiteDeVelocidade = limiteDeVelocidade;
        _queda = queda;
        // O mesmo controle da esquiva, para ataque e esquiva não valerem ao mesmo tempo.
        _controle = controle ?? new ControleDeAcoes();
        _tempoReal = tempoReal;
        _posicoes = posicoes;
        _fontes = fontes.ToArray();
        _escopos = escopos;
        _log = log;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
        _esperarAte = esperarAte ?? EsperaReal;
        _tempoReal.SessaoEncerrada += AoEncerrar;
    }

    /// <summary>Client pediu para saber qual arma está ativa.</summary>
    public void AoConsultarArmaAtiva(SessaoTempoReal sessao)
    {
        if (_armas == null)
        {
            return;
        }

        _ = EnviarArmaAtivaAsync(sessao);
    }

    private async Task EnviarArmaAtivaAsync(SessaoTempoReal sessao)
    {
        try
        {
            var arma = await _armas!.ObterAsync(sessao.PersonagemId, _parada.Token);
            var tipo = arma?.Tipo ?? TipoEquipamento.Espada;
            _tempoReal.Enviar(
                sessao,
                CombateMessageIds.ArmaAtivaInformada,
                new ArmaAtivaInformada { TipoArma = tipo, Conjunto = arma?.Conjunto ?? 1 }
            );
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            _log.LogDebug(
                e,
                "Não enviou ArmaAtivaInformada para {Personagem}.",
                sessao.PersonagemId
            );
        }
    }

    // Sem relógio de combate injetado: dorme o que falta na escala do próprio relógio do serviço.
    private Task EsperaReal(long instanteMs, CancellationToken cancelamento)
    {
        var falta = instanteMs - _relogioMs();
        return falta > 0
            ? Task.Delay(TimeSpan.FromMilliseconds(falta), cancelamento)
            : Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancelamento) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancelamento)
    {
        try
        {
            _parada.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // O host pode parar de novo depois de já ter descartado o serviço (ex.: o servidor
            // parado à mão e depois descartado); parar duas vezes não é erro.
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _tempoReal.SessaoEncerrada -= AoEncerrar;
        _parada.Dispose();
    }

    /// <summary>Trata um <c>IntencaoDeAtaque</c> (laço de rede).</summary>
    public void AoAtacar(SessaoTempoReal sessao, IntencaoDeAtaque intencao)
    {
        // Valor fora do enum ou direção inválida: o contrato não tem código para isso, então o
        // pedido é descartado sem resposta (um client honesto nunca manda).
        if (!RegrasAtaque.TipoValido(intencao.Tipo) || !float.IsFinite(intencao.DirecaoY))
        {
            _log.LogDebug(
                "Ataque descartado do personagem {Personagem}: tipo {Tipo} ou direção inválidos.",
                sessao.PersonagemId,
                intencao.Tipo
            );
            return;
        }

        if (!float.IsFinite(intencao.ElevacaoGraus))
        {
            intencao.ElevacaoGraus = 0f;
        }
        else
        {
            intencao.ElevacaoGraus = Math.Max(
                RegrasProjetil.ElevacaoMinimaGraus,
                Math.Min(RegrasProjetil.ElevacaoMaximaGraus, intencao.ElevacaoGraus)
            );
        }

        var personagemId = sessao.PersonagemId;
        if (!_posicoes.TentarObter(personagemId, out _))
        {
            return; // Fora do mundo (não entrou na cidade): não há de onde atacar.
        }

        // Atacar interrompe quem levantava um aliado (quem está caído é recusado mais adiante).
        _queda?.AoAgir(personagemId);

        lock (_travaSessoes)
        {
            _sessoes[personagemId] = sessao;
        }

        // Quem nada ou escala não ataca (#349); a posição é a que o Mundo aceitou.
        if (_movimento?.EstaNadando(personagemId) == true)
        {
            Recusar(personagemId, intencao.Sequencia, CodigosErroPersonagem.VoceEstaNadando);
            return;
        }

        if (_movimento?.EstaEscalando(personagemId) == true)
        {
            Recusar(personagemId, intencao.Sequencia, CodigosErroPersonagem.VoceEstaEscalando);
            return;
        }

        var pedido = new PedidoDeAtaque(
            intencao.Tipo,
            intencao.DirecaoY,
            intencao.Sequencia,
            intencao.TempoVistoMs,
            intencao.ElevacaoGraus
        );
        switch (_controle.Pedir(personagemId, pedido, _relogioMs(), out var acao))
        {
            case ResultadoDoPedido.Iniciado:
                _ = ExecutarAsync(personagemId, acao!);
                break;
            case ResultadoDoPedido.Enfileirado:
                break; // Começa no fim da ação em curso (EsperarFimAsync dela).
            case ResultadoDoPedido.Atordoado:
                Recusar(personagemId, pedido.Sequencia, CodigosErroCombate.VoceEstaAtordoado);
                break;
            default:
                Recusar(personagemId, pedido.Sequencia, CodigosErroCombate.AcaoEmAndamento);
                break;
        }
    }

    // Confere os recursos, avisa o preparo e, no momento do acerto, decide quem foi atingido.
    private async Task ExecutarAsync(long personagemId, AcaoDeAtaque acao)
    {
        try
        {
            var inicio = await RecursosDoAtaque.TentarIniciarAsync(
                _escopos,
                personagemId,
                acao.Pedido.Tipo,
                _parada.Token
            );
            if (inicio.Recusa != null)
            {
                Liberar(personagemId, acao, inicio.Recusa);
                return;
            }

            if (!_controle.EstaAtiva(personagemId, acao.Id))
            {
                return; // O personagem saiu enquanto o banco respondia.
            }

            if (inicio.Recursos != null)
            {
                EnviarAoDono(personagemId, CombateMessageIds.RecursosMudaram, inicio.Recursos);
            }

            if (!_posicoes.TentarObter(personagemId, out var posicaoInicial))
            {
                return;
            }

            EnviarParaArea(
                posicaoInicial.Area,
                personagemId,
                CombateMessageIds.AtaqueIniciado,
                new AtaqueIniciado
                {
                    AtacanteId = personagemId,
                    Tipo = acao.Pedido.Tipo,
                    DirecaoY = acao.Pedido.DirecaoY,
                    ElevacaoGraus = acao.Pedido.ElevacaoGraus,
                    PreparoMs = RegrasAtaque.PreparoMs(acao.Pedido.Tipo),
                    Passo = acao.Passo,
                }
            );

            // Compromisso: no preparo e no momento de acerto a corrida fica em 40%. O client
            // segura o próprio corpo; aqui o servidor pede ao Mundo o limite da validação.
            _limiteDeVelocidade?.LimitarVelocidade(
                personagemId,
                RegrasAtaque.VelocidadeDuranteGolpe,
                Math.Max(0, acao.AcertoMs - _relogioMs()) / 1000.0
            );

            // Um relógio só: o acerto sai em InicioMs + PreparoMs (a ida ao banco já correu
            // dentro desse tempo) e a fila só avança depois que o acerto foi resolvido.
            var arma = _armas != null ? await _armas.ObterAsync(personagemId, _parada.Token) : null;
            await EsperarAteAsync(acao.AcertoMs);
            if (arma?.Tipo == TipoEquipamento.Arco && _projeteis != null)
            {
                _projeteis.DispararArco(personagemId, acao, inicio.Dano);
            }
            else if (arma?.Tipo == TipoEquipamento.Cajado && _projeteis != null)
            {
                _projeteis.DispararCajado(personagemId, acao, inicio.Dano, arma.Value.Elemento);
            }
            else
            {
                Acertar(personagemId, acao, inicio.Dano);
            }
            await EsperarAteAsync(acao.FimMs);
            AvancarFila(personagemId, acao);
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            _log.LogError(e, "Erro no ataque do personagem {Personagem}.", personagemId);
            Liberar(personagemId, acao, codigo: null);
        }
    }

    private Task EsperarAteAsync(long instanteMs) => _esperarAte(instanteMs, _parada.Token);

    // No fim da ação, o pedido da fila (se houver) começa.
    private void AvancarFila(long personagemId, AcaoDeAtaque acao)
    {
        var proxima = _controle.AvancarFila(personagemId, acao.Id, _relogioMs());
        if (proxima != null)
        {
            _ = ExecutarAsync(personagemId, proxima);
        }
    }

    // O momento de acerto: o volume usa o instante visto no pedido, ancorado no início da ação
    // (não no acerto após o preparo), para caber na janela de lag.
    private void Acertar(long personagemId, AcaoDeAtaque acao, ParcelasDeDano parcelas)
    {
        if (
            !_controle.EstaAtiva(personagemId, acao.Id)
            || _controle.FoiInterrompida(personagemId, acao.Id) // Levou um acerto no preparo.
            || _queda?.EstaCaido(personagemId) == true // Caiu no meio do golpe: não acerta.
            || !_posicoes.TentarObter(personagemId, out var posicao)
        )
        {
            return;
        }

        var instanteTeste = RegrasCompensacaoDeLag.InstanteParaTeste(
            acao.Pedido.TempoVistoMs,
            acao.InicioMs
        );

        var dano = RegrasAtaque.Dano(acao.Pedido.Tipo, parcelas, acao.Passo);
        if (_efeitos != null)
        {
            dano = Math.Max(
                1,
                (int)Math.Round(dano * _efeitos.MultiplicadorDanoCausado(personagemId))
            );
        }

        var direcao = (float)
            RegrasAtaque.DirecaoDoGolpe(
                acao.Pedido.Tipo,
                acao.Pedido.DirecaoY,
                posicao.RotacaoY,
                acao.PreparoMs / 1000.0
            );
        var arco = ArcosDosGolpes.Obter(acao.Pedido.Tipo, acao.Passo);
        var vistos = new HashSet<long>();
        var acertos = new List<AcertoDeAtaque>();
        var interrompidos = new List<long>();
        foreach (var fonte in _fontes)
        {
            var candidatos = fonte.AlvosProximos(
                posicao.Area,
                posicao.X,
                posicao.Z,
                arco.AlcanceMetros,
                instanteTeste
            );
            foreach (var alvo in candidatos)
            {
                // O golpe só acerta se a lâmina passa pela cápsula do corpo (arco do golpe).
                if (
                    !arco.Alcanca(
                        posicao.X,
                        posicao.Z,
                        direcao,
                        alvo.X,
                        alvo.Z,
                        alvo.Raio,
                        alvo.Altura
                    )
                )
                {
                    continue;
                }

                var danoNoAlvo = dano;
                if (_efeitos != null)
                {
                    danoNoAlvo = Math.Max(
                        1,
                        (int)Math.Round(danoNoAlvo * _efeitos.MultiplicadorDanoRecebido(alvo.Id))
                    );
                }

                // O mesmo golpe não acerta o mesmo alvo duas vezes.
                // Aliados do grupo (e o próprio atacante) não levam o golpe.
                if (
                    SelecaoDeAlvos.PodeAcertar(personagemId, alvo, _aliados)
                    && vistos.Add(alvo.Id)
                    && fonte.TentarAplicarDano(alvo.Id, danoNoAlvo, personagemId, out var r)
                )
                {
                    acertos.Add(
                        new AcertoDeAtaque
                        {
                            AlvoId = r.AlvoId,
                            Dano = r.Dano,
                            VidaRestante = r.VidaRestante,
                            VidaMaxima = r.VidaMaxima,
                        }
                    );
                    if (alvo.PersonagemId == 0 && TentarInterromper(fonte, alvo.Id))
                    {
                        interrompidos.Add(alvo.Id);
                    }
                }
            }
        }

        if (acertos.Count > 0)
        {
            EnviarParaArea(
                posicao.Area,
                personagemId,
                CombateMessageIds.AtaqueAcertou,
                new AtaqueAcertou
                {
                    AtacanteId = personagemId,
                    Acertos = acertos.ToArray(),
                    Tipo = acao.Pedido.Tipo,
                    Sequencia = acao.Pedido.Sequencia,
                    Passo = acao.Passo,
                }
            );

            var alvoIds = new long[acertos.Count];
            for (var i = 0; i < acertos.Count; i++)
            {
                alvoIds[i] = acertos[i].AlvoId;
            }
            _eventos?.Publicar(new GolpeComumAcertou(personagemId, alvoIds));
        }

        foreach (var alvoId in interrompidos)
        {
            EnviarParaArea(
                posicao.Area,
                personagemId,
                CombateMessageIds.AcaoInterrompida,
                new AcaoInterrompida
                {
                    AlvoId = alvoId,
                    Jogador = false,
                    DuracaoMs = RegrasReacao.AtordoamentoNoAcertoMs,
                }
            );
        }
    }

    // Reação ao acerto de um inimigo comum: só interrompe fora da folga da reação anterior, e a
    // folga só começa se a fonte do alvo de fato cancelou um preparo.
    private bool TentarInterromper(IAlvosDeCombate fonte, long alvoId)
    {
        var agora = _relogioMs();
        lock (_travaReacoes)
        {
            if (
                _folgaDosAlvos.TryGetValue(alvoId, out var folgaAteMs)
                && !RegrasReacao.PodeReagir(agora, folgaAteMs)
            )
            {
                return false;
            }

            if (!fonte.Interromper(alvoId, RegrasReacao.AtordoamentoNoAcertoMs))
            {
                return false;
            }

            _folgaDosAlvos[alvoId] = RegrasReacao.FimDaFolgaMs(agora);
            return true;
        }
    }

    // A ação não valeu: libera o personagem, avisa (se há código) e começa o pedido da fila.
    private void Liberar(long personagemId, AcaoDeAtaque acao, string? codigo)
    {
        var proxima = _controle.Desistir(personagemId, acao.Id, _relogioMs());
        if (codigo != null)
        {
            Recusar(personagemId, acao.Pedido.Sequencia, codigo);
        }

        if (proxima != null)
        {
            _ = ExecutarAsync(personagemId, proxima);
        }
    }

    private void Recusar(long personagemId, int sequencia, string codigo)
    {
        if (SessaoDe(personagemId) is { } sessao)
        {
            _tempoReal.Enviar(
                sessao,
                CombateMessageIds.AtaqueRecusado,
                new AtaqueRecusado { Sequencia = sequencia, Codigo = codigo }
            );
        }
    }

    private void EnviarAoDono<T>(long personagemId, ushort id, T mensagem)
    {
        if (SessaoDe(personagemId) is { } sessao)
        {
            _tempoReal.Enviar(sessao, id, mensagem);
        }
    }

    // Quem enxerga o atacante (e o próprio atacante, mesmo se acabou de sair dela).
    private void EnviarParaArea<T>(string area, long atacanteId, ushort id, T mensagem)
    {
        var quemVe =
            (area == AreasDoMundo.Cidade && _vizinhanca != null)
                ? _vizinhanca.QuemVe(atacanteId)
                : _posicoes.PersonagensNaArea(area);
        var presentes = new HashSet<long>(quemVe) { atacanteId };
        foreach (var sessao in _tempoReal.Sessoes)
        {
            if (presentes.Contains(sessao.PersonagemId))
            {
                _tempoReal.Enviar(sessao, id, mensagem);
            }
        }
    }

    private SessaoTempoReal? SessaoDe(long personagemId)
    {
        lock (_travaSessoes)
        {
            return _sessoes.GetValueOrDefault(personagemId);
        }
    }

    private void AoEncerrar(SessaoTempoReal sessao)
    {
        lock (_travaSessoes)
        {
            if (!_sessoes.TryGetValue(sessao.PersonagemId, out var atual) || atual != sessao)
            {
                return;
            }

            _sessoes.Remove(sessao.PersonagemId);
        }

        _controle.Esquecer(sessao.PersonagemId);
    }
}
