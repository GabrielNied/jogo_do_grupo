using JogoDoGrupo.Shared.Features.Instancias;

namespace JogoDoGrupo.Client.Features.Instancias
{
    /// <summary>Textos de recusa e resultado mostrados na tela.</summary>
    internal static class TextosInstancias
    {
        public const string NomeDaDungeon = "Mina do Reflexo";

        public static string Recusa(string codigo) =>
            codigo switch
            {
                CodigosErroInstancias.NaoELider => "Só o líder do grupo pode iniciar a dungeon.",
                CodigosErroInstancias.GrupoEmOutraDungeon => "Seu grupo já está em outra dungeon.",
                CodigosErroInstancias.LongeDemais => "Chegue mais perto da entrada da "
                    + NomeDaDungeon
                    + " para entrar.",
                _ => "Não foi possível entrar na dungeon.",
            };

        public static string TituloDoResultado(ResultadoDungeon resultado) =>
            resultado switch
            {
                ResultadoDungeon.Vitoria => "Vitória",
                ResultadoDungeon.Derrota => "Derrota",
                _ => "Dungeon encerrada",
            };

        public static string Resultado(ResultadoDungeon resultado) =>
            resultado switch
            {
                ResultadoDungeon.Vitoria =>
                    "O Guardião do Reflexo foi derrotado. Você voltou para a entrada da "
                        + NomeDaDungeon
                        + ".",
                ResultadoDungeon.Derrota =>
                    "Ninguém do grupo resistiu. Você voltou para a entrada da "
                        + NomeDaDungeon
                        + ".",
                _ => "A " + NomeDaDungeon + " foi fechada. Você voltou para a entrada.",
            };
    }
}
