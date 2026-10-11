using System;
using JogoDoGrupo.Client.Core.TempoReal;

namespace JogoDoGrupo.Client.Features.Mundo.Presenca
{
    /// <summary>
    /// O pedaço da conexão de tempo real que a presença usa. Existe para os testes trocarem a
    /// rede por uma conexão falsa.
    /// </summary>
    public interface IConexaoDePresenca
    {
        bool Conectado { get; }

        event Action<EstadoTempoReal> EstadoMudou;

        bool Enviar<T>(ushort id, T mensagem, EntregaTempoReal entrega);

        IDisposable Assinar<T>(ushort id, Action<T> aoReceber);
    }

    /// <summary>Liga <see cref="IConexaoDePresenca"/> à <see cref="ConexaoTempoReal"/> do Core.</summary>
    public sealed class ConexaoDePresencaReal : IConexaoDePresenca
    {
        public bool Conectado => ConexaoTempoReal.Conectado;

        public event Action<EstadoTempoReal> EstadoMudou
        {
            add => ConexaoTempoReal.EstadoMudou += value;
            remove => ConexaoTempoReal.EstadoMudou -= value;
        }

        public bool Enviar<T>(ushort id, T mensagem, EntregaTempoReal entrega) =>
            ConexaoTempoReal.Enviar(id, mensagem, entrega);

        public IDisposable Assinar<T>(ushort id, Action<T> aoReceber) =>
            ConexaoTempoReal.Assinar(id, aoReceber);
    }
}
