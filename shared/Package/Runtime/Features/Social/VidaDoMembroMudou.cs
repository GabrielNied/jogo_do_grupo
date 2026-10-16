namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor avisa aos membros do grupo que a vida de um membro mudou.
    /// Id: <see cref="SocialMessageIds.VidaDoMembroMudou"/>.
    /// </summary>
    [System.Serializable]
    public class VidaDoMembroMudou
    {
        public long PersonagemId = 0;
        public int Vida = 0;
        public int VidaMaxima = 0;
    }
}
