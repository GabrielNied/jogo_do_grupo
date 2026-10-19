namespace JogoDoGrupo.Shared.Features.Npcs
{
    public sealed class OpcaoDeDialogo
    {
        public string Id { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
        public AcaoDeDialogo Acao { get; set; }
        public string? Parametro { get; set; }

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
