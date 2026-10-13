using System.Buffers.Binary;
using System.Text.Json;

namespace JogoDoGrupo.Server.Core.TempoReal;

/// <summary>
/// Formato de cada pacote UDP: 2 bytes com o ID da mensagem (<c>ushort</c>, little-endian)
/// seguidos do corpo em JSON UTF-8, no mesmo formato da API HTTP e do <c>JsonUtility</c> do
/// Unity (campos públicos, nomes como no C#, enums como número).
/// </summary>
public static class ProtocoloTempoReal
{
    public const int TamanhoCabecalho = sizeof(ushort);

    public static readonly JsonSerializerOptions Json = new()
    {
        IncludeFields = true,
        PropertyNamingPolicy = null,
    };

    // L� tokens sem construir DTOs; inclui propriedades desconhecidas no limite.
    internal static bool CorpoValido(ReadOnlySpan<byte> pacote, int profundidadeMaxima)
    {
        try
        {
            var leitor = new Utf8JsonReader(
                pacote[TamanhoCabecalho..],
                new JsonReaderOptions { MaxDepth = profundidadeMaxima }
            );
            while (leitor.Read()) { }
            return leitor.BytesConsumed > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static byte[] Codificar<T>(ushort id, T mensagem)
    {
        var corpo = JsonSerializer.SerializeToUtf8Bytes(mensagem, Json);
        var pacote = new byte[TamanhoCabecalho + corpo.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(pacote, id);
        corpo.CopyTo(pacote, TamanhoCabecalho);
        return pacote;
    }

    /// <summary>Lê o ID do pacote. Devolve <c>false</c> se o pacote não tem nem o cabeçalho.</summary>
    public static bool TentarLerId(ReadOnlySpan<byte> pacote, out ushort id)
    {
        if (pacote.Length < TamanhoCabecalho)
        {
            id = 0;
            return false;
        }

        id = BinaryPrimitives.ReadUInt16LittleEndian(pacote);
        return true;
    }

    /// <summary>
    /// Lê o corpo de um pacote (sem o cabeçalho). Devolve <c>null</c> se o JSON for inválido
    /// ou vazio.
    /// </summary>
    public static T? LerCorpo<T>(ReadOnlySpan<byte> pacote)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(pacote[TamanhoCabecalho..], Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
