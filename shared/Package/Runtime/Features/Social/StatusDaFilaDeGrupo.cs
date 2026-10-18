namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor → client: estado atual da inscrição na fila de dungeon.
    /// </summary>
    public class StatusDaFilaDeGrupo
    {
        /// <summary>Se o personagem está atualmente na fila.</summary>
        public bool EmFila { get; set; }

        /// <summary>Papel com o qual o personagem se inscreveu.</summary>
        public PapelNoGrupo Papel { get; set; }

        /// <summary>Quantidade de tanques prontos na fila (0 a 1).</summary>
        public int Tanques { get; set; }

        /// <summary>Quantidade de healers prontos na fila (0 a 1).</summary>
        public int Healers { get; set; }

        /// <summary>Quantidade de DPS prontos na fila (0 a 3).</summary>
        public int Dps { get; set; }

        /// <summary>Segundos decorridos desde que entrou na fila.</summary>
        public int SegundosNaFila { get; set; }
    }
}
