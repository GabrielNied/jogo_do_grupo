namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor → quem está na área: a barra de quebra (stagger) de um chefe mudou.
    /// </summary>
    [System.Serializable]
    public class BarraDeQuebraMudou
    {
        public long EntidadeId;
        public int ValorAtual;
        public int CapacidadeMax;
        public bool Quebrado;
    }
}
