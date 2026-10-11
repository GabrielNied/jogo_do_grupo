namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Servidor manda a quem entra numa área os inimigos vivos que estão nela. Os mortos não
    /// vêm: voltam por <see cref="InimigoApareceu"/>.
    /// Id: <see cref="MobsMessageIds.InimigosDaArea"/>.
    /// </summary>
    [System.Serializable]
    public class InimigosDaArea
    {
        /// <summary>Inimigos vivos da área. Nunca nulo.</summary>
        public InimigoNoMundo[] Inimigos = System.Array.Empty<InimigoNoMundo>();
    }
}
