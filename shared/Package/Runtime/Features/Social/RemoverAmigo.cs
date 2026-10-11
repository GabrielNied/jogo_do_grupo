namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Client desfaz uma amizade. Ela acaba para os dois lados e o outro não recebe aviso, só a
    /// lista nova. Id: <see cref="SocialMessageIds.RemoverAmigo"/>.
    /// </summary>
    [System.Serializable]
    public class RemoverAmigo
    {
        /// <summary>O mesmo valor de <see cref="AmigoDaLista.AmigoId"/>.</summary>
        public long AmigoId = 0;
    }
}
