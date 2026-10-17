namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Valores ajustáveis do que um inimigo derrotado deixa cair (issues #493 e #615). Tudo é proposta a
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

        /// <summary>
        /// Chance de uma queda do chefe raro ser o seu item de equipamento exclusivo
        /// (<see cref="TabelaDeLoot.ModeloExclusivoDoChefeRaro"/>).
        /// </summary>
        public const double ChanceDeItemExclusivoDoChefeRaro = 0.50;

        // Faixas de nível do inimigo para o grau da peça: 1-9 Inicial, 10-19 Médio, 20+ Forte.
        // O requisito de nível para equipar é o piso da faixa do grau (1, 10 ou 20).

        /// <summary>Menor nível de inimigo que larga peça de grau Médio.</summary>
        public const int NivelDoInimigoParaGrauMedio = 10;

        /// <summary>Menor nível de inimigo que larga peça de grau Forte.</summary>
        public const int NivelDoInimigoParaGrauForte = 20;

        /// <summary>
        /// Quanto cada degrau de tier soma à pontuação da peça (0,10 = +10% por degrau, de D a
        /// S+). Serve só para dizer qual de duas peças é "melhor" (ritmo e destaque de upgrade).
        /// </summary>
        public const double BonusDePontuacaoPorTier = 0.10;

        // Origem do inimigo: quantas vezes cada atributo é sorteado e vale a melhor rolagem
        // (#615). Mais rolagens puxam o valor para o alto da janela do grau; com 1 o valor é
        // uniforme. Só o valor melhora: o tier é consequência dos valores.

        /// <summary>Inimigo comum sem variante: uma rolagem por atributo.</summary>
        public const int RolagensDoInimigoComum = 1;

        /// <summary>Inimigo comum elite (elite): melhor de duas.</summary>
        public const int RolagensDoInimigoElite = 2;

        /// <summary>Chefe raro: melhor de três.</summary>
        public const int RolagensDoChefeRaro = 3;

        /// <summary>Chefe: melhor de quatro.</summary>
        public const int RolagensDoChefe = 4;

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
