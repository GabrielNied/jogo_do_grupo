using System;
using System.Collections.Generic;
using System.Linq;

namespace JogoDoGrupo.Shared.Features.Missoes
{
    /// <summary>
    /// Regras puras e cálculos autoritativos de ciclo de reset diário e semanal no horário de Brasília (#1062).
    /// </summary>
    public static class RegrasCicloReset
    {
        /// <summary>
        /// Obtém o seed inteiro único do dia atual no horário de Brasília (ex: 20261020).
        /// O dia vira exatamente à meia-noite (00:00) de Brasília.
        /// </summary>
        public static int ObterSeedDiario(DateTimeOffset momentoUtc)
        {
            var local = momentoUtc.ToOffset(ConfigMissoes.FusoHorarioBrasilia);
            return local.Year * 10000 + local.Month * 100 + local.Day;
        }

        /// <summary>
        /// Obtém o seed inteiro único da semana atual no horário de Brasília.
        /// O ciclo semanal inicia na terça-feira às 00:00 de Brasília.
        /// Retorna o valor de data da terça-feira que abriu o ciclo semanal (ex: 20261020).
        /// </summary>
        public static int ObterSeedSemanal(DateTimeOffset momentoUtc)
        {
            var local = momentoUtc.ToOffset(ConfigMissoes.FusoHorarioBrasilia);
            var data = local.Date;
            int diasDesdeReset = ((int)data.DayOfWeek - (int)ConfigMissoes.DiaResetSemanal + 7) % 7;
            var dataInicioSemana = data.AddDays(-diasDesdeReset);
            return dataInicioSemana.Year * 10000
                + dataInicioSemana.Month * 100
                + dataInicioSemana.Day;
        }

        /// <summary>
        /// Verifica se dois instantes Unix (em segundos) pertencem ao mesmo dia no fuso de Brasília.
        /// </summary>
        public static bool MesmoCicloDiario(long timestampASegundos, long timestampBSegundos)
        {
            var dtA = DateTimeOffset.FromUnixTimeSeconds(timestampASegundos);
            var dtB = DateTimeOffset.FromUnixTimeSeconds(timestampBSegundos);
            return ObterSeedDiario(dtA) == ObterSeedDiario(dtB);
        }

        /// <summary>
        /// Verifica se dois instantes Unix (em segundos) pertencem à mesma semana de reset no fuso de Brasília.
        /// </summary>
        public static bool MesmoCicloSemanal(long timestampASegundos, long timestampBSegundos)
        {
            var dtA = DateTimeOffset.FromUnixTimeSeconds(timestampASegundos);
            var dtB = DateTimeOffset.FromUnixTimeSeconds(timestampBSegundos);
            return ObterSeedSemanal(dtA) == ObterSeedSemanal(dtB);
        }

        /// <summary>
        /// Retorna o próximo instante de reset diário (00:00 BRT do dia seguinte).
        /// </summary>
        public static DateTimeOffset ObterProximoResetDiario(DateTimeOffset momentoUtc)
        {
            var local = momentoUtc.ToOffset(ConfigMissoes.FusoHorarioBrasilia);
            var proximoDiaLocal = local.Date.AddDays(1);
            return new DateTimeOffset(proximoDiaLocal, ConfigMissoes.FusoHorarioBrasilia);
        }

        /// <summary>
        /// Retorna o próximo instante de reset semanal (próxima terça-feira às 00:00 BRT).
        /// </summary>
        public static DateTimeOffset ObterProximoResetSemanal(DateTimeOffset momentoUtc)
        {
            var local = momentoUtc.ToOffset(ConfigMissoes.FusoHorarioBrasilia);
            var data = local.Date;
            int diasDesdeReset = ((int)data.DayOfWeek - (int)ConfigMissoes.DiaResetSemanal + 7) % 7;
            var dataInicioSemana = data.AddDays(-diasDesdeReset);
            var proximaSemana = dataInicioSemana.AddDays(7);
            return new DateTimeOffset(proximaSemana, ConfigMissoes.FusoHorarioBrasilia);
        }

