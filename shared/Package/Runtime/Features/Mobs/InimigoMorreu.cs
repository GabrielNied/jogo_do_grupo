namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Servidor avisa quem está na área que um inimigo morreu. Ele deixa de ser alvo; se a
    /// criatura volta sozinha, chega um <see cref="InimigoApareceu"/> depois.
    /// Id: <see cref="MobsMessageIds.InimigoMorreu"/>.
    /// </summary>
    [System.Serializable]
    public class InimigoMorreu
    {
        public long InimigoId = 0;
    }
}
