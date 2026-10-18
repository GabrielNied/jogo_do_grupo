namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>
    /// Servidor → Client: estado atual do jogador na fila da arena.
    /// </summary>
    [System.Serializable]
    public class StatusFilaDeArena
    {
        public bool NaFila;
        public int SegundosNaFila;
    }
}
