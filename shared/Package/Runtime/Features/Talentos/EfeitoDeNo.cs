namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>Efeito de um nó por rank comprado.</summary>
    [System.Serializable]
    public class EfeitoDeNo
    {
        public TipoDeEfeito Tipo = TipoDeEfeito.AtributoPrincipal;

        /// <summary>Atributo que sobe, quando o tipo é <see cref="TipoDeEfeito.AtributoPrincipal"/>.</summary>
        public AtributoPrincipal Atributo = AtributoPrincipal.Forca;

        /// <summary>
        /// Nome do secundário (um valor de <c>AtributoSecundario</c> de Combate, ex.:
        /// <c>"ChanceCritica"</c>), quando o tipo é <see cref="TipoDeEfeito.AtributoSecundario"/>.
        /// </summary>
        public string Chave = "";

        /// <summary>
        /// Só no secundário: <c>false</c> soma <see cref="ValorPorRank"/> vezes o rank ao valor;
        /// <c>true</c> é bônus percentual (0,10 por rank é +10%), que Combate multiplica.
        /// </summary>
        public bool Multiplica = false;

        /// <summary>Quanto soma a cada rank. Maior que zero. No secundário, na unidade dele.</summary>
        public double ValorPorRank = 1;
    }
}
