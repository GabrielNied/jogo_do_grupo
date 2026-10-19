namespace JogoDoGrupo.Shared.Features.Instancias
{
    /// <summary>
    /// Tamanhos da dungeon e da raid. Um grupo que passa de 5 membros é de raid, até 10, com um
    /// líder só (docs/features/instancias.md).
    /// </summary>
    public static class RegrasRaid
    {
        /// <summary>Tamanho de uma dungeon.</summary>
        public const int TamanhoDaDungeon = 5;

        /// <summary>Tamanho máximo de uma raid, contando o líder.</summary>
        public const int TamanhoMaximo = 10;

        /// <summary>Se um grupo desse tamanho entra como raid (passou do tamanho da dungeon).</summary>
        public static bool EhRaid(int membros) => membros > TamanhoDaDungeon;

        /// <summary>Se a instância comporta o grupo (nunca mais que o tamanho da raid).</summary>
        public static bool Cabe(int membros) => membros >= 1 && membros <= TamanhoMaximo;
    }
}
