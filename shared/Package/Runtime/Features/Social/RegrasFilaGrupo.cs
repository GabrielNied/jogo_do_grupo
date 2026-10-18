namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Constantes e validações da fila de dungeon (Dungeon Finder).
    /// Regra: grupo de 5 formado por 1 Tank, 1 Healer e 3 DPS.
    /// </summary>
    public static class RegrasFilaGrupo
    {
        public const int TanquesNecessarios = 1;
        public const int HealersNecessarios = 1;
        public const int DpsNecessarios = 3;
        public const int TamanhoTotalDoGrupo = 5;

        public static bool PapelValido(PapelNoGrupo papel) =>
            papel == PapelNoGrupo.Tank || papel == PapelNoGrupo.Healer || papel == PapelNoGrupo.Dps;
    }
}
