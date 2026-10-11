namespace JogoDoGrupo.Client.Core.TempoReal
{
    /// <summary>Como uma mensagem de tempo real é entregue.</summary>
    public enum EntregaTempoReal
    {
        /// <summary>Chega sempre e na ordem. Use para intenções e eventos (entrar, sair, usar item).</summary>
        Confiavel,

        /// <summary>
        /// Pode se perder, mas nunca chega uma mais velha depois de uma mais nova. Use para estado
        /// que se repete o tempo todo (posição, direção).
        /// </summary>
        Sequenciada,

        /// <summary>Pode se perder e chegar fora de ordem.</summary>
        NaoConfiavel,
    }
}
