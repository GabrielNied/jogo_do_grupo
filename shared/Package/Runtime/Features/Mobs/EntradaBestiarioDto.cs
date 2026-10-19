namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Entrada de progresso de uma espécie no bestiário de um jogador (#922).
    /// </summary>
    public sealed class EntradaBestiarioDto
    {
        public EntradaBestiarioDto() { }

        public EntradaBestiarioDto(int criaturaId, int derrotas, NivelDeConhecimentoBestiario nivel)
        {
            CriaturaId = criaturaId;
            Derrotas = derrotas;
            Nivel = nivel;
        }

        public int CriaturaId { get; set; }
        public int Derrotas { get; set; }
        public NivelDeConhecimentoBestiario Nivel { get; set; }
    }
}
