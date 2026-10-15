using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using JogoDoGrupo.Shared.Core;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.TempoReal;
using LiteNetLib;

namespace JogoDoGrupo.Server.Core.TempoReal;

internal sealed class AgendamentoTempoReal(
    TimeSpan intervalo,
    Action<TimeSpan> acao,
    long proximoEmMs
)
{
    public TimeSpan Intervalo { get; } = intervalo;
    public Action<TimeSpan> Acao { get; } = acao;
    public long ProximoEmMs { get; set; } = proximoEmMs;

    public bool AtualizarSeVencido(long agoraMs)
    {
        if (agoraMs < ProximoEmMs)
        {
            return false;
        }

        Acao(Intervalo);
        ProximoEmMs += (long)Intervalo.TotalMilliseconds;
        return true;
    }
}

/// <summary>Como o pacote viaja.</summary>
public enum EntregaTempoReal
{
    /// <summary>Chega sempre e na ordem (entrar na cidade, alguém saiu).</summary>
    Confiavel,

    /// <summary>Pode se perder; um pacote velho que chega depois de um novo é descartado (posição).</summary>
    Sequenciada,
}

/// <summary>
/// Host UDP do tempo real (LiteNetLib, ADR 0015). Sobe junto com o app. Cada conexão vira uma
/// <see cref="SessaoTempoReal"/>; ela só passa às funcionalidades depois de se autenticar com o
/// JWT do login e um personagem da conta. Uma conta tem no máximo uma sessão: autenticar de
/// novo encerra a anterior.
/// <para>
/// Recebimento, manipuladores e eventos rodam num só laço (um de cada vez). Os métodos públicos
/// de envio e <see cref="Desconectar"/> podem ser chamados de qualquer thread.
/// </para>
/// </summary>
public sealed class ServidorTempoReal : BackgroundService
{
    private const int IntervaloMs = 10;
    private const int LimiteDuracaoPassoMs = 100;

    private readonly ConfiguracaoTempoReal _configuracao;
    private readonly MensagensTempoReal _mensagens;
    private readonly AutenticadorTempoReal _autenticador;
    private readonly TimeProvider _relogio;
    private readonly ILogger<ServidorTempoReal> _log;
    private readonly NetManager _rede;

    // Só o laço mexe nestes dois.
    private readonly Dictionary<int, SessaoTempoReal> _conexoes = [];
    private readonly Dictionary<long, SessaoTempoReal> _porConta = [];
    private readonly Dictionary<long, SessaoTempoReal> _porPersonagem = [];

    private readonly ConcurrentDictionary<SessaoTempoReal, byte> _autenticadas = new();
    private readonly ConcurrentQueue<Action> _pendentes = new();
    private readonly List<AgendamentoTempoReal> _passos = [];
    private readonly object _travaPassos = new();
    private CancellationToken _parada;
    private int _parado;

    internal ServidorTempoReal(
        ConfiguracaoTempoReal configuracao,
        MensagensTempoReal mensagens,
        AutenticadorTempoReal autenticador,
        TimeProvider relogio,
        ILogger<ServidorTempoReal> log
    )
    {
        _configuracao = configuracao;
        _mensagens = mensagens;
        _autenticador = autenticador;
        _relogio = relogio;
        _log = log;

        var ouvinte = new EventBasedNetListener();
        ouvinte.ConnectionRequestEvent += pedido =>
        {
            var pendentes = _conexoes.Values.Count(s =>
                !s.Autenticada && s.Peer.Address.Equals(pedido.RemoteEndPoint.Address)
            );
            if (pendentes >= _configuracao.MaximoConexoesPendentesPorIp)
            {
                pedido.Reject();
                return;
            }
            var peer = pedido.AcceptIfKey(_configuracao.ChaveConexao);
            if (peer != null)
                _conexoes[peer.Id] = new SessaoTempoReal(
                    peer,
                    _configuracao.MensagensPorSegundo,
                    _configuracao.RajadaMensagens,
                    _relogio
                );
        };
        ouvinte.PeerDisconnectedEvent += (peer, _) =>
        {
            if (_conexoes.TryGetValue(peer.Id, out var sessao) && sessao.Peer == peer)
            {
                Encerrar(sessao, desconectar: false);
            }
        };
        ouvinte.NetworkReceiveEvent += (peer, leitor, _, _) =>
        {
            if (_conexoes.TryGetValue(peer.Id, out var sessao) && sessao.Peer == peer)
            {
                if (leitor.AvailableBytes > _configuracao.TamanhoMaximoPacote)
                {
                    Recusar(sessao, 0, MotivoRecusa.PacoteInvalido);
                    return;
                }
                Receber(sessao, leitor.GetRemainingBytes());
            }
        };

        _rede = new NetManager(ouvinte)
        {
            AutoRecycle = true,
            IPv6Enabled = false,
            DisconnectTimeout = _configuracao.TempoQuedaMs,
            ChannelsCount = TransporteTempoReal.Canais,
        };
    }

