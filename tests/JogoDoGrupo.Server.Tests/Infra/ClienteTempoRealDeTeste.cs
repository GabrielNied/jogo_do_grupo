using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Threading.Channels;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Core.TempoReal;
using LiteNetLib;

namespace JogoDoGrupo.Server.Tests.Infra;

/// <summary>
/// Client UDP de verdade (LiteNetLib) para os testes do tempo real. Guarda as mensagens que
/// chegam numa fila; <see cref="ReceberAsync{T}"/> espera a próxima com o ID pedido.
/// </summary>
public sealed class ClienteTempoRealDeTeste : IDisposable
{
    /// <summary>
    /// Limite das esperas por mensagem ou condição. Folgado de propósito: só pesa quando o teste já
    /// falhou, e evita falso erro com a máquina carregada (testes em paralelo, CI).
    /// </summary>
    public static readonly TimeSpan Espera = TimeSpan.FromSeconds(10);

    private readonly NetManager _rede;
    private readonly Channel<byte[]> _recebidos = Channel.CreateUnbounded<byte[]>();
    private readonly ConcurrentQueue<ushort> _ids = new();
    private readonly TaskCompletionSource<NetPeer> _conectado = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private readonly TaskCompletionSource<DisconnectInfo> _desconectado = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    public ClienteTempoRealDeTeste()
    {
        var ouvinte = new EventBasedNetListener();
        ouvinte.PeerConnectedEvent += peer => _conectado.TrySetResult(peer);
        ouvinte.PeerDisconnectedEvent += (_, info) => _desconectado.TrySetResult(info);
        ouvinte.NetworkReceiveEvent += (_, leitor, _, _) =>
        {
            var pacote = leitor.GetRemainingBytes();
            if (pacote.Length >= 2)
            {
                _ids.Enqueue(BinaryPrimitives.ReadUInt16LittleEndian(pacote));
            }

            _recebidos.Writer.TryWrite(pacote);
        };
        _rede = new NetManager(ouvinte)
        {
            UnsyncedEvents = true,
            AutoRecycle = true,
            IPv6Enabled = false,
            // Deve bater com o servidor (ChannelsCount = 32), senão pacotes sequenciados por
            // entidade em canais > 0 são descartados em silêncio pelo LiteNetLib.
            ChannelsCount = 32,
            // Ping frequente: o servidor de teste considera queda depois de 1,5 s em silêncio.
            PingInterval = 200,
        };
        _rede.Start();
    }

    /// <summary>
    /// Os IDs de tudo que já chegou, na ordem (mesmo o que <see cref="ReceberAsync{T}"/>
    /// descartou). Serve para conferir que uma mensagem <b>não</b> chegou.
    /// </summary>
    public IReadOnlyList<ushort> IdsRecebidos => _ids.ToArray();

    /// <summary>Completa quando o servidor desconecta (ou a conexão cai).</summary>
    public Task<DisconnectInfo> Desconectado => _desconectado.Task;

    public static async Task<ClienteTempoRealDeTeste> ConectarAsync(
        int porta,
        string chave = "JogoDoGrupo"
    )
    {
        var cliente = new ClienteTempoRealDeTeste();
        cliente._rede.Connect("127.0.0.1", porta, chave);
        await Task.WhenAny(cliente._conectado.Task, cliente._desconectado.Task).WaitAsync(Espera);
        if (!cliente._conectado.Task.IsCompleted)
        {
            var motivo = cliente._desconectado.Task.Result.Reason;
            cliente.Dispose();
            throw new InvalidOperationException($"Conexão recusada: {motivo}.");
        }

        return cliente;
    }

    /// <summary>Conecta e autentica; devolve o cliente e a resposta do servidor.</summary>
    public static async Task<(
        ClienteTempoRealDeTeste Cliente,
        ResultadoAutenticacaoTempoReal Resultado
    )> AutenticarAsync(int porta, string token, long personagemId)
    {
        var cliente = await ConectarAsync(porta);
        cliente.Enviar(
            TempoRealIds.AutenticarTempoReal,
            new AutenticarTempoReal { Token = token, PersonagemId = personagemId }
        );
        var resultado = await cliente.ReceberAsync<ResultadoAutenticacaoTempoReal>(
            TempoRealIds.ResultadoAutenticacaoTempoReal
        );
        return (cliente, resultado);
    }

    public void Enviar<T>(ushort id, T mensagem) =>
        EnviarBruto(ProtocoloTempoReal.Codificar(id, mensagem));

    public void EnviarBruto(byte[] pacote) =>
        _conectado.Task.Result.Send(pacote, DeliveryMethod.ReliableOrdered);

    /// <summary>Espera a próxima mensagem com <paramref name="id"/>; descarta as outras.</summary>
    public async Task<T> ReceberAsync<T>(ushort id)
        where T : class
    {
        using var limite = new CancellationTokenSource(Espera);
        while (true)
        {
            var pacote = await _recebidos.Reader.ReadAsync(limite.Token);
            if (BinaryPrimitives.ReadUInt16LittleEndian(pacote) == id)
            {
                return ProtocoloTempoReal.LerCorpo<T>(pacote)
                    ?? throw new InvalidOperationException($"Corpo inválido na mensagem {id}.");
            }
        }
    }

    /// <summary>Fecha a conexão avisando o servidor.</summary>
    public void Sair() => _rede.DisconnectAll();

    /// <summary>Some sem avisar, como um client que travou ou perdeu a rede.</summary>
    public void Cair() => _rede.Stop(false);

    public void Dispose() => _rede.Stop();
}
