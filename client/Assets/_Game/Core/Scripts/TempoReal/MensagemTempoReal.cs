using UnityEngine;

namespace JogoDoGrupo.Client.Core.TempoReal
{
    /// <summary>
    /// Mensagem recebida do servidor: o Id (da faixa de alguma funcionalidade) e o corpo em JSON.
    /// Leia o corpo com <see cref="Ler{T}"/> usando o DTO do shared que corresponde ao Id.
    /// </summary>
    public readonly struct MensagemTempoReal
    {
        public ushort Id { get; }

        /// <summary>Corpo em JSON (vazio se a mensagem não tem corpo).</summary>
        public string Json { get; }

        public MensagemTempoReal(ushort id, string json)
        {
            Id = id;
            Json = json ?? "";
        }

        /// <summary>
        /// Converte o corpo no DTO <typeparamref name="T"/>. Sem corpo, devolve um
        /// <typeparamref name="T"/> com os valores padrão. JSON inválido lança <c>ArgumentException</c>.
        /// </summary>
        public T Ler<T>() => JsonUtility.FromJson<T>(string.IsNullOrEmpty(Json) ? "{}" : Json);
    }
}