        /// <summary>
        /// Hash determinístico pseudo-aleatório a partir de uma semente e um ID de missão.
        /// Consistente em qualquer plataforma ou arquitetura.
        /// </summary>
        public static uint HashSeedEId(int seed, int id)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ (uint)seed) * 16777619u;
                h = (h ^ (uint)id) * 16777619u;
                h ^= h >> 16;
                h *= 0x85ebca6bu;
                h ^= h >> 13;
                h *= 0xc2b2ae35u;
                h ^= h >> 16;
                return h;
            }
        }

        /// <summary>
        /// Sorteia a rotação ativa de missões diárias do dia para o personagem, com base nas missões
        /// candidatas (nível e requisitos de região cumpridos), selecionando até <paramref name="quantidade"/>.
        /// </summary>
        public static List<DefinicaoDeMissao> ObterRotacaoDiaria(
            IEnumerable<DefinicaoDeMissao> catalogo,
            int nivelPersonagem,
            IReadOnlyCollection<int> missoesConcluidas,
            DateTimeOffset momentoUtc,
            int quantidade = ConfigMissoes.QuantidadeDiariasPorDia
        )
        {
            int seed = ObterSeedDiario(momentoUtc);
            var concluidasSet =
                missoesConcluidas != null
                    ? new HashSet<int>(missoesConcluidas)
                    : new HashSet<int>();

            var candidatas = catalogo
                .Where(m => m.Tipo == TipoDeMissao.Diaria && nivelPersonagem >= m.NivelMinimo)
                .Where(m =>
                    m.RequisitosMissaoIds == null
                    || m.RequisitosMissaoIds.All(concluidasSet.Contains)
                )
                .OrderBy(m => HashSeedEId(seed, m.Id))
                .ThenBy(m => m.Id)
                .Take(quantidade)
                .ToList();

            return candidatas;
        }

        /// <summary>
        /// Sorteia a rotação ativa de missões semanais da semana para o personagem, com base nas missões
        /// candidatas (nível e requisitos de região cumpridos), selecionando até <paramref name="quantidade"/>.
        /// </summary>
        public static List<DefinicaoDeMissao> ObterRotacaoSemanal(
            IEnumerable<DefinicaoDeMissao> catalogo,
            int nivelPersonagem,
            IReadOnlyCollection<int> missoesConcluidas,
            DateTimeOffset momentoUtc,
            int quantidade = ConfigMissoes.QuantidadeSemanaisPorSemana
        )
        {
            int seed = ObterSeedSemanal(momentoUtc);
            var concluidasSet =
                missoesConcluidas != null
                    ? new HashSet<int>(missoesConcluidas)
                    : new HashSet<int>();

            var candidatas = catalogo
                .Where(m => m.Tipo == TipoDeMissao.Semanal && nivelPersonagem >= m.NivelMinimo)
                .Where(m =>
                    m.RequisitosMissaoIds == null
                    || m.RequisitosMissaoIds.All(concluidasSet.Contains)
                )
                .OrderBy(m => HashSeedEId(seed, m.Id))
                .ThenBy(m => m.Id)
                .Take(quantidade)
                .ToList();

            return candidatas;
        }

        public static HashSet<int> ObterIdsRotacaoDiaria(
            IEnumerable<DefinicaoDeMissao> catalogo,
            int nivelPersonagem,
            IReadOnlyCollection<int> missoesConcluidas,
            DateTimeOffset momentoUtc,
            int quantidade = ConfigMissoes.QuantidadeDiariasPorDia
        )
        {
            var lista = ObterRotacaoDiaria(
                catalogo,
                nivelPersonagem,
                missoesConcluidas,
                momentoUtc,
                quantidade
            );
            var set = new HashSet<int>();
            for (int i = 0; i < lista.Count; i++)
            {
                set.Add(lista[i].Id);
            }
            return set;
        }

        public static HashSet<int> ObterIdsRotacaoSemanal(
            IEnumerable<DefinicaoDeMissao> catalogo,
            int nivelPersonagem,
            IReadOnlyCollection<int> missoesConcluidas,
            DateTimeOffset momentoUtc,
            int quantidade = ConfigMissoes.QuantidadeSemanaisPorSemana
        )
        {
            var lista = ObterRotacaoSemanal(
                catalogo,
                nivelPersonagem,
                missoesConcluidas,
                momentoUtc,
                quantidade
            );
            var set = new HashSet<int>();
            for (int i = 0; i < lista.Count; i++)
            {
                set.Add(lista[i].Id);
            }
            return set;
        }
    }
}
