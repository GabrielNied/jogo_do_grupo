namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>Efeito de um nó por rank comprado.</summary>
    [System.Serializable]
    public class EfeitoDeNo
    {
        public TipoDeEfeito Tipo = TipoDeEfeito.AtributoPrincipal;

        /// <summary>Atributo que sobe, quando o tipo é <see cref="TipoDeEfeito.AtributoPrincipal"/>.</summary>
        public AtributoPrincipal Atributo = AtributoPrincipal.Forca;

        /// <summary>Id do secundário, quando o tipo é <see cref="TipoDeEfeito.AtributoSecundario"/>.</summary>
        public string Chave = "";

        /// <summary>Quanto soma a cada rank. Maior que zero.</summary>
        public int ValorPorRank = 1;
    }
}
