#nullable enable annotations
using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Instancias
{
    /// <summary>
    /// Regras de instâncias de raid: grupos de 6 a 10 membros, progressão em 3 etapas (gates),
    /// chefes por etapa e trava semanal de loot (docs/features/instancias.md).
    /// </summary>
    public static class RegrasRaid
    {
        /// <summary>Tamanho de uma dungeon convencional.</summary>
        public const int TamanhoDaDungeon = 5;

        /// <summary>Tamanho máximo de uma raid, contando o líder.</summary>
        public const int TamanhoMaximo = 10;

        /// <summary>Total de etapas (gates) em uma raid.</summary>
        public const int TotalEtapas = 3;

        public const int PrimeiraEtapa = 1;
        public const int UltimaEtapa = TotalEtapas;

        /// <summary>Id da criatura chefe da Etapa 1 (Presa-Velha).</summary>
        public const int ChefeEtapa1CriaturaId = 200;

        /// <summary>Id da criatura chefe da Etapa 2 (Guardião do Reflexo).</summary>
        public const int ChefeEtapa2CriaturaId = 100;

        /// <summary>Id da criatura chefe da Etapa 3 (Colosso Basáltico).</summary>
        public const int ChefeEtapa3CriaturaId = 301;

        /// <summary>Os ids dos chefes das 3 etapas na ordem (1, 2, 3).</summary>
        public static readonly int[] ChefesDasEtapas =
        {
            ChefeEtapa1CriaturaId,
            ChefeEtapa2CriaturaId,
            ChefeEtapa3CriaturaId,
        };

        /// <summary>Se um grupo desse tamanho entra como raid (passou do tamanho da dungeon).</summary>
        public static bool EhRaid(int membros) => membros > TamanhoDaDungeon;

        /// <summary>Se a instância comporta o grupo (nunca mais que o tamanho da raid).</summary>
        public static bool Cabe(int membros) => membros >= 1 && membros <= TamanhoMaximo;

        /// <summary>Se o número da etapa é válido.</summary>
        public static bool EtapaValida(int etapa) => etapa >= PrimeiraEtapa && etapa <= UltimaEtapa;

        /// <summary>Obtém o Id da criatura chefe para a etapa dada (1-indexada).</summary>
        public static int ObterChefeDaEtapa(int etapa)
        {
            if (etapa < PrimeiraEtapa || etapa > UltimaEtapa)
            {
                throw new ArgumentOutOfRangeException(nameof(etapa), "Etapa inválida.");
            }
            return ChefesDasEtapas[etapa - 1];
        }

        /// <summary>
        /// Obtém a chave única da semana de reset baseada no horário de Brasília (UTC-3),
        /// no formato "yyyy-MM-dd" referente à terça-feira às 06:00 BRT que iniciou o ciclo semanal.
        /// </summary>
        public static string ObterChaveSemana(DateTimeOffset instanteUtc)
        {
            var offsetBrt = TimeSpan.FromHours(-3);
            var local = instanteUtc.ToOffset(offsetBrt);

            // Subtrai 6 horas para que o ponto de corte do reset ocorra às 06:00 BRT
            var ajustado = local.AddHours(-6);
            var diff = (7 + (ajustado.DayOfWeek - DayOfWeek.Tuesday)) % 7;
            var inicioSemana = ajustado.Date.AddDays(-diff);

            return $"{inicioSemana.Year:D4}-{inicioSemana.Month:D2}-{inicioSemana.Day:D2}";
        }

        /// <summary>Se o jogador já realizou a etapa nessa semana.</summary>
        public static bool JaColetouLootDaEtapa(
            IEnumerable<int>? etapasConcluidasNaSemana,
            int etapa
        )
        {
            if (etapasConcluidasNaSemana == null)
                return false;
            foreach (var concluida in etapasConcluidasNaSemana)
            {
                if (concluida == etapa)
                    return true;
            }
            return false;
        }
    }
}
