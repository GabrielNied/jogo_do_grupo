namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor → dono e quem está na área: os efeitos de status de uma entidade mudaram
    /// (aplicado, renovado, expirado ou removido).
    /// </summary>
    public class EfeitosDeStatusMudaram
    {
        public long EntidadeId { get; set; }
        public InstanciaEfeitoDeStatus[] Efeitos { get; set; } = new InstanciaEfeitoDeStatus[0];
        public float ImunidadeAtordoadoRestanteSegundos { get; set; }
    }
}
