namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor envia a lista completa e atual do grupo a cada mudança (nunca só a diferença).
    /// Lista vazia quer dizer que o personagem não está em grupo.
    /// Id: <see cref="SocialMessageIds.ListaDoGrupo"/>.
    /// </summary>
    [System.Serializable]
    public class ListaDoGrupo
    {
        /// <summary>Gerado pelo servidor; zero quando não há grupo.</summary>
        public long GrupoId = 0;

        /// <summary>Id do personagem líder; zero quando não há grupo.</summary>
        public long LiderId = 0;

        /// <summary>Membros em ordem de entrada no grupo. Nunca nulo.</summary>
        public MembroDoGrupo[] Membros = System.Array.Empty<MembroDoGrupo>();
    }
}
