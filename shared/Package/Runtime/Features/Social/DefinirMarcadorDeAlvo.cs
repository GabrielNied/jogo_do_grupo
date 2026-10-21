namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Client → Servidor: solicita atribuir ou remover um marcador tático em um alvo para o grupo.
    /// </summary>
    [System.Serializable]
    public sealed class DefinirMarcadorDeAlvo
    {
        public long AlvoId;
        public TipoMarcadorDeAlvo Marcador;
    }
}
