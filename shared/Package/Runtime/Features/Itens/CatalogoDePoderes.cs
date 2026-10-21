#nullable enable annotations
using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// O primeiro lote de poderes lendários (PODER-001 a PODER-007), a partir do exemplo da
    /// especificação: o peito faz a investida marcar, as luvas fazem golpes seguidos fortalecerem
    /// a próxima skill, as botas fazem a esquiva fortalecer a investida e o anel faz acertar um
    /// alvo marcado reduzir uma recarga. É um lote de exemplo, não o catálogo final.
    /// </summary>
    public static class CatalogoDePoderes
    {
        // Etiquetas que as skills declaram (EtiquetasDeSkill em Skills). Itens só conhece o texto;
        // um teste confere que as duas pontas concordam.
        public const string EtiquetaInvestida = "investida";
        public const string EtiquetaFisica = "fisica";

        public const string MarcaDaInvestida = "marca-investida";
        public const string Embalo = "embalo";
        public const string Impulso = "impulso";
        public const string Ritmo = "ritmo";

        public const string PoderPeito = "poder.peito.investida-marcante";
        public const string PoderLuvas = "poder.luvas.embalo-de-golpes";
        public const string PoderBotas = "poder.botas.esquiva-impetuosa";
        public const string PoderAnel = "poder.anel.cacador-de-marcados";

        private static readonly List<DefinicaoDePoder> Todos = new List<DefinicaoDePoder>();
        private static readonly Dictionary<string, DefinicaoDePoder> PorId =
            new Dictionary<string, DefinicaoDePoder>();

        static CatalogoDePoderes()
        {
            Registrar(
                new DefinicaoDePoder
                {
                    Id = PoderPeito,
                    Nome = "Investida Marcante",
                    Categoria = CategoriaDePoder.PreparacaoEConsumo,
                    Descricao =
                        "A investida marca os inimigos atingidos por "
                        + ConfigPoderes.MarcaDaInvestidaMs / 1000
                        + " s.",
                    Afeta = "Skills de investida",
                    Encaixes = new[] { TipoEquipamento.Peito },
                    Regras = new[]
                    {
                        new RegraDePoder
                        {
                            Id = "marcar",
                            Gatilho = GatilhoDePoder.AoAcertarComSkill,
                            Skill = SeletorDeSkill.PorEtiqueta(EtiquetaInvestida),
                            Acao = AcaoDePoder.MarcarAlvos,
                            Estado = MarcaDaInvestida,
                            DuracaoMs = ConfigPoderes.MarcaDaInvestidaMs,
                        },
                    },
                }
            );
            Registrar(
                new DefinicaoDePoder
                {
                    Id = PoderLuvas,
                    Nome = "Embalo de Golpes",
                    Categoria = CategoriaDePoder.Gatilho,
                    Descricao =
                        "Golpes seguidos dão uma carga de Embalo (até "
                        + ConfigPoderes.EmbaloMaximoDeCargas
                        + "); a próxima skill física gasta as cargas e causa +"
                        + Percentual(ConfigPoderes.EmbaloBonusPorCarga)
                        + "% de dano por carga.",
                    Afeta = "Skills físicas",
                    Encaixes = new[] { TipoEquipamento.Luvas },
                    Regras = new[]
                    {
                        new RegraDePoder
                        {
                            Id = "carregar",
                            Gatilho = GatilhoDePoder.AoAcertarAtaqueBasico,
                            Acao = AcaoDePoder.GanharCarga,
                            Estado = Embalo,
                            DuracaoMs = ConfigPoderes.EmbaloDuracaoMs,
                            MaximoDeAcumulos = ConfigPoderes.EmbaloMaximoDeCargas,
                        },
                        new RegraDePoder
                        {
                            Id = "consumir",
                            Gatilho = GatilhoDePoder.AoUsarSkill,
                            Skill = SeletorDeSkill.PorEtiqueta(EtiquetaFisica),
                            Condicao = CondicaoDePoder.TemCarga,
                            Estado = Embalo,
                            Acao = AcaoDePoder.AmplificarDano,
                            Valor = ConfigPoderes.EmbaloBonusPorCarga,
                        },
                    },
                }
            );
            Registrar(
                new DefinicaoDePoder
                {
                    Id = PoderBotas,
                    Nome = "Esquiva Impetuosa",
                    Categoria = CategoriaDePoder.Mobilidade,
                    Descricao =
                        "Uma esquiva fortalece a próxima investida em +"
                        + Percentual(ConfigPoderes.ImpulsoBonus)
                        + "% de dano por "
                        + ConfigPoderes.ImpulsoDuracaoMs / 1000
                        + " s.",
                    Afeta = "Skills de investida",
                    Encaixes = new[] { TipoEquipamento.Botas },
                    Regras = new[]
                    {
                        new RegraDePoder
                        {
                            Id = "esquivar",
                            Gatilho = GatilhoDePoder.AoEsquivar,
                            Acao = AcaoDePoder.GanharCarga,
                            Estado = Impulso,
                            DuracaoMs = ConfigPoderes.ImpulsoDuracaoMs,
                        },
                        new RegraDePoder
                        {
                            Id = "investir",
                            Gatilho = GatilhoDePoder.AoUsarSkill,
                            Skill = SeletorDeSkill.PorEtiqueta(EtiquetaInvestida),
                            Condicao = CondicaoDePoder.TemCarga,
                            Estado = Impulso,
                            Acao = AcaoDePoder.AmplificarDano,
                            Valor = ConfigPoderes.ImpulsoBonus,
                        },
                    },
                }
            );
            Registrar(
                new DefinicaoDePoder
                {
                    Id = PoderAnel,
                    Nome = "Caçador de Marcados",
                    Categoria = CategoriaDePoder.RecargaERecursos,
                    Descricao =
                        "Acertar um inimigo marcado pela investida tira "
                        + ConfigPoderes.CacadorReducaoDeRecargaMs / 1000
                        + " s da recarga das suas outras skills físicas (a cada "
                        + ConfigPoderes.CacadorIntervaloMs / 1000
                        + " s).",
                    Afeta = "Skills físicas, em alvo marcado pela investida",
                    Encaixes = new[] { TipoEquipamento.Anel },
                    Regras = new[]
                    {
                        new RegraDePoder
                        {
                            Id = "cacar",
                            Gatilho = GatilhoDePoder.AoAcertarComSkill,
                            Skill = SeletorDeSkill.PorEtiqueta(EtiquetaFisica),
                            Condicao = CondicaoDePoder.AlvoMarcado,
                            Estado = MarcaDaInvestida,
                            Acao = AcaoDePoder.ReduzirRecarga,
                            Valor = ConfigPoderes.CacadorReducaoDeRecargaMs,
                            IntervaloMinimoMs = ConfigPoderes.CacadorIntervaloMs,
                            AlvoDaAcao = SeletorDeSkill.PorEtiqueta(EtiquetaFisica),
                        },
                    },
                }
            );
        }

        /// <summary>Todos os poderes do catálogo.</summary>
        public static IReadOnlyList<DefinicaoDePoder> Poderes => Todos;

        /// <summary>O poder pelo id, ou <c>null</c> se não existe (id vazio é "sem poder").</summary>
        public static DefinicaoDePoder? Obter(string id) =>
            !string.IsNullOrEmpty(id) && PorId.TryGetValue(id, out var poder) ? poder : null;

        /// <summary>
        /// Quais categorias de poder o modelo declara (PODER-006 e PODER-007). Nem todo modelo
        /// declara: hoje só as peças variante B, que seguem o foco de cada encaixe.
        /// </summary>
        public static CategoriaDePoder[] CategoriasDoModelo(Equipamento modelo)
        {
            if (modelo == null || modelo.Variacao != "B")
                return Array.Empty<CategoriaDePoder>();
            switch (modelo.Tipo)
            {
                case TipoEquipamento.Capacete:
                    return new[] { CategoriaDePoder.Amplificacao, CategoriaDePoder.Transformacao };
                case TipoEquipamento.Peito:
                    return new[]
                    {
                        CategoriaDePoder.Transformacao,
                        CategoriaDePoder.PreparacaoEConsumo,
                    };
                case TipoEquipamento.Luvas:
                    return new[] { CategoriaDePoder.Gatilho, CategoriaDePoder.Amplificacao };
                case TipoEquipamento.Calca:
                    return new[] { CategoriaDePoder.DefesaReativa };
                case TipoEquipamento.Botas:
                    return new[] { CategoriaDePoder.Mobilidade };
                case TipoEquipamento.Adorno:
                    return new[] { CategoriaDePoder.Amplificacao, CategoriaDePoder.Conversao };
                case TipoEquipamento.Anel:
                    return new[] { CategoriaDePoder.Gatilho, CategoriaDePoder.RecargaERecursos };
                case TipoEquipamento.Cinto:
                    return new[]
                    {
                        CategoriaDePoder.RecargaERecursos,
                        CategoriaDePoder.DefesaReativa,
                    };
                case TipoEquipamento.Brinco:
                    return new[] { CategoriaDePoder.Amplificacao };
                default:
                    return Array.Empty<CategoriaDePoder>();
            }
        }

        /// <summary>Os poderes que o modelo pode receber: categoria declarada e encaixe compatível.</summary>
        public static List<DefinicaoDePoder> Candidatos(Equipamento modelo)
        {
            var lista = new List<DefinicaoDePoder>();
            var categorias = CategoriasDoModelo(modelo);
            if (categorias.Length == 0)
                return lista;
            foreach (var poder in Todos)
            {
                if (poder.Aceita(modelo.Tipo) && Array.IndexOf(categorias, poder.Categoria) >= 0)
                    lista.Add(poder);
            }
            return lista;
        }

        /// <summary>
        /// Sorteia o poder do exemplar entre os candidatos do modelo (PODER-006).
        /// <paramref name="sorteio"/> devolve de 0 (inclusive) ao limite recebido (exclusive).
        /// Vazio quando o modelo não declara categorias ou não há poder válido.
        /// </summary>
        public static string Sortear(Equipamento modelo, Func<int, int> sorteio)
        {
            var candidatos = Candidatos(modelo);
            return candidatos.Count == 0 ? "" : candidatos[sorteio(candidatos.Count)].Id;
        }

        private static string Percentual(double fracao) =>
            Math.Round(fracao * 100, MidpointRounding.AwayFromZero).ToString();

        private static void Registrar(DefinicaoDePoder poder)
        {
            Todos.Add(poder);
            PorId[poder.Id] = poder;
        }
    }
}
