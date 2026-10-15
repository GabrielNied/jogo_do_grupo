using System;
using System.Text;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.TempoReal
{
    /// <summary>
    /// Formato de cada pacote de tempo real: 2 bytes com o Id da mensagem (<c>ushort</c>,
    /// little-endian, o mesmo de <c>NetDataWriter.Put(ushort)</c>) e, no resto, o DTO do shared em
    /// JSON UTF-8 (campos públicos com os nomes do C#, como na API HTTP). Sem corpo, o resto fica
    /// vazio. O servidor lê com <c>GetUShort()</c> e <c>GetRemainingBytes()</c>.
    /// </summary>
    internal static class CodecTempoReal
    {
        /// <summary>Bytes do cabeçalho (o Id).</summary>
        public const int TamanhoCabecalho = 2;

        private static readonly UTF8Encoding Utf8Estrito = new UTF8Encoding(false, true);
        private static readonly UTF8Encoding Utf8SemBom = new UTF8Encoding(false, false);

        [ThreadStatic]
        private static byte[] s_BufferEnvio;

        /// <summary>
        /// Codifica a mensagem em um buffer reutilizável da thread atual, devolvendo um <see cref="ArraySegment{byte}"/>.
        /// Usado para envio direto sem alocação de byte[].
        /// </summary>
        public static ArraySegment<byte> CodificarParaEnvio<T>(ushort id, T corpo)
        {
            var json = corpo == null ? "" : JsonUtility.ToJson(corpo);
            if (string.IsNullOrEmpty(json))
            {
                s_BufferEnvio ??= new byte[64];
                s_BufferEnvio[0] = (byte)id;
                s_BufferEnvio[1] = (byte)(id >> 8);
                return new ArraySegment<byte>(s_BufferEnvio, 0, TamanhoCabecalho);
            }

            var tamanhoNecessario = TamanhoCabecalho + Utf8SemBom.GetMaxByteCount(json.Length);
            if (s_BufferEnvio == null || s_BufferEnvio.Length < tamanhoNecessario)
            {
                s_BufferEnvio = new byte[Math.Max(tamanhoNecessario, 256)];
            }

            s_BufferEnvio[0] = (byte)id;
            s_BufferEnvio[1] = (byte)(id >> 8);
            var bytesEscritos = Utf8SemBom.GetBytes(
                json,
                0,
                json.Length,
                s_BufferEnvio,
                TamanhoCabecalho
            );
            return new ArraySegment<byte>(s_BufferEnvio, 0, TamanhoCabecalho + bytesEscritos);
        }

        /// <summary>Monta o pacote em um novo array. <paramref name="corpo"/> <c>null</c> vira pacote só com o Id.</summary>
        public static byte[] Codificar<T>(ushort id, T corpo)
        {
            var segmento = CodificarParaEnvio(id, corpo);
            var dados = new byte[segmento.Count];
            Buffer.BlockCopy(segmento.Array, segmento.Offset, dados, 0, segmento.Count);
            return dados;
        }

        /// <summary>Lê o Id e o JSON a partir de um segmento. Devolve <c>false</c> se o pacote for curto demais ou não for UTF-8.</summary>
        public static bool TentarDecodificar(
            ArraySegment<byte> dados,
            out MensagemTempoReal mensagem
        ) => TentarDecodificar(dados.Array, dados.Offset, dados.Count, out mensagem);

        /// <summary>Lê o Id e o JSON. Devolve <c>false</c> se o pacote for curto demais ou não for UTF-8.</summary>
        public static bool TentarDecodificar(byte[] dados, out MensagemTempoReal mensagem) =>
            TentarDecodificar(dados, 0, dados?.Length ?? 0, out mensagem);

        /// <summary>Lê o Id e o JSON com offset e contagem. Devolve <c>false</c> se o pacote for curto demais ou não for UTF-8.</summary>
        public static bool TentarDecodificar(
            byte[] dados,
            int offset,
            int count,
            out MensagemTempoReal mensagem
        )
        {
            mensagem = default;
            if (
                dados == null
                || count < TamanhoCabecalho
                || offset < 0
                || offset + count > dados.Length
            )
            {
                return false;
            }

            var id = (ushort)(dados[offset] | (dados[offset + 1] << 8));
            try
            {
                var json = Utf8Estrito.GetString(
                    dados,
                    offset + TamanhoCabecalho,
                    count - TamanhoCabecalho
                );
                mensagem = new MensagemTempoReal(id, json);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
