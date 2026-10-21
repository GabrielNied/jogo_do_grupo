namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Client → servidor: pede para entrar na fila automática de dungeon com o papel indicado.
    /// </summary>
    [System.Serializable]
    public class EntrarNaFilaDeGrupo
    {
        public PapelNoGrupo Papel;
    }
}
