namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor → client: notificação enviada aos membros quando o grupo de 5 da fila foi formado.
    /// </summary>
    [System.Serializable]
    public class GrupoDeFilaFormado
    {
        public long GrupoId;
        public PapelNoGrupo SeuPapel;
        public string Mensagem = "";
    }
}
