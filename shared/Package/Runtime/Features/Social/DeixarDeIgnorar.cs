namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Client tira um jogador da lista de ignorados. A amizade antiga não volta sozinha.
    /// Id: <see cref="SocialMessageIds.DeixarDeIgnorar"/>.
    /// </summary>
    [System.Serializable]
    public class DeixarDeIgnorar
    {
        /// <summary>O mesmo valor de <see cref="JogadorIgnorado.IgnoradoId"/>.</summary>
        public long IgnoradoId = 0;
    }
}
