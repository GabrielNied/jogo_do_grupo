using System;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Regras puras do loot no chão (fatia jogável, seção 5; ficha da issue #64). Os valores são
    /// proposta a calibrar. Quem sorteia, define o dono e valida a coleta é o servidor; o client
    /// usa isto só para mostrar (o que dá para coletar, quanto tempo falta).
    /// </summary>
    public static class RegrasLoot
    {
        // Sorteio por participante, a cada inimigo comum. As três chances somam 1.
        // Aumentar ChanceDeUmItem ou ChanceDeDoisItens enche o inventário mais depressa.

        /// <summary>Chance de o participante não receber item nenhum.</summary>
        public const double ChanceDeNenhumItem = 0.5;

        /// <summary>Chance de o participante receber um item.</summary>
        public const double ChanceDeUmItem = 0.4;

        /// <summary>Chance de o participante receber dois itens.</summary>
        public const double ChanceDeDoisItens = 0.1;

        /// <summary>Maior quantidade de itens que um inimigo comum dá a um participante.</summary>
        public const int MaximoPorInimigo = 2;

        /// <summary>Segundos em que o item fica no chão, reservado ao dono, antes de sumir.</summary>
        public const int ReservaSegundos = 180;

        /// <summary>Distância máxima, em metros no plano do chão, para coletar um item.</summary>
        public const double RaioDeColetaMetros = 3.0;

        /// <summary>
        /// Distância, em metros, em que o client mostra o item no chão (os "12 m" da fatia). Não
        /// é regra do servidor.
        /// </summary>
        public const double RaioDeVisaoMetros = 12.0;

        /// <summary>
        /// Valor de <see cref="ColetarLoot.LootId"/> que pede "tudo o que é meu por perto". Nenhum
        /// item no chão tem esse id.
        /// </summary>
        public const long TudoPorPerto = 0;

        /// <summary>
        /// Quantos itens um participante recebe, a partir de um número sorteado em [0, 1).
        /// Valores fora da faixa são tratados como a ponta mais próxima.
        /// </summary>
        public static int QuantidadeSorteada(double sorteio)
        {
            if (!(sorteio >= ChanceDeNenhumItem))
            {
                return 0;
            }

            return sorteio < ChanceDeNenhumItem + ChanceDeUmItem ? 1 : MaximoPorInimigo;
        }

        /// <summary>
        /// Diz se o personagem alcança o item: distância no plano do chão (a altura não conta) de
        /// até <see cref="RaioDeColetaMetros"/>, inclusive.
        /// </summary>
        public static bool DentroDoRaioDeColeta(
            double personagemX,
            double personagemZ,
            double lootX,
            double lootZ
        )
        {
            var dx = personagemX - lootX;
            var dz = personagemZ - lootZ;
            return dx * dx + dz * dz <= RaioDeColetaMetros * RaioDeColetaMetros;
        }

        /// <summary>
        /// Segundos que faltam para o item expirar, arredondados para cima e nunca negativos,
        /// para <see cref="LootNoChao.ExpiraEmSegundos"/>.
        /// </summary>
        public static int SegundosRestantes(double segundosDesdeQueApareceu)
        {
            var restante = ReservaSegundos - segundosDesdeQueApareceu;
            if (!(restante > 0))
            {
                return 0;
            }

            return restante >= ReservaSegundos ? ReservaSegundos : (int)Math.Ceiling(restante);
        }

        /// <summary>Diz se o item já passou do prazo de <see cref="ReservaSegundos"/>.</summary>
        public static bool Expirou(double segundosDesdeQueApareceu) =>
            segundosDesdeQueApareceu >= ReservaSegundos;
    }
}
