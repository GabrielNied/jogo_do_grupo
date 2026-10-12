namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Valores ajustáveis do que um inimigo derrotado deixa cair (issue #493). Tudo é proposta a
    /// calibrar; as contas ficam em <see cref="TabelaDeLoot"/>. Quantos itens cada participante
    /// recebe continua em <see cref="RegrasLoot"/>.
    /// </summary>
    public static class ConfigLoot
    {
        /// <summary>
        /// Chance de cada item sorteado ser uma peça de equipamento. Hoje equipamento é o único
        /// item que existe fora do baú de teste, então fica em 1. Abaixo de 1, o item que não
        /// vira equipamento simplesmente não cai (poção e moeda ainda não têm função).
        /// </summary>
        public const double ChanceDeEquipamento = 1.0;

        // Faixas de nível do inimigo para o grau da peça: 1-9 Inicial, 10-19 Médio, 20+ Forte.
        // O requisito de nível para equipar é o piso da faixa do grau (1, 10 ou 20).

        /// <summary>Menor nível de inimigo que larga peça de grau Médio.</summary>
        public const int NivelDoInimigoParaGrauMedio = 10;

        /// <summary>Menor nível de inimigo que larga peça de grau Forte.</summary>
        public const int NivelDoInimigoParaGrauForte = 20;

        /// <summary>
        /// Quanto cada degrau de raridade soma à pontuação da peça (0,10 = +10% por degrau).
        /// Serve só para dizer qual de duas peças é "melhor" (ritmo e destaque de upgrade).
        /// </summary>
        public const double BonusDePontuacaoPorRaridade = 0.10;

        // Pesos de raridade por origem do inimigo, na ordem Comum, Incomum, Raro, Épico,
        // Lendário. Só a proporção importa. Subir o peso de uma raridade a torna mais comum
        // nessa origem; a soma de cada linha não precisa ser 1000.

        /// <summary>Inimigo comum sem variante: quase tudo comum ou incomum.</summary>
        public static readonly int[] PesosDoInimigoComum = { 700, 250, 45, 5, 0 };

        /// <summary>Inimigo comum fortalecido (elite): incomum e raro bem mais frequentes.</summary>
        public static readonly int[] PesosDoInimigoFortalecido = { 400, 400, 170, 28, 2 };

        /// <summary>Chefe raro: raro e épico são o miolo da tabela.</summary>
        public static readonly int[] PesosDoChefeRaro = { 100, 400, 380, 100, 20 };

        /// <summary>Chefe: nunca comum; raro é o mais provável e lendário é possível.</summary>
        public static readonly int[] PesosDoChefe = { 0, 300, 450, 200, 50 };

        // Meta de ritmo (testada em RitmoDeLootTestes). Referência: jogador solo, mesmo mob de
        // referência de ConfigExperiencia (4 mobs por minuto), níveis 1 a 10.

        /// <summary>
        /// X: em média, uma peça melhor que a equipada a cada tantos minutos nos níveis 1 a 10.
        /// O teste exige que o ritmo medido seja igual ou melhor (hoje sai ~1,4 min: as posições
        /// vazias enchem rápido, depois cada upgrade pede uma peça acima da equipada).
        /// </summary>
        public const double MinutosPorPecaMelhorNaMeta = 3.0;

        /// <summary>
        /// Maior espera, em minutos, sem nenhuma peça melhor nos níveis 1 a 10 (hoje a pior
        /// corrida de 200 espera ~15 min, perto do nível 10, com as posições quase cheias).
        /// </summary>
        public const double MaiorEsperaSemPecaMelhorMinutos = 20.0;
    }
}
