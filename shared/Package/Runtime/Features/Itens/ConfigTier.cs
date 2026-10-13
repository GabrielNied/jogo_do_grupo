namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Valores ajustáveis do tier (issue #615, TIER-001 a TIER-008). Todos são números iniciais
    /// de balanceamento; as contas ficam em <see cref="RegrasTier"/>. Limiares e faixas mudam só
    /// aqui.
    /// </summary>
    public static class ConfigTier
    {
        /// <summary>Menor e maior valor da escala interna de um atributo de item.</summary>
        public const int EscalaMinima = 0;

        public const int EscalaMaxima = 100;

        // Limiares da qualidade média (0 a 100): D abaixo de C, C de 35 a menos de 50, e assim por
        // diante. S vai até 100; S+ não é um limiar (todos os elegíveis no máximo).

        /// <summary>Qualidade mínima do tier C.</summary>
        public const double LimiarC = 35.0;

        /// <summary>Qualidade mínima do tier B.</summary>
        public const double LimiarB = 50.0;

        /// <summary>Qualidade mínima do tier A.</summary>
        public const double LimiarA = 65.0;

        /// <summary>Qualidade mínima do tier S.</summary>
        public const double LimiarS = 80.0;

        /// <summary>
        /// Tolerância da comparação com os limiares (a média de inteiros em divisões inexatas pode
        /// sair 1e-13 abaixo do limite exato).
        /// </summary>
        public const double Tolerancia = 1e-9;

        /// <summary>
        /// Faixa de cada atributo por categoria de peça. Hoje todos os atributos da lista são
        /// elegíveis em toda categoria, de 0 a 100: a #623 troca a lista e a #627 diferencia as
        /// categorias (ex.: <c>.Categoria(TipoEquipamento.Anel, StatusItem.X, 10, 60)</c> ou
        /// <c>.Inelegivel(...)</c>).
        /// </summary>
        public static readonly TabelaDeFaixas Faixas = new TabelaDeFaixas()
            .Padrao(StatusItem.Forca, EscalaMinima, EscalaMaxima)
            .Padrao(StatusItem.Destreza, EscalaMinima, EscalaMaxima)
            .Padrao(StatusItem.Intelecto, EscalaMinima, EscalaMaxima)
            .Padrao(StatusItem.Vitalidade, EscalaMinima, EscalaMaxima)
            .Padrao(StatusItem.Espirito, EscalaMinima, EscalaMaxima)
            .Padrao(StatusItem.Vigor, EscalaMinima, EscalaMaxima)
            .Padrao(StatusItem.Defesa, EscalaMinima, EscalaMaxima);

        /// <summary>
        /// Onde a geração sorteia dentro da faixa, por grau (proposta de TIER-006): o grau não muda
        /// a faixa, só a parte dela em que o valor cai. Frações da amplitude, de 0 a 1.
        /// </summary>
        private static readonly double[] InicioDaJanela = { 0.0, 0.2, 0.4 };

        private static readonly double[] FimDaJanela = { 0.6, 0.8, 1.0 };

        /// <summary>Início da janela de sorteio do grau, como fração da amplitude da faixa.</summary>
        public static double InicioDoGrau(GrauEquipamento grau) => InicioDaJanela[(int)grau];

        /// <summary>Fim da janela de sorteio do grau, como fração da amplitude da faixa.</summary>
        public static double FimDoGrau(GrauEquipamento grau) => FimDaJanela[(int)grau];

        /// <summary>A faixa do atributo na categoria, ou <c>null</c> se não é elegível.</summary>
        public static FaixaDeAtributo? Faixa(TipoEquipamento tipo, StatusItem status) =>
            Faixas.Faixa(tipo, status);

        /// <summary>
        /// O máximo absoluto do atributo na categoria, ou <c>null</c> se não é elegível. É o teto
        /// que a fusão respeita (FUSAO-015).
        /// </summary>
        public static int? Maximo(TipoEquipamento tipo, StatusItem status) =>
            Faixas.Faixa(tipo, status)?.Maximo;

        /// <summary>Os atributos elegíveis (geração, tier e S+) da categoria.</summary>
        public static StatusItem[] Elegiveis(TipoEquipamento tipo) => Faixas.Elegiveis(tipo);

        /// <summary>
        /// Referência da escala antiga do atributo: o valor legado que equivale a 100 na escala
        /// atual (veja <see cref="MigracaoDeTier"/>). É 1,25 vez o que o grau Forte dava no
        /// catálogo (6 x 4 nos básicos, 3 x 4 no Espírito, 38 x 4 na maior defesa, 10 x 4 no
        /// Vigor): o Forte legado vira ~80, o Médio ~40 e o Inicial ~20.
        /// </summary>
        public static int ReferenciaLegada(StatusItem status)
        {
            switch (status)
            {
                case StatusItem.Forca:
                case StatusItem.Destreza:
                case StatusItem.Intelecto:
                case StatusItem.Vitalidade:
                    return 30;
                case StatusItem.Espirito:
                    return 15;
                case StatusItem.Vigor:
                    return 50;
                case StatusItem.Defesa:
                    return 190;
                default:
                    // Atributos sem faixa (legado sem efeito): o valor já era pequeno e fica como está.
                    return EscalaMaxima;
            }
        }
    }
}
