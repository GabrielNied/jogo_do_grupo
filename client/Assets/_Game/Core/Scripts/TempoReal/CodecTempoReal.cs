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

        /// <summary>Monta o pacote. <paramref name="corpo"/> <c>null</c> vira pacote só com o Id.</summary>
        public static byte[] Codificar<T>(ushort id, T corpo)
        {
            var json = corpo == null ? "" : JsonUtility.ToJson(corpo);
            var bytesJson = Encoding.UTF8.GetBytes(json);
            var dados = new byte[TamanhoCabecalho + bytesJson.Length];
            dados[0] = (byte)id;
            dados[1] = (byte)(id >> 8);
            Buffer.BlockCopy(bytesJson, 0, dados, TamanhoCabecalho, bytesJson.Length);
            return dados;
        }

        /// <summary>Lê o Id e o JSON. Devolve <c>false</c> se o pacote for curto demais ou não for UTF-8.</summary>
        public static bool TentarDecodificar(byte[] dados, out MensagemTempoReal mensagem)
        {
            mensagem = default;
            if (dados == null || dados.Length < TamanhoCabecalho)
            {
                return false;
            }

            var id = (ushort)(dados[0] | (dados[1] << 8));
            try
            {
                var json = new UTF8Encoding(false, true).GetString(
                    dados,
                    TamanhoCabecalho,
                    dados.Length - TamanhoCabecalho
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
