namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Servidor avisa que um inimigo saiu do raio de interesse do jogador (não morreu). O client
    /// remove o modelo sem animação de morte. Id: <see cref="MobsMessageIds.InimigoSaiuDoInteresse"/>.
    /// </summary>
    [System.Serializable]
    public class InimigoSaiuDoInteresse
    {
        public long InimigoId = 0;
    }
}
