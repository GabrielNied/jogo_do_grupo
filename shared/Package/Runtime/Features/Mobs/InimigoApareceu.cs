namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Servidor avisa quem está na área que um inimigo apareceu (por exemplo, voltou depois de
    /// morto). A variante é sorteada de novo a cada aparição.
    /// Id: <see cref="MobsMessageIds.InimigoApareceu"/>.
    /// </summary>
    [System.Serializable]
    public class InimigoApareceu
    {
        /// <summary>O inimigo que apareceu. Nunca nulo.</summary>
        public InimigoNoMundo Inimigo = new InimigoNoMundo();
    }
}