    /// <summary>Porta UDP em que o servidor escuta (a real, mesmo com <c>TempoReal:Porta</c> 0).</summary>
    public int Porta => _rede.LocalPort;

    /// <summary>Duração e ritmo dos últimos passos do laço (o "tick"), para as métricas.</summary>
    public MedidorDePassos Passos { get; } = new();

    /// <summary>Sessões autenticadas neste momento, em uma coleção sem alocar cópias.</summary>
    public ICollection<SessaoTempoReal> Sessoes => _autenticadas.Keys;

    public bool TentarObterSessao(long personagemId, out SessaoTempoReal? sessao) =>
        _porPersonagem.TryGetValue(personagemId, out sessao);

    /// <summary>
    /// Agenda uma ação para rodar novamente no laço principal do servidor, em um passo fixo.
    /// A ação recebe a duração do intervalo e fica em ordem de inscrição.
    /// </summary>
    public void AoPasso(TimeSpan intervalo, Action<TimeSpan> acao)
    {
        ArgumentNullException.ThrowIfNull(acao);
        if (intervalo <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalo));
        }

        lock (_travaPassos)
        {
            _passos.Add(new AgendamentoTempoReal(intervalo, acao, AgoraMs()));
        }
    }

    /// <summary>Enfileira uma ação para executar pelo laço principal após trabalho assíncrono.</summary>
    public void NoLaco(Action acao)
    {
        ArgumentNullException.ThrowIfNull(acao);
        _pendentes.Enqueue(acao);
    }

    /// <summary>Uma sessão acabou de se autenticar. Roda no laço de rede.</summary>
    public event Action<SessaoTempoReal>? SessaoAutenticada;

    /// <summary>
    /// Uma sessão autenticada acabou (saiu, caiu, foi substituída ou desconectada). Roda no laço
    /// de rede; <c>ContaId</c> e <c>PersonagemId</c> ainda estão preenchidos.
    /// </summary>
    public event Action<SessaoTempoReal>? SessaoEncerrada;

    /// <summary>Uma mensagem foi descartada. Roda no laço de rede.</summary>
    public event Action<SessaoTempoReal, ushort, MotivoRecusa>? MensagemRecusada;

    /// <summary>Envia a uma sessão. Devolve <c>false</c> se ela já não está conectada.</summary>
    public bool Enviar<T>(
        SessaoTempoReal sessao,
        ushort id,
        T mensagem,
        EntregaTempoReal entrega = EntregaTempoReal.Confiavel,
        long? entidadeId = null
    )
    {
        if (sessao.Peer.ConnectionState != ConnectionState.Connected)
        {
            return false;
        }

        var pacote = ProtocoloTempoReal.Codificar(id, mensagem);
        var metodo = Metodo(entrega);
        if (entrega == EntregaTempoReal.Sequenciada && entidadeId is { } entidade)
        {
            sessao.Peer.Send(pacote, CanalParaEntidade(entidade, _rede.ChannelsCount), metodo);
            return true;
        }

        sessao.Peer.Send(pacote, metodo);
        return true;
    }

    public void Enviar<T>(
        IEnumerable<SessaoTempoReal> sessoes,
        ushort id,
        T mensagem,
        EntregaTempoReal entrega = EntregaTempoReal.Confiavel,
        long? entidadeId = null
    )
    {
        var pacote = ProtocoloTempoReal.Codificar(id, mensagem);
        var metodo = Metodo(entrega);
        var canal =
            entrega == EntregaTempoReal.Sequenciada && entidadeId is { } entidade
                ? CanalParaEntidade(entidade, _rede.ChannelsCount)
                : (byte)0;

        foreach (var sessao in sessoes)
        {
            if (sessao.Peer.ConnectionState != ConnectionState.Connected)
            {
                continue;
            }

            if (entrega == EntregaTempoReal.Sequenciada && entidadeId is not null)
            {
                sessao.Peer.Send(pacote, canal, metodo);
                continue;
            }

            sessao.Peer.Send(pacote, metodo);
        }
    }

    /// <summary>Envia a todas as sessões autenticadas, menos <paramref name="exceto"/>.</summary>
    public void EnviarParaTodos<T>(
        ushort id,
        T mensagem,
        EntregaTempoReal entrega = EntregaTempoReal.Confiavel,
        SessaoTempoReal? exceto = null,
        long? entidadeId = null
    )
    {
        var pacote = ProtocoloTempoReal.Codificar(id, mensagem);
        var metodo = Metodo(entrega);
        var canal =
            entrega == EntregaTempoReal.Sequenciada && entidadeId is { } entidade
                ? CanalParaEntidade(entidade, _rede.ChannelsCount)
                : (byte)0;

        foreach (var sessao in _autenticadas.Keys)
        {
            if (sessao == exceto || sessao.Peer.ConnectionState != ConnectionState.Connected)
            {
                continue;
            }

            if (entrega == EntregaTempoReal.Sequenciada && entidadeId is not null)
            {
                sessao.Peer.Send(pacote, canal, metodo);
                continue;
            }

            sessao.Peer.Send(pacote, metodo);
        }
    }

    /// <summary>Encerra a sessão (no próximo passo do laço) e desconecta o client.</summary>
    public void Desconectar(SessaoTempoReal sessao) =>
        _pendentes.Enqueue(() => Encerrar(sessao, desconectar: true));

    public override Task StartAsync(CancellationToken cancelamento)
    {
        var endereco = IPAddress.Parse(_configuracao.Endereco);
        if (!_rede.Start(endereco, IPAddress.IPv6Any, _configuracao.Porta))
        {
            throw new InvalidOperationException(
                $"Não foi possível abrir a porta UDP {_configuracao.Porta} do tempo real."
            );
        }

        _log.LogInformation("Tempo real escutando em UDP {Endereco}:{Porta}.", endereco, Porta);
        return base.StartAsync(cancelamento);
    }

    public override async Task StopAsync(CancellationToken cancelamento)
    {
        await base.StopAsync(cancelamento);
        // O host pode pedir a parada duas vezes ao mesmo tempo (ex.: no WebApplicationFactory).
        if (Interlocked.Exchange(ref _parado, 1) == 0 && _rede.IsRunning)
        {
            _rede.Stop();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        _parada = parada;
        while (!parada.IsCancellationRequested)
        {
            Passo();
            try
            {
                await Task.Delay(IntervaloMs, parada);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void Passo()
    {
        var inicio = Stopwatch.GetTimestamp();
        _rede.PollEvents();
        while (_pendentes.TryDequeue(out var acao))
        {
            acao();
        }

        ProcessarPassos();
        ExpirarNaoAutenticadas();

        var duracao = Stopwatch.GetElapsedTime(inicio);
        Passos.Registrar(inicio, duracao);
        if (duracao > TimeSpan.FromMilliseconds(LimiteDuracaoPassoMs))
        {
            _log.LogWarning(
                "Tempo real: passo do laço demorou {Duracao}ms, acima do limite de {Limite}ms.",
                duracao.TotalMilliseconds,
                LimiteDuracaoPassoMs
            );
        }
    }

    private void ProcessarPassos()
    {
        var agora = AgoraMs();
        List<AgendamentoTempoReal> vencidos;
        lock (_travaPassos)
        {
            vencidos = _passos.Where(p => agora >= p.ProximoEmMs).ToList();
        }

        foreach (var agendamento in vencidos)
        {
            try
            {
                agendamento.AtualizarSeVencido(agora);
            }
            catch (Exception erro)
            {
                _log.LogError(
                    erro,
                    "Erro no tick do tempo real agendado em {Intervalo}.",
                    agendamento.Intervalo
                );
            }
        }
    }

    private void Receber(SessaoTempoReal sessao, byte[] pacote)
    {
        if (!sessao.Limite.Consumir(AgoraMs()))
        {
            DerrubarPorExcesso(sessao, pacote);
            return;
        }

        if (!ProtocoloTempoReal.TentarLerId(pacote, out var id))
        {
            Recusar(sessao, 0, MotivoRecusa.PacoteInvalido);
            return;
        }

        if (!sessao.Autenticada)
        {
            ReceberAntesDeAutenticar(sessao, id, pacote);
            return;
        }

        if (!ProtocoloTempoReal.CorpoValido(pacote, _configuracao.ProfundidadeMaximaJson))
        {
            Recusar(sessao, id, MotivoRecusa.CorpoInvalido);
            return;
        }

        if (CoreMessageIds.Faixa.Contem(id))
        {
            // AutenticarTempoReal de quem já entrou, ou ID do Core sem uso.
            Recusar(sessao, id, MotivoRecusa.SemManipulador);
            return;
        }

        MotivoRecusa? motivo;
        try
        {
            motivo = _mensagens.Despachar(sessao, id, pacote);
        }
        catch (Exception e)
        {
            _log.LogError(
                e,
                "Erro ao tratar a mensagem {Id} da conta {Conta}.",
                id,
                sessao.ContaId
            );
            if (++sessao.Excecoes > _configuracao.MaximoExcecoesPorSessao)
                Encerrar(sessao, desconectar: true);
            return;
        }

        if (motivo is { } recusa)
        {
            Recusar(sessao, id, recusa);
        }
    }

    // Proteção contra trapaça (#211): quem inunda o servidor perde a conexão na hora.
    private void DerrubarPorExcesso(SessaoTempoReal sessao, byte[] pacote)
    {
        ProtocoloTempoReal.TentarLerId(pacote, out var id);
        _log.LogWarning(
            "Tempo real: conexão {Conexao} (conta {Conta}) passou de {Limite} mensagens/s e foi derrubada.",
            sessao.Id,
            sessao.ContaId,
            _configuracao.MensagensPorSegundo
        );
        Recusar(sessao, id, MotivoRecusa.LimiteExcedido);
        Encerrar(sessao, desconectar: true);
    }

    private void ReceberAntesDeAutenticar(SessaoTempoReal sessao, ushort id, byte[] pacote)
    {
        if (id != TempoRealIds.AutenticarTempoReal)
        {
            Recusar(sessao, id, MotivoRecusa.NaoAutenticada);
            return;
        }

        if (++sessao.TentativasAutenticacao > _configuracao.MaximoTentativasAutenticacao)
        {
            Encerrar(sessao, desconectar: true);
            return;
        }
        if (sessao.AutenticacaoEmAndamento)
        {
            return;
        }

        var pedido = ProtocoloTempoReal.CorpoValido(pacote, _configuracao.ProfundidadeMaximaJson)
            ? ProtocoloTempoReal.LerCorpo<AutenticarTempoReal>(pacote)
            : null;
        if (pedido == null)
        {
            Recusar(sessao, id, MotivoRecusa.CorpoInvalido);
            ResponderAutenticacao(sessao, CodigosErro.NaoAutenticado);
            return;
        }

        sessao.AutenticacaoEmAndamento = true;
        _ = AutenticarAsync(sessao, pedido);
    }

    // Validar o token e consultar o banco ficam fora do laço; o resultado volta para ele.
    private async Task AutenticarAsync(SessaoTempoReal sessao, AutenticarTempoReal pedido)
    {
        ResultadoAutenticacao resultado;
        try
        {
            resultado = await _autenticador.AutenticarAsync(pedido, _parada);
        }
        catch (Exception e)
        {
            if (!_parada.IsCancellationRequested)
            {
                _log.LogError(e, "Erro ao autenticar a conexão de tempo real.");
            }

            resultado = new(CodigosErro.ErroInterno, 0, 0);
        }

        _pendentes.Enqueue(() => ConcluirAutenticacao(sessao, resultado));
    }

    private void ConcluirAutenticacao(SessaoTempoReal sessao, ResultadoAutenticacao resultado)
    {
        sessao.AutenticacaoEmAndamento = false;
        if (!EstaConectada(sessao))
        {
            return;
        }

        if (!resultado.Aceita)
        {
            // A conexão continua pendente: pode tentar de novo até o prazo de autenticar.
            ResponderAutenticacao(sessao, resultado.Codigo);
            return;
        }

        if (_porConta.TryGetValue(resultado.ContaId, out var anterior))
        {
            Encerrar(anterior, desconectar: true);
        }

        if (_porPersonagem.TryGetValue(resultado.PersonagemId, out var daMesmaPessoa))
        {
            Encerrar(daMesmaPessoa, desconectar: true);
        }

        sessao.ContaId = resultado.ContaId;
        sessao.PersonagemId = resultado.PersonagemId;
        sessao.Autenticada = true;
        _porConta[sessao.ContaId] = sessao;
        _porPersonagem[sessao.PersonagemId] = sessao;
        _autenticadas[sessao] = 0;

        ResponderAutenticacao(sessao, "");
        _log.LogInformation(
            "Tempo real: conta {Conta} entrou com o personagem {Personagem}.",
            sessao.ContaId,
            sessao.PersonagemId
        );
        Avisar(SessaoAutenticada, sessao);
    }

    private void ResponderAutenticacao(SessaoTempoReal sessao, string codigo) =>
        Enviar(
            sessao,
            TempoRealIds.ResultadoAutenticacaoTempoReal,
            new ResultadoAutenticacaoTempoReal { Aceita = codigo.Length == 0, Codigo = codigo }
        );

    private void ExpirarNaoAutenticadas()
    {
        var agora = AgoraMs();
        List<SessaoTempoReal>? expiradas = null;
        foreach (var sessao in _conexoes.Values)
        {
            if (
                !sessao.Autenticada
                && agora - sessao.ConectadaEm > _configuracao.TempoParaAutenticarMs
            )
            {
                (expiradas ??= []).Add(sessao);
            }
        }

        foreach (var sessao in expiradas ?? [])
        {
            Encerrar(sessao, desconectar: true);
        }
    }

    private void Encerrar(SessaoTempoReal sessao, bool desconectar)
    {
        if (!EstaConectada(sessao))
        {
            return;
        }

        _conexoes.Remove(sessao.Id);
        if (desconectar)
        {
            _rede.DisconnectPeer(sessao.Peer);
        }

        if (!sessao.Autenticada)
        {
            return;
        }

        _autenticadas.TryRemove(sessao, out _);
        if (_porConta.TryGetValue(sessao.ContaId, out var daConta) && daConta == sessao)
        {
            _porConta.Remove(sessao.ContaId);
        }
        if (
            _porPersonagem.TryGetValue(sessao.PersonagemId, out var doPersonagem)
            && doPersonagem == sessao
        )
        {
            _porPersonagem.Remove(sessao.PersonagemId);
        }

        Avisar(SessaoEncerrada, sessao);
    }

    private bool EstaConectada(SessaoTempoReal sessao) =>
        _conexoes.TryGetValue(sessao.Id, out var atual) && atual == sessao;

    private void Recusar(SessaoTempoReal sessao, ushort id, MotivoRecusa motivo)
    {
        _log.LogDebug(
            "Mensagem {Id} recusada ({Motivo}) da conexão {Conexao}.",
            id,
            motivo,
            sessao.Id
        );
        if (++sessao.Recusas > _configuracao.MaximoRecusasPorSessao)
            Encerrar(sessao, desconectar: true);
        if (MensagemRecusada == null)
        {
            return;
        }

        foreach (var ouvinte in MensagemRecusada.GetInvocationList())
        {
            try
            {
                ((Action<SessaoTempoReal, ushort, MotivoRecusa>)ouvinte)(sessao, id, motivo);
            }
            catch (Exception e)
            {
                _log.LogError(e, "Erro num ouvinte de MensagemRecusada.");
            }
        }
    }

    private void Avisar(Action<SessaoTempoReal>? evento, SessaoTempoReal sessao)
    {
        if (evento == null)
        {
            return;
        }

        foreach (var ouvinte in evento.GetInvocationList())
        {
            try
            {
                ((Action<SessaoTempoReal>)ouvinte)(sessao);
            }
            catch (Exception e)
            {
                _log.LogError(e, "Erro num ouvinte de sessão do tempo real.");
            }
        }
    }

    private long AgoraMs() => _relogio.GetUtcNow().ToUnixTimeMilliseconds();

    private static byte CanalParaEntidade(long entidadeId, int canais)
    {
        if (canais <= 1)
        {
            return 0;
        }

        return (byte)((ulong)entidadeId % (uint)canais);
    }

    private static DeliveryMethod Metodo(EntregaTempoReal entrega) =>
        entrega == EntregaTempoReal.Sequenciada
            ? DeliveryMethod.Sequenced
            : DeliveryMethod.ReliableOrdered;
}
