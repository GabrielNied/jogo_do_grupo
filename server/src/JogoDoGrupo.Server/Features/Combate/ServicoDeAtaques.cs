using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// O ataque da primeira arma, do pedido ao acerto (docs/features/fatia-jogavel.md, seção 2).
/// O client só envia a intenção: o servidor confere ação em andamento (fila de 1 pedido),
/// queda e vigor, avisa o preparo a quem está na área, e no momento do acerto decide quem está
/// no cone (pela posição que o Mundo tem <b>agora</b>, sem compensação de lag) e quanto cada um
/// leva. O mesmo golpe acerta cada alvo uma vez só.
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
    private readonly TimeProvider _relogio;
    private readonly ILogger<ServicoDeAtaques> _log;
    private readonly Func<long> _relogioMs;
    private readonly ControleDeAcoes _controle = new();
    private readonly Dictionary<long, SessaoTempoReal> _sessoes = [];
    private readonly Lock _travaSessoes = new();
    private readonly CancellationTokenSource _parada = new();

    public ServicoDeAtaques(
        ServidorTempoReal tempoReal,
        IPosicoesDoMundo posicoes,
        IEnumerable<IAlvosDeCombate> fontes,
        IServiceScopeFactory escopos,
        TimeProvider relogio,
        ILogger<ServicoDeAtaques> log,
        Func<long>? relogioMs = null
    )
    {
        _tempoReal = tempoReal;
        _posicoes = posicoes;
        _fontes = fontes.ToArray();
        _escopos = escopos;
        _relogio = relogio;
        _log = log;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
        _tempoReal.SessaoEncerrada += AoEncerrar;
    }

    public Task StartAsync(CancellationToken cancelamento) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancelamento)
    {
        _parada.Cancel();
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

        var personagemId = sessao.PersonagemId;
        if (!_posicoes.TentarObter(personagemId, out _))
        {
            return; // Fora do mundo (não entrou na cidade): não há de onde atacar.
        }

        lock (_travaSessoes)
        {
            _sessoes[personagemId] = sessao;
        }

        var pedido = new PedidoDeAtaque(intencao.Tipo, intencao.DirecaoY, intencao.Sequencia);
        switch (_controle.Pedir(personagemId, pedido, _relogioMs(), out var acao))
        {
            case ResultadoDoPedido.Iniciado:
                _ = ExecutarAsync(personagemId, acao!);
                break;
            case ResultadoDoPedido.Enfileirado:
                break; // Começa no fim da ação em curso (EsperarFimAsync dela).
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
                _relogio,
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
                    PreparoMs = RegrasAtaque.PreparoMs(acao.Pedido.Tipo),
                }
            );

            _ = EsperarFimAsync(personagemId, acao);
            await Task.Delay(TimeSpan.FromMilliseconds(acao.PreparoMs), _parada.Token);
            await AcertarAsync(personagemId, acao);
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            _log.LogError(e, "Erro no ataque do personagem {Personagem}.", personagemId);
            Liberar(personagemId, acao, codigo: null);
        }
    }

    // No fim da ação, o pedido da fila (se houver) começa.
    private async Task EsperarFimAsync(long personagemId, AcaoDeAtaque acao)
    {
        try
        {
            var falta = acao.FimMs - _relogioMs();
            if (falta > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(falta), _parada.Token);
            }

            var proxima = _controle.AvancarFila(personagemId, acao.Id, _relogioMs());
            if (proxima != null)
            {
                _ = ExecutarAsync(personagemId, proxima);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            _log.LogError(e, "Erro ao passar a fila de ataques de {Personagem}.", personagemId);
        }
    }

    // O momento de acerto: o volume é medido agora, pela posição que o servidor tem.
    private async Task AcertarAsync(long personagemId, AcaoDeAtaque acao)
    {
        var forca = await RecursosDoAtaque.ForcaAsync(_escopos, personagemId, _parada.Token);
        if (
            !_controle.EstaAtiva(personagemId, acao.Id)
            || !_posicoes.TentarObter(personagemId, out var posicao)
        )
        {
            return;
        }

        var dano = RegrasAtaque.Dano(acao.Pedido.Tipo, forca);
        var vistos = new HashSet<long>();
        var acertos = new List<AcertoDeAtaque>();
        foreach (var fonte in _fontes)
        {
            var alvos = fonte.AlvosNoVolume(
                posicao.Area,
                posicao.X,
                posicao.Z,
                acao.Pedido.DirecaoY
            );
            foreach (var alvo in alvos)
            {
                // O mesmo golpe não acerta o mesmo alvo duas vezes.
                if (vistos.Add(alvo.Id) && fonte.TentarAplicarDano(alvo.Id, dano, out var r))
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
                }
            }
        }

        if (acertos.Count > 0)
        {
            EnviarParaArea(
                posicao.Area,
                personagemId,
                CombateMessageIds.AtaqueAcertou,
                new AtaqueAcertou { AtacanteId = personagemId, Acertos = acertos.ToArray() }
            );
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

    // Quem está na área (e o próprio atacante, mesmo se acabou de sair dela).
    private void EnviarParaArea<T>(string area, long atacanteId, ushort id, T mensagem)
    {
        var presentes = new HashSet<long>(_posicoes.PersonagensNaArea(area)) { atacanteId };
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
