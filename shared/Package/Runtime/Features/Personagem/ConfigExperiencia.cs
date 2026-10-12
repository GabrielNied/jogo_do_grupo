namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>Valores provisórios de balanceamento. Altere aqui para calibrar a progressão.</summary>
    public static class ConfigExperiencia
    {
        public const int NivelMaximo = 30;
        public const int PontosPorNivel = 5;

        // XP necessário = BaseXp * nivel ^ ExpoenteCurva.
        // Reduzir a base acelera toda a progressão; o expoente controla seu crescimento.
        public const double BaseXp = 50;
        public const double ExpoenteCurva = 1.2;

        // Meta de ritmo (proposta; docs/features/personagem.md). O teste de simulação falha se a
        // curva sair dela. Referência: um jogador solo matando mobs comuns de 20 XP.
        public const double MobsPorMinutoDeReferencia = 4;
        public const int XpPorMobDeReferencia = 20;
        public const int NivelDaMetaDeRitmo = 10;
        public const double MinutosMinimosAteNivelDaMeta = 30;
        public const double MinutosMaximosAteNivelDaMeta = 50;
        public const double MinutosMaximosPorNivelInicial = 10;
    }
}
