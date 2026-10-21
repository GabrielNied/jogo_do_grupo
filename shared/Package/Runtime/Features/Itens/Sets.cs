#nullable enable annotations

using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Um marco de set (SET-001): com <see cref="Pecas"/> peças vestidas, as
    /// <see cref="Regras"/> ligam. Usa o mesmo motor de efeitos dos poderes.
    /// </summary>
    [System.Serializable]
    public sealed class MarcoDeSet
    {
        public int Pecas;
        public string Nome = "";
        public string Descricao = "";
        public RegraDePoder[] Regras = Array.Empty<RegraDePoder>();
    }

    /// <summary>Um set: peças próprias (por modelo-base) e os bônus dos marcos.</summary>
    [System.Serializable]
    public sealed class DefinicaoDeSet
    {
        public string Id = "";
        public string Nome = "";
        public int[] Modelos = Array.Empty<int>();
        public MarcoDeSet[] Marcos = Array.Empty<MarcoDeSet>();

        public bool Contem(int modeloId) => Array.IndexOf(Modelos, modeloId) >= 0;
    }

    /// <summary>
    /// Os sets do jogo. A lista completa é definida set a set na criação do conteúdo (SET-001);
    /// por ora só o set de exemplo que prova o motor.
    /// </summary>
    public static class CatalogoDeSets
    {
        public const string SetCacadaDoPredador = "set.cacada-do-predador";

        // Etiqueta que as skills declaram (veja CatalogoDePoderes).
        private const string Fisica = CatalogoDePoderes.EtiquetaFisica;

        private static readonly List<DefinicaoDeSet> Todos = new List<DefinicaoDeSet>();

        static CatalogoDeSets()
        {
            // Peito, luvas, botas, anel e brinco do Predador (variante A): peças sem poder
            // próprio, para mostrar que o set independe de poder (PODER-007).
            Todos.Add(
                new DefinicaoDeSet
                {
                    Id = SetCacadaDoPredador,
                    Nome = "Caçada do Predador",
                    Modelos = new[] { 120, 121, 123, 125, 126 },
                    Marcos = new[]
                    {
                        new MarcoDeSet
                        {
                            Pecas = 2,
                            Nome = "Ritmo de caça",
                            Descricao =
                                "Usar uma skill física dá uma carga de Ritmo (até "
                                + ConfigPoderes.RitmoMaximoDeCargas
                                + "); a próxima skill física gasta as cargas e causa +"
                                + Math.Round(ConfigPoderes.RitmoBonusPorCarga * 100)
                                + "% de dano por carga.",
                            Regras = new[]
                            {
                                new RegraDePoder
                                {
                                    Id = "carregar",
                                    Gatilho = GatilhoDePoder.AoUsarSkill,
                                    Skill = SeletorDeSkill.PorEtiqueta(Fisica),
                                    Acao = AcaoDePoder.GanharCarga,
                                    Estado = CatalogoDePoderes.Ritmo,
                                    DuracaoMs = ConfigPoderes.RitmoDuracaoMs,
                                    MaximoDeAcumulos = ConfigPoderes.RitmoMaximoDeCargas,
                                },
                                new RegraDePoder
                                {
                                    Id = "consumir",
                                    Gatilho = GatilhoDePoder.AoUsarSkill,
                                    Skill = SeletorDeSkill.PorEtiqueta(Fisica),
                                    Condicao = CondicaoDePoder.TemCarga,
                                    Estado = CatalogoDePoderes.Ritmo,
                                    Acao = AcaoDePoder.AmplificarDano,
                                    Valor = ConfigPoderes.RitmoBonusPorCarga,
                                },
                            },
                        },
                        new MarcoDeSet
                        {
                            Pecas = 4,
                            Nome = "Fôlego do predador",
                            Descricao =
                                "Acertar com uma skill física tira "
                                + ConfigPoderes.FolegoReducaoDeRecargaMs / 1000.0
                                + " s da recarga das suas outras skills físicas (a cada "
                                + ConfigPoderes.FolegoIntervaloMs / 1000
                                + " s).",
                            Regras = new[]
                            {
                                new RegraDePoder
                                {
                                    Id = "folego",
                                    Gatilho = GatilhoDePoder.AoAcertarComSkill,
                                    Skill = SeletorDeSkill.PorEtiqueta(Fisica),
                                    Acao = AcaoDePoder.ReduzirRecarga,
                                    Valor = ConfigPoderes.FolegoReducaoDeRecargaMs,
                                    IntervaloMinimoMs = ConfigPoderes.FolegoIntervaloMs,
                                    AlvoDaAcao = SeletorDeSkill.PorEtiqueta(Fisica),
                                },
                            },
                        },
                    },
                }
            );
        }

        public static IReadOnlyList<DefinicaoDeSet> Sets => Todos;

        public static DefinicaoDeSet? Obter(string? id)
        {
            if (id == null)
                return null;
            foreach (var set in Todos)
            {
                if (set.Id == id)
                    return set;
            }
            return null;
        }

        /// <summary>O set a que o modelo pertence, ou <c>null</c> (a maioria das peças não tem set).</summary>
        public static DefinicaoDeSet? DoModelo(int modeloId)
        {
            foreach (var set in Todos)
            {
                if (set.Contem(modeloId))
                    return set;
            }
            return null;
        }
    }

    /// <summary>O estado de um set para o tooltip: quantas peças e quais marcos estão ligados.</summary>
    public sealed class ResumoDoSet
    {
        public DefinicaoDeSet Set = null!;

        /// <summary>Quantos modelos distintos do set estão vestidos e ativos.</summary>
        public int Vestidas;

        /// <summary>Quantas peças o set tem.</summary>
        public int Total;

        /// <summary>Marcos em ordem; <see cref="Ativo"/> diz se ligou.</summary>
        public List<MarcoDoResumo> Marcos = new List<MarcoDoResumo>();
    }

    [System.Serializable]
    public sealed class MarcoDoResumo
    {
        public MarcoDeSet Marco = null!;
        public bool Ativo;
    }

    /// <summary>Contagem de peças e marcos de set (SET-001 a SET-003), pura e sem estado.</summary>
    public static class RegrasDeSets
    {
        /// <summary>
        /// Quantas peças do set estão ativas: vestidas e, no caso de arma, do conjunto ativo
        /// (<see cref="RegrasEquipamentos.EstaAtiva"/>). Conta modelos distintos: duas peças
        /// iguais nos encaixes de par (anel 1 e 2) valem uma.
        /// </summary>
        public static int Contar(
            DefinicaoDeSet set,
            IEnumerable<EntradaInventario> entradas,
            int conjunto
        )
        {
            var modelos = new HashSet<int>();
            if (set == null || entradas == null)
                return 0;
            foreach (var entrada in entradas)
            {
                if (!RegrasEquipamentos.EstaAtiva(entrada, conjunto))
                    continue;
                var item = RegrasEquipamentos.DadosDe(entrada);
                if (item != null && set.Contem(item.ModeloId))
                    modelos.Add(item.ModeloId);
            }
            return modelos.Count;
        }

        /// <summary>Os marcos ligados com <paramref name="pecas"/> peças.</summary>
        public static List<MarcoDeSet> MarcosAtivos(DefinicaoDeSet set, int pecas)
        {
            var lista = new List<MarcoDeSet>();
            if (set == null)
                return lista;
            foreach (var marco in set.Marcos)
            {
                if (marco.Pecas > 0 && pecas >= marco.Pecas)
                    lista.Add(marco);
            }
            return lista;
        }

        /// <summary>O resumo do set de um modelo (para o tooltip), ou <c>null</c> se o modelo não tem set.</summary>
        public static ResumoDoSet? Resumir(
            int modeloId,
            IEnumerable<EntradaInventario> entradas,
            int conjunto
        )
        {
            var set = CatalogoDeSets.DoModelo(modeloId);
            if (set == null)
                return null;
            var vestidas = Contar(set, entradas, conjunto);
            var resumo = new ResumoDoSet
            {
                Set = set,
                Vestidas = vestidas,
                Total = set.Modelos.Length,
            };
            foreach (var marco in set.Marcos)
                resumo.Marcos.Add(
                    new MarcoDoResumo { Marco = marco, Ativo = vestidas >= marco.Pecas }
                );
            return resumo;
        }
    }
}
