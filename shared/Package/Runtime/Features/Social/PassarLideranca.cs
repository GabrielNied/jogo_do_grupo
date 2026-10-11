namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// O líder pede para passar a liderança a outro membro online. O id vem de
    /// <see cref="ListaDoGrupo.Membros"/>.
    /// Id: <see cref="SocialMessageIds.PassarLideranca"/>.
    /// </summary>
    [System.Serializable]
    public class PassarLideranca
    {
        public long PersonagemId = 0;
    }
}
