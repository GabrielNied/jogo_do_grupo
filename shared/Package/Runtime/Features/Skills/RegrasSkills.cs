using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Features.Skills
{
    /// <summary>
    /// Regras puras de Skills: o sorteio das skills de uma arma e o dano do uso. O servidor é
    /// quem sorteia e decide; o client só mostra.
    /// </summary>
    public static class RegrasSkills
    {
        /// <summary>
        /// Sorteia as <see cref="ConfigSkills.SkillsPorArma"/> skills de uma arma do pool do seu
        /// modelo-base (<paramref name="tipo"/>), sem repetir. <paramref name="sorteio"/> devolve
        /// um inteiro de 0 (inclusive) ao limite recebido (exclusive). Tipo sem skills: vazio.
        /// </summary>
        public static string[] Sortear(TipoEquipamento tipo, Func<int, int> sorteio)
        {
            var restantes = new List<DefinicaoDeSkill>(CatalogoDeSkills.Pool(tipo));
            var quantidade = Math.Min(ConfigSkills.SkillsPorArma, restantes.Count);
            var ids = new string[quantidade];
            for (int i = 0; i < quantidade; i++)
            {
                var indice = sorteio(restantes.Count);
                ids[i] = restantes[indice].Id;
                restantes.RemoveAt(indice);
            }
            return ids;
        }

        /// <summary>
        /// Se os ids gravados num exemplar ainda valem para a arma: a quantidade certa, todos do
        /// pool do tipo e sem repetição. Um catálogo que mudou invalida o gravado (sorteia de novo).
        /// </summary>
        public static bool IdsValidos(TipoEquipamento tipo, IReadOnlyList<string> ids)
        {
            var pool = CatalogoDeSkills.Pool(tipo);
            if (ids == null || ids.Count != Math.Min(ConfigSkills.SkillsPorArma, pool.Count))
                return false;
            var vistos = new HashSet<string>();
            foreach (var id in ids)
            {
                var pertence = false;
                foreach (var skill in pool)
                    pertence |= skill.Id == id;
                if (!pertence || !vistos.Add(id))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Dano da skill: <c>(dano da arma + flat dos itens) x multiplicador</c>, arredondado e
        /// nunca menor que 1. O flat é o físico ou o mágico conforme a categoria da skill; na
        /// híbrida, cada parte leva o seu.
        /// </summary>
        public static int Dano(
            DefinicaoDeSkill skill,
            double danoDaArma,
            double flatFisico,
            double flatMagico
        )
        {
            var fisica = RegrasDeSkill.FracaoFisica(skill.Propriedades);
            var flat = flatFisico * fisica + flatMagico * (1 - fisica);
            var bruto = (Math.Max(0, danoDaArma) + Math.Max(0, flat)) * skill.Multiplicador;
            var dano = Math.Round(bruto, MidpointRounding.AwayFromZero);
            return (int)Math.Max(1, Math.Min(dano, int.MaxValue));
        }
    }
}
