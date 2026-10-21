namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Par associando um identificador de entidade/alvo a um marcador tático atribuído pelo grupo.
    /// </summary>
    [System.Serializable]
    public struct MarcadorAlvoDto
    {
        public long AlvoId;
        public TipoMarcadorDeAlvo Marcador;

        public MarcadorAlvoDto(long alvoId, TipoMarcadorDeAlvo marcador)
        {
            AlvoId = alvoId;
            Marcador = marcador;
        }
    }
}
