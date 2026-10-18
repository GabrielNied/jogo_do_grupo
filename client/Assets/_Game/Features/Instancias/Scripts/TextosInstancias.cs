using System;
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
                CodigosErroInstancias.NivelNaoDesbloqueado =>
                    "Nível ainda não desbloqueado. Vença o nível anterior dentro do tempo para liberar.",
                _ => "Não foi possível entrar na dungeon.",
            };

        public static string FormatarTempo(int segundos)
        {
            var seg = Math.Max(0, segundos);
            return $"{seg / 60:D2}:{seg % 60:D2}";
        }

        public static string TituloDoResultado(ResultadoDungeon resultado) =>
            resultado switch
            {
                ResultadoDungeon.Vitoria => "Vitória",
                ResultadoDungeon.Derrota => "Derrota",
                _ => "Dungeon encerrada",
            };

        public static string TituloDoResultado(
            ResultadoDungeon resultado,
            int nivel,
            bool noTempo
        ) =>
            resultado switch
            {
                ResultadoDungeon.Vitoria => noTempo
                    ? $"Vitória — Nível {nivel} (No tempo!)"
                    : $"Vitória — Nível {nivel} (Fora do tempo)",
                ResultadoDungeon.Derrota => $"Derrota — Nível {nivel}",
                _ => $"Dungeon encerrada — Nível {nivel}",
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

        public static string DetalheDoResultado(
            ResultadoDungeon resultado,
            int nivel,
            int tempoSegundos,
            bool noTempo,
            int proximoNivel
        )
        {
            var tempoTexto = FormatarTempo(tempoSegundos);
            if (resultado == ResultadoDungeon.Vitoria)
            {
                if (noTempo)
                {
                    return $"O Guardião do Reflexo foi derrotado em {tempoTexto}! Nível {proximoNivel} desbloqueado.";
                }

                return $"O Guardião do Reflexo foi derrotado em {tempoTexto} (fora do tempo limite de 15:00).";
            }

            if (resultado == ResultadoDungeon.Derrota)
            {
                return $"Ninguém do grupo resistiu no Nível {nivel} após {tempoTexto}. Você voltou para a entrada.";
            }

            return $"A {NomeDaDungeon} foi fechada. Você voltou para a entrada.";
        }
    }
}
