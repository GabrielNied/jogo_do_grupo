namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Retrato de um efeito de status ativo numa entidade.
    /// </summary>
    [System.Serializable]
    public struct InstanciaEfeitoDeStatus
    {
        public TipoEfeitoDeStatus Tipo;
        public int Acumulos;
        public float DuracaoRestanteSegundos;
        public float DuracaoTotalSegundos;
    }
}
