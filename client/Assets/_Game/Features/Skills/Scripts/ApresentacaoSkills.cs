using JogoDoGrupo.Shared.Features.Skills;

namespace JogoDoGrupo.Client.Features.Skills
{
    /// <summary>
    /// Os textos da barra de skills. Só formata o que o servidor mandou: nada é decidido aqui.
    /// </summary>
    internal static class ApresentacaoSkills
    {
        public const string TextoSemArma = "Equipe uma arma para ter skills";
        public const string RotuloStamina = "Stamina";
        public const string RotuloMana = "Mana";

        /// <summary>Rótulo da tecla do espaço (0, 1, 2 viram 1, 2, 3).</summary>
        public static string Tecla(int espaco) => (espaco + 1).ToString();

        /// <summary>"30 Stamina" ou "35 Mana".</summary>
        public static string Custo(SkillDaBarra skill) =>
            skill.Custo + " " + (skill.Recurso == RecursoDeSkill.Mana ? RotuloMana : RotuloStamina);

        /// <summary>Segundos que faltam da recarga, arredondados para cima ("12"); vazio se pronta.</summary>
        public static string Recarga(int restanteMs)
        {
            if (restanteMs <= 0)
            {
                return "";
            }

            return ((restanteMs + 999) / 1000).ToString();
        }

        /// <summary>Quanto da recarga ainda falta, de 0 (pronta) a 1 (acabou de usar).</summary>
        public static float FracaoDaRecarga(int restanteMs, int recargaMs)
        {
            if (restanteMs <= 0 || recargaMs <= 0)
            {
                return 0f;
            }

            var fracao = (float)restanteMs / recargaMs;
            return fracao > 1f ? 1f : fracao;
        }

        /// <summary>A mensagem de uma recusa do servidor, em português para o jogador.</summary>
        public static string TextoDaRecusa(string codigo)
        {
            switch (codigo)
            {
                case CodigosErroSkills.EmRecarga:
                    return "Skill em recarga";
                case CodigosErroSkills.RecursoInsuficiente:
                    return "Falta stamina ou mana";
                case CodigosErroSkills.VoceEstaCaido:
                    return "Você está caído";
                case CodigosErroSkills.AcaoEmAndamento:
                    return "Trocando de arma";
                default:
                    return "Sem skill nesse espaço";
            }
        }
    }
}
