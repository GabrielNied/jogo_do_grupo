namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>
    /// Client → Servidor: solicita entrada na fila da arena 3v3.
    /// </summary>
    [System.Serializable]
    public class EntrarNaFilaDeArena
    {
        public int TamanhoEquipe = RegrasPvp.TamanhoEquipeArena;
    }
}
