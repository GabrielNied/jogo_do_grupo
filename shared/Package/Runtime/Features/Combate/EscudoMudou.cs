namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor → dono e quem está na área: o escudo de uma entidade mudou (aplicado, consumido
    /// ou expirado). <see cref="Absorcao"/> 0 significa sem escudo.
    /// </summary>
    public class EscudoMudou
    {
        public long EntidadeId { get; set; }
        public int Absorcao { get; set; }
        public int AbsorcaoMaxima { get; set; }
        public float DuracaoRestanteSegundos { get; set; }
        public float DuracaoTotalSegundos { get; set; }
    }
}
