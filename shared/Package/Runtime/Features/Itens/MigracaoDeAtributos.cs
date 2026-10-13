using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Troca os atributos do modelo anterior pelos atributos próprios dos equipamentos (#623,
    /// versão 2 de <see cref="Equipamento.VersaoDosAtributos"/>). Mapa (itens.md, Atributos dos
    /// equipamentos): Destreza vira Agilidade; Vitalidade e Vida viram Vigor; o Vigor antigo (o
    /// recurso) vira Agilidade; Energia vira Espírito; Defesa vira defesa física;
    /// RecuperacaoVigor vira regeneração de stamina; Força, Intelecto e Espírito ficam;
    /// PotenciaFogo, PotenciaGelo e Alcance ficam como legado sem efeito. Dois antigos que caem no
    /// mesmo novo <b>somam</b> (limitado à faixa), e o valor continua na escala de 0 a 100.
    /// Só roda em exemplar de versão menor que 2, então a troca de sentido de
    /// <see cref="StatusItem.Vigor"/> não se confunde. O tier de origem não muda aqui.
    /// </summary>
    public static class MigracaoDeAtributos
    {
        /// <summary>O atributo novo que recebe o valor de um atributo do modelo anterior.</summary>
        public static StatusItem Novo(StatusItem legado)
        {
            switch (legado)
            {
                case StatusItem.Destreza:
                    return StatusItem.Agilidade;
                case StatusItem.Vitalidade:
                case StatusItem.Vida:
                    return StatusItem.Vigor;
                case StatusItem.Vigor:
                    return StatusItem.Agilidade; // O Vigor antigo era o recurso.
                case StatusItem.Energia:
                    return StatusItem.Espirito;
                case StatusItem.Defesa:
                    return StatusItem.DefesaFisica;
                case StatusItem.RecuperacaoVigor:
                    return StatusItem.RegeneracaoStamina;
                default:
                    return legado;
            }
        }

        /// <summary>
        /// Aplica o mapa ao exemplar no lugar, na ordem em que cada atributo novo apareceu
        /// primeiro. Não mexe na versão nem no tier de origem (<see cref="MigracaoDeTier"/> faz).
        /// </summary>
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
                var valor = faixa != null ? faixa.Value.Limitar(soma[alvo]) : Limitar(soma[alvo]);
                resultado[i] = new AtributoItem
                {
                    Status = alvo,
                    Valor = valor,
                    Fixo = fixo[alvo],
                };
            }

            item.Atributos = resultado;
        }

        // Sem faixa na categoria: fica na escala geral de 0 a 100.
        private static double Limitar(double valor) =>
            Math.Min(
                Math.Max(Math.Round(valor, MidpointRounding.AwayFromZero), ConfigTier.EscalaMinima),
                ConfigTier.EscalaMaxima
            );
    }
}
