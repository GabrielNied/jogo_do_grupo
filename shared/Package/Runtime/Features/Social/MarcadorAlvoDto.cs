namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Par associando um identificador de entidade/alvo a um marcador tático atribuído pelo grupo.
    /// </summary>
    public struct MarcadorAlvoDto
    {
        public long AlvoId { get; set; }
        public TipoMarcadorDeAlvo Marcador { get; set; }

        public MarcadorAlvoDto(long alvoId, TipoMarcadorDeAlvo marcador)
        {
            AlvoId = alvoId;
            Marcador = marcador;
        }
    }
}
