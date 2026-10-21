namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor → client: estado atual da inscrição na fila de dungeon.
    /// </summary>
    [System.Serializable]
    public class StatusDaFilaDeGrupo
    {
        /// <summary>Se o personagem está atualmente na fila.</summary>
        public bool EmFila;

        /// <summary>Papel com o qual o personagem se inscreveu.</summary>
        public PapelNoGrupo Papel;

        /// <summary>Quantidade de tanques prontos na fila (0 a 1).</summary>
        public int Tanques;

        /// <summary>Quantidade de healers prontos na fila (0 a 1).</summary>
        public int Healers;

        /// <summary>Quantidade de DPS prontos na fila (0 a 3).</summary>
        public int Dps;

        /// <summary>Segundos decorridos desde que entrou na fila.</summary>
        public int SegundosNaFila;
    }
}
