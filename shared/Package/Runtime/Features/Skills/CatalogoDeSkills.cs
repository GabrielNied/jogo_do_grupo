#nullable enable annotations

using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Features.Skills
{
    /// <summary>
    /// O catálogo inicial de skills de arma (nomes e custos do catálogo de proposta em
    /// docs/features/skills.md, números de teste). O conjunto de cada arma vem do seu
    /// modelo-base: o tipo da arma (SKILL-004). Todas causam dano nesta fatia; os outros
    /// efeitos (cura, escudo, controle) entram com os poderes e os estados do Combate.
    /// </summary>
    public static class CatalogoDeSkills
    {
        private static readonly Dictionary<string, DefinicaoDeSkill> PorId =
            new Dictionary<string, DefinicaoDeSkill>();

        private static readonly Dictionary<TipoEquipamento, DefinicaoDeSkill[]> PorTipo =
            new Dictionary<TipoEquipamento, DefinicaoDeSkill[]>();

        static CatalogoDeSkills()
        {
            Registrar(
                TipoEquipamento.Espada,
                Fisica("espada.corte-veloz", "Corte Veloz", 15, 8, 1.3),
                Fisica("espada.ruptura-sangrenta", "Ruptura Sangrenta", 20, 12, 1.6, true),
                Fisica("espada.lamina-giratoria", "Lâmina Giratória", 30, 14, 2.0),
                Fisica("espada.avanco-devastador", "Avanço Devastador", 25, 15, 1.8),
                Fisica("espada.golpe-do-carrasco", "Golpe do Carrasco", 40, 22, 3.0, true)
            );
            Registrar(
                TipoEquipamento.Martelo,
                Fisica("martelo.pancada-atordoante", "Pancada Atordoante", 20, 10, 1.5),
                Fisica("martelo.martelada-sismica", "Martelada Sísmica", 25, 12, 2.2),
                Fisica("martelo.onda-de-choque", "Onda de Choque", 30, 15, 2.0),
                Fisica("martelo.esmagar", "Esmagar", 35, 18, 2.8),
                Fisica("martelo.queda-do-tita", "Queda do Titã", 40, 22, 3.2)
            );
            Registrar(
                TipoEquipamento.Arco,
                Fisica("arco.flecha-sinal", "Flecha-Sinal", 10, 8, 1.2),
                Fisica("arco.recuo-do-cacador", "Recuo do Caçador", 20, 12, 1.4),
                Fisica("arco.disparo-perfurante", "Disparo Perfurante", 25, 14, 2.2, true),
                Hibrida("arco.linha-incendiaria", "Linha Incendiária", 30, 18, 2.4),
                Fisica("arco.chuva-de-brasas", "Chuva de Brasas", 40, 25, 3.0)
            );
            Registrar(
                TipoEquipamento.Cajado,
                Magica("cajado.estilhaco-glacial", "Estilhaço Glacial", 20, 10, 1.8),
                Magica("cajado.raio-arcano", "Raio Arcano", 25, 12, 2.0, true),
                Magica("cajado.nova-de-fogo", "Nova de Fogo", 30, 16, 2.4),
                Magica("cajado.orbes-da-aurora", "Orbes da Aurora", 35, 20, 2.6),
                Magica("cajado.surto-solar", "Surto Solar", 50, 30, 4.0)
            );
            Registrar(
                TipoEquipamento.Foco,
                Magica("foco.pulso-arcano", "Pulso Arcano", 15, 8, 1.3),
                Magica("foco.dardo-ressonante", "Dardo Ressonante", 20, 10, 1.7, true),
                Magica("foco.lanca-de-energia", "Lança de Energia", 30, 14, 2.2),
                Magica("foco.selo-explosivo", "Selo Explosivo", 35, 20, 2.8),
                Magica("foco.vortice", "Vórtice", 45, 28, 3.4)
            );
            Registrar(
                TipoEquipamento.Escudo,
                Fisica("escudo.pancada-de-escudo", "Pancada de Escudo", 15, 8, 1.2),
                Fisica("escudo.avanco-de-muralha", "Avanço de Muralha", 30, 16, 1.8),
                Fisica("escudo.desafio-do-guardiao", "Desafio do Guardião", 25, 18, 1.6),
                Fisica("escudo.postura-de-ferro", "Postura de Ferro", 20, 20, 1.4)
            );

            // Skills que levam o personagem até o alvo (os poderes de investida se ligam a elas).
            Etiquetar("espada.avanco-devastador", EtiquetasDeSkill.Investida);
            Etiquetar("escudo.avanco-de-muralha", EtiquetasDeSkill.Investida);
        }

        private static void Etiquetar(string id, string etiqueta)
        {
            var skill = PorId[id];
            var etiquetas = new List<string>(skill.Etiquetas) { etiqueta };
            skill.Etiquetas = etiquetas.ToArray();
        }

        // A etiqueta de dano sai da categoria; a híbrida leva as duas.
        private static string[] EtiquetasDaCategoria(CategoriaDeDano categoria) =>
            categoria == CategoriaDeDano.Fisica ? new[] { EtiquetasDeSkill.Fisica }
            : categoria == CategoriaDeDano.Magica ? new[] { EtiquetasDeSkill.Magica }
            : new[] { EtiquetasDeSkill.Fisica, EtiquetasDeSkill.Magica };

        /// <summary>Os tipos de equipamento que possuem conjunto de skills catalogadas.</summary>
        public static readonly TipoEquipamento[] TiposComSkills = new[]
        {
            TipoEquipamento.Espada,
            TipoEquipamento.Martelo,
            TipoEquipamento.Arco,
            TipoEquipamento.Cajado,
            TipoEquipamento.Foco,
            TipoEquipamento.Escudo,
        };

        /// <summary>O conjunto do modelo-base de um tipo de arma; vazio se o tipo não tem skills.</summary>
        public static IReadOnlyList<DefinicaoDeSkill> Pool(TipoEquipamento tipo) =>
            PorTipo.TryGetValue(tipo, out var pool) ? pool : Array.Empty<DefinicaoDeSkill>();

        /// <summary>A skill pelo id, ou <c>null</c> se o id não existe no catálogo.</summary>
        public static DefinicaoDeSkill? Obter(string? id) =>
            id != null && PorId.TryGetValue(id, out var skill) ? skill : null;

        /// <summary>Identifica a qual tipo de arma a skill pertence, ou <c>null</c> se não encontrada.</summary>
        public static TipoEquipamento? TipoDaSkill(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (var par in PorTipo)
            {
                foreach (var skill in par.Value)
                {
                    if (skill.Id == id)
                    {
                        return par.Key;
                    }
                }
            }

            return null;
        }

        private static void Registrar(TipoEquipamento tipo, params DefinicaoDeSkill[] skills)
        {
            PorTipo[tipo] = skills;
            foreach (var skill in skills)
            {
                PorId[skill.Id] = skill;
            }
        }

        private static DefinicaoDeSkill Fisica(
            string id,
            string nome,
            int custo,
            int recargaSegundos,
            double multiplicador,
            bool critica = false
        ) =>
            Criar(
                id,
                nome,
                RecursoDeSkill.Stamina,
                custo,
                recargaSegundos,
                multiplicador,
                CategoriaDeDano.Fisica,
                critica
            );

        private static DefinicaoDeSkill Magica(
            string id,
            string nome,
            int custo,
            int recargaSegundos,
            double multiplicador,
            bool critica = false
        ) =>
            Criar(
                id,
                nome,
                RecursoDeSkill.Mana,
                custo,
                recargaSegundos,
                multiplicador,
                CategoriaDeDano.Magica,
                critica
            );

        // Híbrida: gasta stamina e divide o dano meio a meio entre físico e mágico.
        private static DefinicaoDeSkill Hibrida(
            string id,
            string nome,
            int custo,
            int recargaSegundos,
            double multiplicador
        ) =>
            Criar(
                id,
                nome,
                RecursoDeSkill.Stamina,
                custo,
                recargaSegundos,
                multiplicador,
                CategoriaDeDano.Ambas,
                true
            );

        private static DefinicaoDeSkill Criar(
            string id,
            string nome,
            RecursoDeSkill recurso,
            int custo,
            int recargaSegundos,
            double multiplicador,
            CategoriaDeDano categoria,
            bool critica
        ) =>
            new DefinicaoDeSkill
            {
                Id = id,
                Nome = nome,
                Recurso = recurso,
                Custo = custo,
                RecargaMs = Math.Max(ConfigSkills.RecargaMinimaMs, recargaSegundos * 1000),
                Multiplicador = multiplicador,
                PreparoMs =
                    multiplicador >= ConfigSkills.MultiplicadorDeSkillForte
                        ? ConfigSkills.PreparoDaSkillForteMs
                        : 0,
                Etiquetas = EtiquetasDaCategoria(categoria),
                Propriedades = new PropriedadesDaSkill
                {
                    Id = id,
                    Categoria = categoria,
                    Tipo = TipoDeEfeito.Dano,
                    PodeCritar = critica,
                    RecebePenetracao = critica,
                    PodeSerAcelerada = true,
                    FracaoFisica = 0.5,
                },
            };
    }
}
