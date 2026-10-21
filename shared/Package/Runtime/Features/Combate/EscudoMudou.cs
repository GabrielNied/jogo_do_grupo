namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor → dono e quem está na área: o escudo de uma entidade mudou (aplicado, consumido
    /// ou expirado). <see cref="Absorcao"/> 0 significa sem escudo.
    /// </summary>
    [System.Serializable]
    public class EscudoMudou
    {
        public long EntidadeId;
        public int Absorcao;
        public int AbsorcaoMaxima;
        public float DuracaoRestanteSegundos;
        public float DuracaoTotalSegundos;
    }
}
