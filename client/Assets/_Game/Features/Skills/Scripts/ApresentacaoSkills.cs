using JogoDoGrupo.Shared.Features.Itens;
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
            skill == null
                ? ""
                : skill.Custo
                    + " "
                    + (skill.Recurso == RecursoDeSkill.Mana ? RotuloMana : RotuloStamina);

        /// <summary>"30 Stamina" ou "35 Mana" formatado da definição do catálogo.</summary>
        public static string Custo(DefinicaoDeSkill skill) =>
            skill == null
                ? ""
                : skill.Custo
                    + " "
                    + (skill.Recurso == RecursoDeSkill.Mana ? RotuloMana : RotuloStamina);

        /// <summary>Nome de exibição amigável do tipo de arma.</summary>
        public static string NomeDaArma(TipoEquipamento tipo)
        {
            switch (tipo)
            {
                case TipoEquipamento.Espada:
                    return "Espada";
                case TipoEquipamento.Martelo:
                    return "Martelo";
                case TipoEquipamento.Arco:
                    return "Arco";
                case TipoEquipamento.Cajado:
                    return "Cajado";
                case TipoEquipamento.Foco:
                    return "Foco";
                case TipoEquipamento.Escudo:
                    return "Escudo";
                default:
                    return tipo.ToString();
            }
        }

        /// <summary>Tempo total de recarga em segundos com sufixo ("8 s", "14 s").</summary>
        public static string RecargaEmSegundos(int recargaMs) => $"{recargaMs / 1000f:0.#} s";

        /// <summary>Multiplicador percentual do dano base ("130%", "200%").</summary>
        public static string DescricaoDano(DefinicaoDeSkill skill)
        {
            if (skill == null)
            {
                return "";
            }

            var perc = (int)System.Math.Round(skill.Multiplicador * 100);
            return $"{perc}% do dano base";
        }

        /// <summary>Nome em português da categoria de dano ("Física", "Mágica", "Híbrida").</summary>
        public static string DescricaoCategoria(DefinicaoDeSkill skill)
        {
            if (skill == null || skill.Propriedades == null)
            {
                return "Física";
            }

            switch (skill.Propriedades.Categoria)
            {
                case JogoDoGrupo.Shared.Features.Combate.CategoriaDeDano.Magica:
                    return "Mágica";
                case JogoDoGrupo.Shared.Features.Combate.CategoriaDeDano.Ambas:
                    return "Híbrida";
                default:
                    return "Física";
            }
        }

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

        /// <summary>
        /// O aviso de que um poder ou bônus de set disparou ("Investida Marcante: alvo marcado",
        /// "Embalo de Golpes: +20% de dano"). Só formata o que o servidor mandou.
        /// </summary>
        public static string TextoDoPoder(PoderDisparou aviso)
        {
            string efeito;
            switch (aviso.Acao)
            {
                case AcaoDePoder.MarcarAlvos:
                    efeito = aviso.Valor == 1 ? "alvo marcado" : aviso.Valor + " alvos marcados";
                    break;
                case AcaoDePoder.GanharCarga:
                    efeito = aviso.Valor + (aviso.Valor == 1 ? " carga" : " cargas");
                    break;
                case AcaoDePoder.AmplificarDano:
                    efeito = "+" + aviso.Valor + "% de dano";
                    break;
                case AcaoDePoder.ReduzirRecarga:
                    efeito = "-" + (aviso.Valor / 1000f).ToString("0.#") + " s de recarga";
                    break;
                default:
                    efeito = "ativo";
                    break;
            }

            return aviso.Nome + ": " + efeito;
        }

        /// <summary>O texto da barra de preparo: nome e a dica de como cancelar e o que custa.</summary>
        public static string TextoDoPreparo(string nome, int espaco) =>
            nome
            + " - "
            + Tecla(espaco)
            + " cancela (custa "
            + (int)(ConfigSkills.FracaoDoCustoAoCancelar * 100)
            + "%)";

        /// <summary>O aviso depois de cancelar: o que custou ("Preparo cancelado: -20 Stamina").</summary>
        public static string TextoDoCancelamento(SkillCancelada cancelada, DefinicaoDeSkill skill)
        {
            if (cancelada == null || cancelada.CustoCobrado <= 0 || skill == null)
            {
                return "Preparo cancelado";
            }

            return "Preparo cancelado: -"
                + cancelada.CustoCobrado
                + " "
                + (skill.Recurso == RecursoDeSkill.Mana ? RotuloMana : RotuloStamina);
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
                    return "Ação em andamento";
                default:
                    return "Sem skill nesse espaço";
            }
        }
    }
}
