namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Client pede para começar ou parar de escalar. O servidor só aceita dentro de uma via
    /// marcada (<see cref="VolumesMarcados"/>) e responde com <see cref="EstadoDoMovimento"/>.
    /// Id: <see cref="PersonagemMessageIds.IntencaoDeEscalada"/>.
    /// </summary>
    [System.Serializable]
    public class IntencaoDeEscalada
    {
        /// <summary>Verdadeiro para começar a escalar, falso para soltar a via.</summary>
        public bool Escalar = false;
    }

    /// <summary>
    /// Servidor avisa o dono do estado de natação e escalada quando ele muda (ou quando um pedido
    /// de escalada é recusado). O client mostra e ajusta a previsão; quem decide é o servidor.
    /// Id: <see cref="PersonagemMessageIds.EstadoDoMovimento"/>.
    /// </summary>
    [System.Serializable]
    public class EstadoDoMovimento
    {
        public bool Nadando = false;
        public bool Escalando = false;

        /// <summary>A stamina não cobre o nado ou a escalada: nado lento, e a escalada termina.</summary>
        public bool SemStamina = false;
    }

    /// <summary>
    /// Servidor avisa o dono que uma queda tirou vida (o dano já foi aplicado por Combate).
    /// Id: <see cref="PersonagemMessageIds.DanoDeQueda"/>.
    /// </summary>
    [System.Serializable]
    public class DanoDeQueda
    {
        /// <summary>Vida tirada pela queda.</summary>
        public int Dano = 0;

        /// <summary>Altura da queda, em metros.</summary>
        public float Altura = 0f;
    }
}
