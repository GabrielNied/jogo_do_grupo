using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// O estado caído no servidor (docs/features/personagem.md, "Morte e caído"; fatia-jogavel.md,
/// seção 7). Combate é o dono: quando a vida chega a 0, o personagem fica caído por 45 s; um
/// aliado vivo a até 2 m levanta segurando a interação por 3 s (volta com 20% da vida); fim do
/// tempo ou pedido depois de 5 s volta à cidade com tudo cheio. As decisões ficam em
/// <see cref="ControleDeQueda"/> (puro); aqui estão a rede, o banco e os eventos.
/// <para>
/// Cada caído tem um laço próprio que dorme até o próximo momento (fim da interação ou do tempo)
/// ou até a próxima conferência de alcance, e resolve. Os pedidos do client e o laço correm em
/// threads diferentes, mas o controle devolve cada saída uma única vez: quem a recebe a aplica.
/// </para>
/// <para>
/// O ajudante que levanta precisa pertencer ao mesmo grupo do caído (<see cref="IConsultaDeGrupo"/>).
/// Estranhos recebem a recusa <see cref="CodigosErroCombate.NaoSaoDoMesmoGrupo"/>.
/// </para>
/// </summary>
public sealed class ServicoDeQueda : IEstadoDeQueda, IHostedService, IDisposable
{
    private readonly ServidorTempoReal _tempoReal;
    private readonly IPosicoesDoMundo _posicoes;
    private readonly EventosDoServidor _eventos;
    private readonly IServiceScopeFactory _escopos;
    private readonly ILogger<ServicoDeQueda> _log;
    private readonly Func<long> _relogioMs;
    private readonly ConfigQueda _config;
    private readonly ControleDeQueda _controle;
    private readonly ConcurrentDictionary<long, byte> _gravando = new();
    private readonly CancellationTokenSource _parada = new();
    private readonly IVizinhanca? _vizinhanca;
    private readonly IConsultaDeGrupo? _grupos;

    public ServicoDeQueda(
        ServidorTempoReal tempoReal,
        IPosicoesDoMundo posicoes,
        EventosDoServidor eventos,
        IServiceScopeFactory escopos,
        ILogger<ServicoDeQueda> log,
        ConfigQueda? config = null,
        Func<long>? relogioMs = null,
        IVizinhanca? vizinhanca = null,
        IConsultaDeGrupo? grupos = null
    )
    {
        _vizinhanca = vizinhanca;
        _grupos = grupos;
        _tempoReal = tempoReal;
        _posicoes = posicoes;
        _eventos = eventos;
        _escopos = escopos;
        _log = log;
        _config = config ?? new ConfigQueda();
        _controle = new ControleDeQueda(_config, id => _gravando[id] = 0);
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
        _tempoReal.SessaoEncerrada += AoEncerrar;
    }

    internal ControleDeQueda Controle => _controle;

    internal void LimparGravandoParaTeste(long personagemId) =>
        _gravando.TryRemove(personagemId, out _);

