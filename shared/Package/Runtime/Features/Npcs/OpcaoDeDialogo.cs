namespace JogoDoGrupo.Shared.Features.Npcs
{
    [System.Serializable]
    public sealed class OpcaoDeDialogo
    {
        public string Id = string.Empty;
        public string Texto = string.Empty;
        public AcaoDeDialogo Acao;
        public string? Parametro;

        public OpcaoDeDialogo() { }

        public OpcaoDeDialogo(string id, string texto, AcaoDeDialogo acao, string? parametro = null)
        {
            Id = id;
            Texto = texto;
            Acao = acao;
            Parametro = parametro;
        }
    }
}
