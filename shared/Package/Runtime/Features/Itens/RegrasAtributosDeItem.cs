using System;
using System.Collections.Generic;
using System.Globalization;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Do atributo do item ao número de combate (ATRIB-002 a ATRIB-005). Funções puras; o servidor
    /// decide, o client só mostra os textos. Nenhum valor do item soma nos atributos do
    /// personagem: cada fonte vira número pela sua própria conversão.
    /// </summary>
    public static class RegrasAtributosDeItem
    {
        /// <summary>
        /// Resume o que as peças ativas dão ao combate. Conta a peça vestida (fora da bolsa) e,
        /// das armas, só as do <paramref name="conjunto"/> ativo; o dano-base vem da arma da mão
        /// principal. Atributo que não é elegível na categoria da peça (legado) não vale.
        /// </summary>
        public static AtributosDosEquipamentos Resumir(
            IEnumerable<EntradaInventario> entradas,
            int conjunto
        )
        {
            var resumo = new AtributosDosEquipamentos();
            var somas = new SortedDictionary<StatusItem, double>();
            double vida = 0;
            double stamina = 0;
            double mana = 0;
            foreach (var entrada in entradas)
            {
                if (!RegrasEquipamentos.EstaAtiva(entrada, conjunto))
                {
                    continue;
                }

                var item = entrada.Equipamento;
                var ehArmaPrincipal =
                    entrada.Posicao == RegrasEquipamentos.Principal(conjunto)
                    && ConfigAtributosItem.EhArma(item.Tipo);
                if (ehArmaPrincipal)
                {
                    resumo.TemArma = true;
                    resumo.ArmaMagica = ConfigAtributosItem.ArmaMagica(item.Tipo);
                }

                foreach (var atributo in item.Atributos ?? new AtributoItem[0])
                {
                    var convertido = ConfigAtributosItem.Converter(
                        item.Tipo,
                        atributo.Status,
                        atributo.Valor
                    );
                    if (!ConfigAtributosItem.Elegivel(item.Tipo, atributo.Status))
                    {
                        continue;
                    }

                    switch (atributo.Status)
                    {
                        case StatusItem.DanoDaArma:
                            if (!ehArmaPrincipal)
                            {
                                continue;
                            }

                            resumo.DanoDaArma += convertido;
                            break;
                        case StatusItem.Forca:
                            resumo.PercentualForca += convertido;
                            break;
                        case StatusItem.Intelecto:
                            resumo.PercentualIntelecto += convertido;
                            break;
                        case StatusItem.Vigor:
                            vida += convertido;
                            break;
                        case StatusItem.Agilidade:
                            stamina += convertido;
                            break;
                        case StatusItem.Espirito:
                            mana += convertido;
                            break;
                        case StatusItem.DefesaFisica:
                            resumo.DefesaFisica += convertido;
                            break;
                        case StatusItem.DefesaMagica:
                            resumo.DefesaMagica += convertido;
                            break;
                        default:
                            continue; // Secundário: vai pela pipeline, não por aqui.
                    }

                    somas.TryGetValue(atributo.Status, out var atual);
                    somas[atributo.Status] = atual + convertido;
                }
            }

            resumo.VidaMaxima = (int)Math.Round(vida, MidpointRounding.AwayFromZero);
            resumo.StaminaMaxima = (int)Math.Round(stamina, MidpointRounding.AwayFromZero);
            resumo.ManaMaxima = (int)Math.Round(mana, MidpointRounding.AwayFromZero);

            var linhas = new List<LinhaDeEquipamento>();
            foreach (var soma in somas)
            {
                linhas.Add(
                    new LinhaDeEquipamento
                    {
                        Status = soma.Key,
                        Valor = soma.Value,
                        Texto =
                            Nome(soma.Key)
                            + ": "
                            + Efeito(soma.Key, soma.Value, resumo.ArmaMagica, semSinal: true),
                    }
                );
            }

            resumo.Linhas = linhas.ToArray();
            return resumo;
        }

        /// <summary>Se o atributo é um dos nove secundários (chega ao combate pela pipeline).</summary>
        public static bool EhSecundario(StatusItem status)
        {
            switch (status)
            {
                case StatusItem.RegeneracaoVida:
                case StatusItem.RegeneracaoStamina:
                case StatusItem.RegeneracaoMana:
                case StatusItem.ChanceCritica:
                case StatusItem.DanoCritico:
                case StatusItem.VelocidadeAtaqueFisico:
                case StatusItem.VelocidadeAtaqueMagico:
                case StatusItem.PenetracaoFisica:
                case StatusItem.PenetracaoMagica:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>O nome do atributo do item, como o jogador lê.</summary>
        public static string Nome(StatusItem status)
        {
            switch (status)
            {
                case StatusItem.Forca:
                    return "Força";
                case StatusItem.Intelecto:
                    return "Intelecto";
                case StatusItem.Espirito:
                    return "Espírito";
                case StatusItem.Vigor:
                    return "Vigor";
                case StatusItem.Agilidade:
                    return "Agilidade";
                case StatusItem.DanoDaArma:
                    return "Dano da arma";
                case StatusItem.DefesaFisica:
                    return "Defesa física";
                case StatusItem.DefesaMagica:
                    return "Defesa mágica";
                case StatusItem.RegeneracaoVida:
                    return "Regeneração de vida";
                case StatusItem.RegeneracaoStamina:
                    return "Regeneração de stamina";
                case StatusItem.RegeneracaoMana:
                    return "Regeneração de mana";
                case StatusItem.ChanceCritica:
                    return "Chance crítica";
                case StatusItem.DanoCritico:
                    return "Dano crítico";
                case StatusItem.VelocidadeAtaqueFisico:
                    return "Velocidade de ataque físico";
                case StatusItem.VelocidadeAtaqueMagico:
                    return "Velocidade de ataque mágico";
                case StatusItem.PenetracaoFisica:
                    return "Penetração física";
                case StatusItem.PenetracaoMagica:
                    return "Penetração mágica";
                case StatusItem.PotenciaFogo:
                    return "Potência de fogo (%)";
                case StatusItem.PotenciaGelo:
                    return "Potência de gelo (%)";
                case StatusItem.Alcance:
                    return "Alcance (m)";
                default:
                    return status.ToString();
            }
        }

        /// <summary>
        /// O que o número de combate faz, em palavras: "+7% de dano físico", "+120 de vida
        /// máxima". <paramref name="magica"/> diz se o dano da arma é mágico (só o dano da arma
        /// usa). Atributo legado sem efeito devolve o valor puro.
        /// </summary>
        public static string Efeito(
            StatusItem status,
            double convertido,
            bool magica = false,
            bool semSinal = false
        )
        {
            var sinal = semSinal ? "" : "+";
            switch (status)
            {
                case StatusItem.DanoDaArma:
                    return Numero(convertido) + (magica ? " de dano mágico" : " de dano físico");
                case StatusItem.Forca:
                    return sinal + Percentual(convertido) + " de dano físico";
                case StatusItem.Intelecto:
                    return sinal + Percentual(convertido) + " de dano mágico";
                case StatusItem.Vigor:
                    return sinal + Numero(convertido) + " de vida máxima";
                case StatusItem.Agilidade:
                    return sinal + Numero(convertido) + " de stamina máxima";
                case StatusItem.Espirito:
                    return sinal + Numero(convertido) + " de mana máxima";
                case StatusItem.DefesaFisica:
                case StatusItem.DefesaMagica:
                    return sinal + Numero(convertido) + " de defesa";
                case StatusItem.RegeneracaoVida:
                case StatusItem.RegeneracaoStamina:
                case StatusItem.RegeneracaoMana:
                    return sinal + Numero(convertido) + "/s";
                case StatusItem.ChanceCritica:
                case StatusItem.DanoCritico:
                case StatusItem.VelocidadeAtaqueFisico:
                case StatusItem.VelocidadeAtaqueMagico:
                case StatusItem.PenetracaoFisica:
                case StatusItem.PenetracaoMagica:
                    return sinal + Percentual(convertido);
                default:
                    return Numero(convertido);
            }
        }

        private static string Percentual(double fracao) => Numero(fracao * 100) + "%";

        // Até uma casa decimal, com vírgula, igual aos textos dos secundários.
        private static string Numero(double valor) =>
            valor.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',');
    }
}