    public Task StartAsync(CancellationToken cancelamento) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancelamento)
    {
        try
        {
            _parada.Cancel();
        }
        catch (ObjectDisposedException) { }

        return Task.CompletedTask;
    }

    public void Dispose() => _tempoReal.SessaoEncerrada -= AoEncerrar;

    /// <summary>
    /// Verdadeiro enquanto está caído, sendo levantado ou com a saída sendo gravada no banco
    /// (nesse intervalo a vida guardada ainda é 0; ninguém deve bater nem tratar como alvo).
    /// </summary>
    public bool EstaCaido(long personagemId) =>
        _controle.EstaCaido(personagemId) || _gravando.ContainsKey(personagemId);

    /// <summary>
    /// Verdadeiro durante os primeiros 2 s após ser levantado por um aliado. Nesse intervalo
    /// o personagem não recebe dano.
    /// </summary>
    public bool EstaImuneAposLevantar(long personagemId, long agoraMs) =>
        _controle.EstaImuneAposLevantar(personagemId, agoraMs);

    /// <summary>
    /// Verdadeiro durante os primeiros 2 s após ser levantado por um aliado no instante atual.
    /// </summary>
    public bool EstaImuneAposLevantar(long personagemId) =>
        _controle.EstaImuneAposLevantar(personagemId, _relogioMs());

    // ── Entradas vindas de Combate ────────────────────────────────────────────────

    /// <summary>
    /// O personagem levou dano e a vida que sobrou é <paramref name="vidaRestante"/>. Vida 0
    /// leva a caído; vida acima de 0 interrompe a interação de quem estava levantando alguém.
    /// </summary>
    public void AoDano(long personagemId, int vidaRestante)
    {
        if (vidaRestante <= 0)
        {
            Cair(personagemId);
            return;
        }

        Interromper(personagemId);
    }

    /// <summary>
    /// O personagem atacou ou esquivou: se estava levantando alguém, a interação zera. Não faz
    /// nada para quem não levanta ninguém.
    /// </summary>
    public void AoAgir(long personagemId) => Interromper(personagemId);

    /// <summary>A vida chegou a 0: passa a estar caído e começa a contagem.</summary>
    public void Cair(long personagemId)
    {
        if (!_controle.Cair(personagemId, _relogioMs(), out var efeito))
        {
            return;
        }

        var area = AreaDe(personagemId);
        PublicarSemFalhar(new PersonagemCaiu(personagemId, area));
        Aplicar(efeito);
        Avisar(personagemId);
        _ = MonitorarAsync(personagemId);
    }

    // ── Pedidos do client ─────────────────────────────────────────────────────────

    /// <summary>Trata <c>ComecarALevantar</c> (laço de rede).</summary>
    public void AoComecarALevantar(SessaoTempoReal sessao, ComecarALevantar pedido)
    {
        var ajudanteId = sessao.PersonagemId;
        var alvoId = pedido.AlvoId;

        if (_grupos != null)
        {
            var grupoAjudante = _grupos.Consultar(ajudanteId);
            var grupoAlvo = _grupos.Consultar(alvoId);
            if (
                grupoAjudante == null
                || grupoAlvo == null
                || grupoAjudante.GrupoId != grupoAlvo.GrupoId
            )
            {
                Recusar(sessao, CodigosErroCombate.NaoSaoDoMesmoGrupo);
                return;
            }
        }

        var resultado = _controle.Comecar(
            ajudanteId,
            alvoId,
            _relogioMs(),
            () => NoAlcance(ajudanteId, alvoId),
            out var efeito
        );
        Aplicar(efeito);

        switch (resultado)
        {
            case ResultadoDeComecar.Iniciado:
                Avisar(alvoId);
                break;
            case ResultadoDeComecar.VoceEstaCaido:
                Recusar(sessao, CodigosErroCombate.VoceEstaCaido);
                break;
            case ResultadoDeComecar.AlvoNaoEstaCaido:
                Recusar(sessao, CodigosErroCombate.AlvoNaoEstaCaido);
                break;
            case ResultadoDeComecar.LongeDemais:
                Recusar(sessao, CodigosErroCombate.LongeDemais);
                break;
            default:
                Recusar(sessao, CodigosErroCombate.JaEstaSendoLevantado);
                break;
        }
    }

    /// <summary>Trata <c>CancelarLevantar</c> (laço de rede).</summary>
    public void AoCancelarLevantar(SessaoTempoReal sessao) => Interromper(sessao.PersonagemId);

    /// <summary>Trata <c>VoltarParaACidade</c> (laço de rede).</summary>
    public void AoVoltarParaACidade(SessaoTempoReal sessao)
    {
        var resultado = _controle.Voltar(sessao.PersonagemId, _relogioMs(), out var saida);
        if (saida.Houve)
        {
            Aplicar(saida);
        }

        switch (resultado)
        {
            case ResultadoDeVoltar.NaoEstaCaido:
                Recusar(sessao, CodigosErroCombate.NaoEstaCaido);
                break;
            case ResultadoDeVoltar.AindaNaoPodeVoltar:
                Recusar(sessao, CodigosErroCombate.AindaNaoPodeVoltar);
                break;
        }
    }

    // ── Laço de cada caído ────────────────────────────────────────────────────────

    // Dorme até o próximo momento (ou a próxima conferência de alcance) e resolve. Termina
    // quando o caído sai, por qualquer caminho.
    private async Task MonitorarAsync(long alvoId)
    {
        try
        {
            while (!_parada.IsCancellationRequested)
            {
                var falta = _controle.MsAteOProximoEvento(alvoId, _relogioMs());
                if (falta < 0)
                {
                    return;
                }

                var espera = Math.Clamp(falta, 1, Math.Max(1, _config.IntervaloDeConferenciaMs));
                await Task.Delay(TimeSpan.FromMilliseconds(espera), _parada.Token);

                var saida = _controle.Resolver(alvoId, _relogioMs());
                if (saida.Houve)
                {
                    Aplicar(saida);
                    return;
                }

                var ajudanteId = _controle.AjudanteDe(alvoId);
                if (ajudanteId != 0 && !NoAlcance(ajudanteId, alvoId))
                {
                    Interromper(ajudanteId);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            _log.LogError(e, "Erro na contagem do caído {Personagem}.", alvoId);
        }
    }

    // ── Aplicar o que o controle decidiu ──────────────────────────────────────────

    private void Interromper(long ajudanteId)
    {
        var alvoId = _controle.Interromper(ajudanteId, _relogioMs(), out var saida);
        if (saida.Houve)
        {
            Aplicar(saida);
        }

        if (alvoId != 0)
        {
            Avisar(alvoId);
        }
    }

    private void Aplicar(EfeitoDaQueda efeito)
    {
        foreach (var saida in efeito.Saidas)
        {
            Aplicar(saida);
        }

        if (efeito.AlvoInterrompido != 0)
        {
            Avisar(efeito.AlvoInterrompido);
        }
    }

    // Cada saída chega aqui uma única vez. O controle já marcou "gravando" na própria trava, então
    // ninguém bate no personagem enquanto a vida guardada ainda é 0.
    private void Aplicar(SaidaDaQueda saida)
    {
        if (saida.Houve)
        {
            _ = AplicarSaidaAsync(saida);
        }
    }

    private async Task AplicarSaidaAsync(SaidaDaQueda saida)
    {
        var id = saida.PersonagemId;
        var area = AreaDe(id);
        try
        {
            var levantou = saida.Tipo == TipoDeSaida.Levantou;
            var recursos = levantou
                ? await RecursosDaQueda.LevantarAsync(_escopos, id, CancellationToken.None)
                : await RecursosDaQueda.EncherTudoAsync(_escopos, id, CancellationToken.None);

            _gravando.TryRemove(id, out _);
            if (SessaoDe(id) is { } dono)
            {
                _tempoReal.Enviar(dono, CombateMessageIds.RecursosMudaram, recursos);
            }

            Difundir(
                id,
                saida.AjudanteId,
                new EstadoDeQueda
                {
                    PersonagemId = id,
                    Estado = levantou ? FaseDeQueda.Vivo : FaseDeQueda.Retorno,
                    AjudanteId = saida.AjudanteId,
                }
            );

            if (levantou)
            {
                PublicarSemFalhar(new PersonagemLevantou(id, saida.AjudanteId, area));
            }
            else
            {
                PublicarSemFalhar(new PersonagemVoltouParaACidade(id, area));
            }
        }
        catch (Exception e)
        {
            _gravando.TryRemove(id, out _);
            _log.LogError(e, "Erro ao gravar a saída do caído {Personagem}.", id);
        }
    }

    // ── Avisos ────────────────────────────────────────────────────────────────────

    // Manda o estado atual do alvo ao caído, ao ajudante e a quem está na área.
    private void Avisar(long alvoId)
    {
        var situacao = _controle.Situacao(alvoId, _relogioMs());
        Difundir(
            alvoId,
            situacao.AjudanteId,
            new EstadoDeQueda
            {
                PersonagemId = alvoId,
                Estado = situacao.Fase,
                SegundosRestantes = RegrasQueda.SegundosRestantes(situacao.RestanteMs),
                AjudanteId = situacao.AjudanteId,
                ProgressoMs = situacao.ProgressoMs,
            }
        );
    }

    private void Difundir(long alvoId, long ajudanteId, EstadoDeQueda estado)
    {
        var destino = new HashSet<long> { alvoId };
        if (ajudanteId != 0)
        {
            destino.Add(ajudanteId);
        }

        if (_posicoes.TentarObter(alvoId, out var posicao))
        {
            if (posicao.Area == AreasDoMundo.Cidade && _vizinhanca != null)
            {
                destino.UnionWith(_vizinhanca.QuemVe(alvoId));
            }
            else
            {
                destino.UnionWith(_posicoes.PersonagensNaArea(posicao.Area));
            }
        }

        foreach (var sessao in _tempoReal.Sessoes)
        {
            if (destino.Contains(sessao.PersonagemId))
            {
                _tempoReal.Enviar(sessao, CombateMessageIds.EstadoDeQueda, estado);
            }
        }
    }

    private void Recusar(SessaoTempoReal sessao, string codigo) =>
        _tempoReal.Enviar(
            sessao,
            CombateMessageIds.AcaoRecusada,
            new AcaoRecusada { Codigo = codigo }
        );

    // ── Apoio ─────────────────────────────────────────────────────────────────────

    private bool NoAlcance(long ajudanteId, long alvoId)
    {
        if (
            !_posicoes.TentarObter(ajudanteId, out var a)
            || !_posicoes.TentarObter(alvoId, out var b)
            || a.Area != b.Area
        )
        {
            return false;
        }

        var dx = (double)a.X - b.X;
        var dz = (double)a.Z - b.Z;
        return RegrasQueda.NoAlcanceParaLevantar(Math.Sqrt(dx * dx + dz * dz));
    }

    private string AreaDe(long personagemId) =>
        _posicoes.TentarObter(personagemId, out var p) ? p.Area : AreasDoMundo.Cidade;

    private SessaoTempoReal? SessaoDe(long personagemId)
    {
        return _tempoReal.TentarObterSessao(personagemId, out var sessao) ? sessao : null;
    }

    private void PublicarSemFalhar<T>(T evento)
        where T : class
    {
        try
        {
            _eventos.Publicar(evento);
        }
        catch (Exception e)
        {
            _log.LogError(e, "Assinante de {Evento} falhou.", typeof(T).Name);
        }
    }

    // Quem sai do jogo deixa de levantar. O caído que sai continua na contagem (a saída acontece
    // no servidor mesmo sem sessão).
    private void AoEncerrar(SessaoTempoReal sessao)
    {
        var id = sessao.PersonagemId;
        if (
            _tempoReal.TentarObterSessao(id, out var outra)
            && outra is not null
            && !ReferenceEquals(outra, sessao)
        )
        {
            return;
        }

        Interromper(id);
    }
}
