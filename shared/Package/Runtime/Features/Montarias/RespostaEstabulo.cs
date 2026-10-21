namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Servidor → Client: catálogo de montarias à venda no estábulo (#1067).
    /// </summary>
    [System.Serializable]
    public sealed class RespostaEstabulo
    {
        public MontariaEstabuloDto[] Ofertas = new MontariaEstabuloDto[0];
    }
}
