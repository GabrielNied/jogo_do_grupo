namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor avisa aos membros do grupo que a área atual de um membro mudou (#351).
    /// Id: <see cref="SocialMessageIds.AreaDoMembroMudou"/>.
    /// </summary>
    [System.Serializable]
    public class AreaDoMembroMudou
    {
        public long PersonagemId = 0;
        public string Area = "";
    }
}
