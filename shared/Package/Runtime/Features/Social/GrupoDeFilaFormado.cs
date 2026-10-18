namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor → client: notificação enviada aos membros quando o grupo de 5 da fila foi formado.
    /// </summary>
    public class GrupoDeFilaFormado
    {
        public long GrupoId { get; set; }
        public PapelNoGrupo SeuPapel { get; set; }
        public string Mensagem { get; set; } = "";
    }
}
