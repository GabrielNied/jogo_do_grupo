using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Versão 3 dos atributos (#709): Força vira dano físico, Intelecto vira dano mágico, Vigor vira
    /// vida máxima; Agilidade vira regeneração de stamina; Espírito vira regeneração de mana. Os
    /// cinco nomes do personagem deixam de ser stats de item. Só roda em exemplar com versão 2.
    /// </summary>
    public static class MigracaoDeAtributosV3
    {
        public static StatusItem Novo(StatusItem v2)
        {
            switch (v2)
            {
                case StatusItem.Forca:
                    return StatusItem.DanoFisico;
                case StatusItem.Intelecto:
                    return StatusItem.DanoMagico;
                case StatusItem.Vigor:
                    return StatusItem.VidaMaxima;
                case StatusItem.Agilidade:
                    return StatusItem.RegeneracaoStamina;
                case StatusItem.Espirito:
                    return StatusItem.RegeneracaoMana;
                default:
                    return v2;
            }
        }

        /// <summary>Aplica o mapa ao exemplar no lugar, somando duplicatas e limitando à faixa.</summary>
        public static void Aplicar(Equipamento item)
        {
            var atributos = item.Atributos ?? Array.Empty<AtributoItem>();
            var ordem = new List<StatusItem>();
            var soma = new Dictionary<StatusItem, double>();
            var fixo = new Dictionary<StatusItem, bool>();
            foreach (var atributo in atributos)
            {
                var alvo = Novo(atributo.Status);
                if (!soma.ContainsKey(alvo))
                {
                    ordem.Add(alvo);
                    soma[alvo] = 0;
                    fixo[alvo] = false;
                }

                soma[alvo] += atributo.Valor;
                fixo[alvo] |= atributo.Fixo;
            }

            var resultado = new AtributoItem[ordem.Count];
            for (var i = 0; i < ordem.Count; i++)
            {
                var alvo = ordem[i];
                var faixa = ConfigTier.Faixa(item.Tipo, alvo);
                var valor =
                    faixa != null
                        ? faixa.Value.Limitar(soma[alvo])
                        : Math.Min(
                            Math.Max(
                                Math.Round(soma[alvo], MidpointRounding.AwayFromZero),
                                ConfigTier.EscalaMinima
                            ),
                            ConfigTier.EscalaMaxima
                        );
                resultado[i] = new AtributoItem
                {
                    Status = alvo,
                    Valor = valor,
                    Fixo = fixo[alvo],
                };
            }

            item.Atributos = resultado;
        }
    }
}
