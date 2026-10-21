namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor → dono e quem está na área: os efeitos de status de uma entidade mudaram
    /// (aplicado, renovado, expirado ou removido).
    /// </summary>
    [System.Serializable]
    public class EfeitosDeStatusMudaram
    {
        public long EntidadeId;
        public InstanciaEfeitoDeStatus[] Efeitos = new InstanciaEfeitoDeStatus[0];
        public float ImunidadeAtordoadoRestanteSegundos;
    }
}
