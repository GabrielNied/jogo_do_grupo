namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Client → servidor: pede para entrar na fila automática de dungeon com o papel indicado.
    /// </summary>
    public class EntrarNaFilaDeGrupo
    {
        public PapelNoGrupo Papel { get; set; }
    }
}
