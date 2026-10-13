using System.Globalization;
using System.Text;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Textos dos atributos secundários na janela do personagem (docs/features/personagem.md,
    /// "Atributos secundários"). Só apresentação: o servidor calcula os valores e a origem.
    /// </summary>
    internal static class TextosSecundarios
    {
        public const string Titulo = "Atributos secundários";
        public const string Carregando = "Carregando atributos secundários...";
        public const string Erro = "Não foi possível carregar os atributos secundários.";

        public static string Rotulo(AtributoSecundario atributo)
        {
            switch (atributo)
            {
                case AtributoSecundario.RegeneracaoVida:
                    return "Regeneração de vida";
                case AtributoSecundario.RegeneracaoStamina:
                    return "Regeneração de stamina";
                case AtributoSecundario.RegeneracaoMana:
                    return "Regeneração de mana";
                case AtributoSecundario.ChanceCritica:
                    return "Chance crítica";
                case AtributoSecundario.DanoCritico:
                    return "Dano crítico";
                case AtributoSecundario.VelocidadeAtaqueFisico:
                    return "Velocidade de ataque físico";
                case AtributoSecundario.VelocidadeAtaqueMagico:
                    return "Velocidade de ataque mágico";
                case AtributoSecundario.PenetracaoFisica:
                    return "Penetração física";
                default:
                    return "Penetração mágica";
            }
        }

        public static string Fonte(FonteDeModificador fonte)
        {
            switch (fonte)
            {
                case FonteDeModificador.Base:
                    return "Base";
                case FonteDeModificador.Personagem:
                    return "Personagem";
                case FonteDeModificador.Arvore:
                    return "Árvore";
                case FonteDeModificador.Equipamento:
                    return "Equipamento";
                case FonteDeModificador.Efeito:
                    return "Efeito";
                default:
                    return "Alvo";
            }
        }

        /// <summary>O valor final: "10/s" para regeneração, "5%" para os demais.</summary>
        public static string Valor(AtributoSecundario atributo, double valor)
        {
            return EhRegeneracao(atributo) ? PorSegundo(valor) : Percentual(valor);
        }

        /// <summary>"Base 10/s · Árvore: Nó de teste +2/s", na ordem em que as parcelas entraram.</summary>
        public static string Origem(SecundarioCalculado calculado)
        {
            if (calculado == null || calculado.Parcelas == null || calculado.Parcelas.Length == 0)
            {
                return "";
            }

            var texto = new StringBuilder();
            foreach (var parcela in calculado.Parcelas)
            {
                if (texto.Length > 0)
                {
                    texto.Append(" · ");
                }

                texto.Append(Fonte(parcela.Fonte));
                if (
                    parcela.Fonte != FonteDeModificador.Base
                    && !string.IsNullOrEmpty(parcela.Origem)
                )
                {
                    texto.Append(": ").Append(parcela.Origem);
                }

                texto.Append(' ').Append(Parcela(calculado.Atributo, parcela));
            }

            return texto.ToString();
        }

        private static string Parcela(AtributoSecundario atributo, Modificador parcela)
        {
            // Multiplicação é sempre um bônus em % do que já havia (0,10 = +10%).
            var multiplica = parcela.Operacao == OperacaoDeModificador.Multiplica;
            var texto =
                multiplica ? Percentual(parcela.Valor)
                : EhRegeneracao(atributo) ? PorSegundo(parcela.Valor)
                : Percentual(parcela.Valor);
            if (parcela.Fonte != FonteDeModificador.Base && parcela.Valor >= 0)
            {
                return "+" + texto;
            }

            return texto;
        }

        private static bool EhRegeneracao(AtributoSecundario atributo)
        {
            return atributo == AtributoSecundario.RegeneracaoVida
                || atributo == AtributoSecundario.RegeneracaoStamina
                || atributo == AtributoSecundario.RegeneracaoMana;
        }

        private static string PorSegundo(double valor)
        {
            return Numero(valor) + "/s";
        }

        private static string Percentual(double fracao)
        {
            return Numero(fracao * 100) + "%";
        }

        // Até uma casa decimal, com vírgula.
        private static string Numero(double valor)
        {
            return valor.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',');
        }
    }
}
