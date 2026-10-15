using System.Buffers.Binary;
using System.Net;
using System.Text.Json;
using JogoDoGrupo.Shared.Core.TempoReal;
using LiteNetLib;

namespace JogoDoGrupo.Robos;

/// <summary>
/// Uma conexão LiteNetLib de robô, no mesmo formato do client: 2 bytes com o ID (little-endian)
/// e o DTO do shared em JSON (campos públicos, nomes como no C#), como em docs/arquitetura.md.
/// Os pacotes chegam na thread da LiteNetLib e vão para <see cref="AoReceber"/>.
/// </summary>
public sealed class ConexaoDoRobo : IDisposable
{
    public static readonly JsonSerializerOptions Json = new()
    {
        IncludeFields = true,
        PropertyNamingPolicy = null,
    };

    private readonly NetManager _rede;
    private readonly TaskCompletionSource<NetPeer> _conectado = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private readonly TaskCompletionSource<DisconnectInfo> _desconectado = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private NetPeer? _peer;

    /// <summary>ID e pacote inteiro (com o cabeçalho). Roda na thread da LiteNetLib.</summary>
    public Action<ushort, byte[]>? AoReceber { get; set; }

    public Task<DisconnectInfo> Desconectado => _desconectado.Task;

    public ConexaoDoRobo()
    {
        var ouvinte = new EventBasedNetListener();
        ouvinte.PeerConnectedEvent += peer => _conectado.TrySetResult(peer);
        ouvinte.PeerDisconnectedEvent += (_, info) => _desconectado.TrySetResult(info);
        ouvinte.NetworkReceiveEvent += (_, leitor, _, _) =>
        {
            var pacote = leitor.GetRemainingBytes();
            var id =
                pacote.Length >= 2 ? BinaryPrimitives.ReadUInt16LittleEndian(pacote) : (ushort)0;
            AoReceber?.Invoke(id, pacote);
        };
        _rede = new NetManager(ouvinte)
        {
            UnsyncedEvents = true,
            AutoRecycle = true,
            IPv6Enabled = false,
            ChannelsCount = TransporteTempoReal.Canais,
        };
        _rede.Start();
    }

    public async Task ConectarAsync(string host, int porta, string chave, TimeSpan espera)
    {
        _rede.Connect(new IPEndPoint(IPAddress.Parse(host), porta), chave);
        await Task.WhenAny(_conectado.Task, _desconectado.Task).WaitAsync(espera);
        if (!_conectado.Task.IsCompleted)
        {
            throw new InvalidOperationException(
                $"conexão recusada ({_desconectado.Task.Result.Reason})"
            );
        }

        _peer = _conectado.Task.Result;
    }

    public void Enviar<T>(ushort id, T mensagem)
    {
        var corpo = JsonSerializer.SerializeToUtf8Bytes(mensagem, Json);
        var pacote = new byte[2 + corpo.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(pacote, id);
        corpo.CopyTo(pacote, 2);
        _peer!.Send(pacote, DeliveryMethod.ReliableOrdered);
    }

    /// <summary>Lê o corpo; <c>null</c> se o JSON não bate com o DTO.</summary>
    public static T? Ler<T>(byte[] pacote)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(pacote.AsSpan(2), Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Dispose() => _rede.Stop();
}
