using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Como os secundários se aplicam a uma skill, a partir das propriedades que ela declara.
    /// Puras: o sorteio do crítico vem de fora (o servidor sorteia uma vez por golpe).
    /// <para>
    /// Skill híbrida: o dano se divide em parte física e parte mágica
    /// (<see cref="PropriedadesDaSkill.FracaoFisica"/>); cada parte recebe só a penetração e a
    /// defesa do seu tipo; o crítico é um sorteio só por golpe e multiplica o total; a velocidade
    /// é a da categoria dominante (empate: física).
    /// </para>
    /// </summary>
    public static class RegrasDeSkill
    {
        /// <summary>Parte física do dano de 0 a 1 (1 física, 0 mágica, a declarada na híbrida).</summary>
        public static double FracaoFisica(PropriedadesDaSkill skill)
        {
            switch (skill.Categoria)
            {
                case CategoriaDeDano.Fisica:
                    return 1;
                case CategoriaDeDano.Magica:
                    return 0;
                default:
                    return Math.Min(1, Math.Max(0, skill.FracaoFisica));
            }
        }

        /// <summary>Chance de crítico da skill: a do personagem com a flag, 0 sem ela.</summary>
        public static double ChanceCritica(PropriedadesDaSkill skill, AtributosSecundarios s) =>
            skill.PodeCritar ? s.ChanceCritica : 0;

        /// <summary>
        /// Multiplicador do crítico. <paramref name="sorteio"/> é um número de 0 (inclusive) a 1
        /// (exclusive), um por golpe: critica se for menor que a chance. Sem a flag, sempre 1.
        /// </summary>
        public static double MultiplicadorCritico(
            PropriedadesDaSkill skill,
            AtributosSecundarios s,
            double sorteio
        ) => sorteio < ChanceCritica(skill, s) ? s.DanoCritico : 1;

        /// <summary>Penetração física que a skill usa: a do personagem com a flag, 0 sem ela.</summary>
        public static double PenetracaoFisica(PropriedadesDaSkill skill, AtributosSecundarios s) =>
            skill.RecebePenetracao ? s.PenetracaoFisica : 0;

        /// <summary>Penetração mágica que a skill usa: a do personagem com a flag, 0 sem ela.</summary>
        public static double PenetracaoMagica(PropriedadesDaSkill skill, AtributosSecundarios s) =>
            skill.RecebePenetracao ? s.PenetracaoMagica : 0;

        /// <summary>Defesa que sobra depois de <paramref name="penetracao"/> (fração ignorada).</summary>
        public static double DefesaEfetiva(double defesa, double penetracao) =>
            Math.Max(0, defesa) * (1 - Math.Min(1, Math.Max(0, penetracao)));

        /// <summary>Mitigação provisória (teste): <c>dano * K / (K + defesa)</c>.</summary>
        public static double Mitigar(double dano, double defesa) =>
            dano
            * ConfigAtributosSecundarios.ConstanteDeMitigacao
            / (ConfigAtributosSecundarios.ConstanteDeMitigacao + Math.Max(0, defesa));

        /// <summary>
        /// Dano da skill depois da defesa do alvo, sem crítico: divide o dano pela categoria, tira
        /// a penetração do tipo de cada parte e mitiga cada parte pela defesa do mesmo tipo.
        /// </summary>
        public static double DanoAposDefesa(
            PropriedadesDaSkill skill,
            AtributosSecundarios s,
            double dano,
            double defesaFisica,
            double defesaMagica
        )
        {
            double fisica = FracaoFisica(skill);
            double parteFisica = dano * fisica;
            double parteMagica = dano * (1 - fisica);
            return Mitigar(parteFisica, DefesaEfetiva(defesaFisica, PenetracaoFisica(skill, s)))
                + Mitigar(parteMagica, DefesaEfetiva(defesaMagica, PenetracaoMagica(skill, s)));
        }

        /// <summary>
        /// Velocidade da skill: a da categoria (a dominante na híbrida) com a flag, 1 sem ela.
        /// </summary>
        public static double Velocidade(PropriedadesDaSkill skill, AtributosSecundarios s)
        {
            if (!skill.PodeSerAcelerada)
            {
                return 1;
            }
            return FracaoFisica(skill) >= 0.5 ? s.VelocidadeAtaqueFisico : s.VelocidadeAtaqueMagico;
        }
    }
}
