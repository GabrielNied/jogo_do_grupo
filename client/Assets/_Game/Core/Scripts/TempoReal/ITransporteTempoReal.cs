using System;

namespace JogoDoGrupo.Client.Core.TempoReal
{
    /// <summary>
    /// O que o <see cref="ClienteTempoReal"/> precisa do transporte UDP. Na rede é o
    /// <see cref="TransporteLiteNetLib"/>; nos testes, um transporte falso.
    /// </summary>
    internal interface ITransporteTempoReal
    {
        /// <summary>Ping do servidor em milissegundos, ou -1 sem conexão.</summary>
        int Latencia { get; }

        /// <summary>A conexão abriu.</summary>
        event Action Conectou;

        /// <summary>
        /// A conexão caiu ou não abriu (com o motivo, para log). Não dispara depois de
        /// <see cref="Desconectar"/>.
        /// </summary>
        event Action<string> Caiu;

        /// <summary>Chegou um pacote do servidor.</summary>
        event Action<ArraySegment<byte>> Recebeu;

        void Conectar(string host, int porta, string chave);

        void Enviar(ArraySegment<byte> dados, EntregaTempoReal entrega);

        void Enviar(byte[] dados, EntregaTempoReal entrega);

        /// <summary>Fecha a conexão sem disparar <see cref="Caiu"/>.</summary>
        void Desconectar();

        /// <summary>Dispara os eventos pendentes. Chamado a cada frame, na thread principal.</summary>
        void Processar();
    }
}
