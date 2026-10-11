namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>Valores provisórios de balanceamento. Altere aqui para calibrar a progressão.</summary>
    public static class ConfigExperiencia
    {
        public const int NivelMaximo = 30;
        public const int PontosPorNivel = 5;

        // XP necessário = BaseXp * nivel ^ ExpoenteCurva.
        // Reduzir a base acelera toda a progressão; o expoente controla seu crescimento.
        public const double BaseXp = 100;
        public const double ExpoenteCurva = 1.7;
    }
}
