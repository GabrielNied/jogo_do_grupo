namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Retrato de um efeito de status ativo numa entidade.
    /// </summary>
    public struct InstanciaEfeitoDeStatus
    {
        public TipoEfeitoDeStatus Tipo { get; set; }
        public int Acumulos { get; set; }
        public float DuracaoRestanteSegundos { get; set; }
        public float DuracaoTotalSegundos { get; set; }
    }
}
