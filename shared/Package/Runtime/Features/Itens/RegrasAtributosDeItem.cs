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
        public static AtributosDosEquipamentos Resumir(
            IEnumerable<EntradaInventario> entradas,
            int conjunto
        )
        {
            var resumo = new AtributosDosEquipamentos();
            var somas = new SortedDictionary<StatusItem, double>();
            double vida = 0;
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
                        case StatusItem.DanoFisico:
                            resumo.BonusDanoFisico += convertido;
                            break;
                        case StatusItem.DanoMagico:
                            resumo.BonusDanoMagico += convertido;
                            break;
                        case StatusItem.VidaMaxima:
                            vida += convertido;
                            break;
                        case StatusItem.DefesaFisica:
                            resumo.DefesaFisica += convertido;
                            break;
                        case StatusItem.DefesaMagica:
                            resumo.DefesaMagica += convertido;
                            break;
                        default:
                            continue;
                    }

                    somas.TryGetValue(atributo.Status, out var atual);
                    somas[atributo.Status] = atual + convertido;
                }
            }

            resumo.VidaMaxima = (int)Math.Round(vida, MidpointRounding.AwayFromZero);

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

        public static string Nome(StatusItem status)
        {
            switch (status)
            {
                case StatusItem.DanoFisico:
                    return "Dano físico";
                case StatusItem.DanoMagico:
                    return "Dano mágico";
                case StatusItem.VidaMaxima:
                    return "Vida";
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
                case StatusItem.Forca:
                    return "Força (legado)";
                case StatusItem.Intelecto:
                    return "Intelecto (legado)";
                case StatusItem.Espirito:
                    return "Espírito (legado)";
                case StatusItem.Vigor:
                    return "Vigor (legado)";
                case StatusItem.Agilidade:
                    return "Agilidade (legado)";
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
                case StatusItem.DanoFisico:
                    return sinal + Numero(convertido) + " de dano físico";
                case StatusItem.DanoMagico:
                    return sinal + Numero(convertido) + " de dano mágico";
                case StatusItem.VidaMaxima:
                    return sinal + Numero(convertido) + " de vida máxima";
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

        private static string Numero(double valor) =>
            valor.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',');
    }
}
