namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Valores e regras de recuperação (#347): recarga de poções, regeneração fora de combate,
    /// bônus de refeições e descanso. Números calibráveis aqui.
    /// </summary>
    public static class ConfigRecuperacao
    {
        /// <summary>Tempo sem dar ou receber dano para sair de combate (segundos).</summary>
        public const int TempoParaSairDeCombateSegundos = 5;

        /// <summary>Recarga compartilhada das poções de recuperação (segundos).</summary>
        public const int RecargaPocaoSegundos = 30;

        /// <summary>Cura fixa básica da poção.</summary>
        public const int CuraFixaPocao = 50;

        /// <summary>Percentual de cura baseado na vida máxima do personagem.</summary>
        public const double CuraPercentualPocao = 0.25;

        /// <summary>Taxa de regeneração de vida fora de combate (pontos por segundo).</summary>
        public const double RegeneracaoVidaForaDeCombate = 5.0;

        /// <summary>Multiplicador de regeneração quando descansando (sentado ou em local seguro).</summary>
        public const double MultiplicadorDescanso = 3.0;

        /// <summary>Multiplicador de regeneração de stamina e mana fora de combate.</summary>
        public const double MultiplicadorForaDeCombate = 2.0;

        // Modelos de refeições produzidas pelo Cozinheiro (#335)
        public const int ModeloCaldoDosErrantes = 109;
        public const int ModeloAssadoDeGalhada = 118;
        public const int ModeloEnsopadoDaFornalha = 136;

        // Duração dos buffs de refeição (segundos)
        public const int DuracaoCaldoDosErrantesSegundos = 30 * 60; // 30 min
        public const int DuracaoAssadoDeGalhadaSegundos = 45 * 60; // 45 min
        public const int DuracaoEnsopadoDaFornalhaSegundos = 60 * 60; // 60 min

        // Bônus de regeneração de vida das refeições (pontos por segundo)
        public const double BonusRegenVidaCaldo = 10.0;
        public const double BonusRegenVidaAssado = 15.0;
        public const double BonusRegenVidaEnsopado = 20.0;

        public static int DuracaoRefeicao(int modeloId)
        {
            switch (modeloId)
            {
                case ModeloCaldoDosErrantes:
                    return DuracaoCaldoDosErrantesSegundos;
                case ModeloAssadoDeGalhada:
                    return DuracaoAssadoDeGalhadaSegundos;
                case ModeloEnsopadoDaFornalha:
                    return DuracaoEnsopadoDaFornalhaSegundos;
                default:
                    return DuracaoCaldoDosErrantesSegundos;
            }
        }

        public static double BonusRegenVida(int modeloId)
        {
            switch (modeloId)
            {
                case ModeloCaldoDosErrantes:
                    return BonusRegenVidaCaldo;
                case ModeloAssadoDeGalhada:
                    return BonusRegenVidaAssado;
                case ModeloEnsopadoDaFornalha:
                    return BonusRegenVidaEnsopado;
                default:
                    return 5.0;
            }
        }

        public static string NomeRefeicao(int modeloId)
        {
            switch (modeloId)
            {
                case ModeloCaldoDosErrantes:
                    return "Caldo dos Errantes";
                case ModeloAssadoDeGalhada:
                    return "Assado de Galhada";
                case ModeloEnsopadoDaFornalha:
                    return "Ensopado da Fornalha";
                default:
                    return "Refeição";
            }
        }
    }
}
