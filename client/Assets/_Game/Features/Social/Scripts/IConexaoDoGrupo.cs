using System;
using JogoDoGrupo.Client.Core.TempoReal;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// O pedaço da conexão de tempo real que o grupo usa. Existe para os testes trocarem a rede
    /// por uma conexão falsa.
    /// </summary>
    internal interface IConexaoDoGrupo
    {
        bool Conectado { get; }

        bool Enviar<T>(ushort id, T mensagem, EntregaTempoReal entrega);

        IDisposable Assinar<T>(ushort id, Action<T> aoReceber);
    }

    /// <summary>Liga <see cref="IConexaoDoGrupo"/> à <see cref="ConexaoTempoReal"/> do Core.</summary>
    internal sealed class ConexaoDoGrupoReal : IConexaoDoGrupo
    {
        public bool Conectado => ConexaoTempoReal.Conectado;

        public bool Enviar<T>(ushort id, T mensagem, EntregaTempoReal entrega) =>
            ConexaoTempoReal.Enviar(id, mensagem, entrega);

        public IDisposable Assinar<T>(ushort id, Action<T> aoReceber) =>
            ConexaoTempoReal.Assinar(id, aoReceber);
    }
}
