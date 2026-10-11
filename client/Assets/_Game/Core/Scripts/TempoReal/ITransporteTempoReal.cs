using System;

namespace JogoDoGrupo.Client.Core.TempoReal
{
    /// <summary>
    /// O que o <see cref="ClienteTempoReal"/> precisa do transporte UDP. Na rede é o
    /// <see cref="TransporteLiteNetLib"/>; nos testes, um transporte falso.
    /// </summary>
    internal interface ITransporteTempoReal
    {
        /// <summary>A conexão abriu.</summary>
        event Action Conectou;

        /// <summary>
        /// A conexão caiu ou não abriu (com o motivo, para log). Não dispara depois de
        /// <see cref="Desconectar"/>.
        /// </summary>
        event Action<string> Caiu;

        /// <summary>Chegou um pacote do servidor.</summary>
        event Action<byte[]> Recebeu;

        void Conectar(string host, int porta, string chave);

        void Enviar(byte[] dados, EntregaTempoReal entrega);

        /// <summary>Fecha a conexão sem disparar <see cref="Caiu"/>.</summary>
        void Desconectar();

        /// <summary>Dispara os eventos pendentes. Chamado a cada frame, na thread principal.</summary>
        void Processar();
    }
}
