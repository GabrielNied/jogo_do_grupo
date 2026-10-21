namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Entrada de progresso de uma espécie no bestiário de um jogador (#922).
    /// </summary>
    [System.Serializable]
    public sealed class EntradaBestiarioDto
    {
        public EntradaBestiarioDto() { }

        public EntradaBestiarioDto(int criaturaId, int derrotas, NivelDeConhecimentoBestiario nivel)
        {
            CriaturaId = criaturaId;
            Derrotas = derrotas;
            Nivel = nivel;
        }

        public int CriaturaId;
        public int Derrotas;
        public NivelDeConhecimentoBestiario Nivel;
    }
}
