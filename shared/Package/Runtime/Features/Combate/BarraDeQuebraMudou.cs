namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor → quem está na área: a barra de quebra (stagger) de um chefe mudou.
    /// </summary>
    public class BarraDeQuebraMudou
    {
        public long EntidadeId { get; set; }
        public int ValorAtual { get; set; }
        public int CapacidadeMax { get; set; }
        public bool Quebrado { get; set; }
    }
}
