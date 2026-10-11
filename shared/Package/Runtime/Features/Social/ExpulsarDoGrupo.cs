namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// O líder pede para expulsar um membro. O id vem de <see cref="ListaDoGrupo.Membros"/>.
    /// Id: <see cref="SocialMessageIds.ExpulsarDoGrupo"/>.
    /// </summary>
    [System.Serializable]
    public class ExpulsarDoGrupo
    {
        public long PersonagemId = 0;
    }
}
