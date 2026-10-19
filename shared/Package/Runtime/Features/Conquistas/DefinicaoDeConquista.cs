namespace JogoDoGrupo.Shared.Features.Conquistas
{
    public sealed class DefinicaoDeConquista
    {
        public int Id { get; }
        public string Nome { get; }
        public string Descricao { get; }
        public CategoriaConquista Categoria { get; }
        public int Meta { get; }
        public int Pontos { get; }
        public string TituloRecompensa { get; }

        public DefinicaoDeConquista(
            int id,
            string nome,
            string descricao,
            CategoriaConquista categoria,
            int meta,
            int pontos,
            string tituloRecompensa = ""
        )
        {
            Id = id;
            Nome = nome;
            Descricao = descricao;
            Categoria = categoria;
            Meta = meta;
            Pontos = pontos;
            TituloRecompensa = tituloRecompensa ?? "";
        }
    }
}
