using System;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Converte um exemplar da escala antiga do catálogo (valores fracionários com fator de grau)
    /// para a escala inteira de 0 a 100 (issue #615). A regra: <c>novo = round(legado x 100 /
    /// referência)</c>, limitado à faixa do atributo na categoria; <see cref="ConfigTier.ReferenciaLegada"/>
    /// é a referência do atributo. O <see cref="Equipamento.TierOrigem"/> do exemplar migrado é o
    /// tier calculado na hora da conversão. A conversão é determinística e só roda uma vez por
    /// exemplar (<see cref="Equipamento.VersaoDosAtributos"/>).
    /// </summary>
    public static class MigracaoDeTier
    {
        /// <summary>
        /// Converte o exemplar no lugar se ele ainda está na escala antiga ou tem infusões do
        /// modelo anterior. Devolve <c>true</c> se mudou alguma coisa (quem lê do banco grava de
        /// volta).
        /// </summary>
        public static bool Migrar(Equipamento item)
        {
            var escala = MigrarEscala(item);
            var fusao = MigrarInfusaoParaFusao(item);
            return escala || fusao;
        }

        /// <summary>
        /// A infusão vira fusão (issue #616): <c>InfusoesUsadas</c> passa a ser o nível de fusão,
        /// no máximo +10, e os valores fracionários que a infusão deixou são arredondados para o
        /// inteiro mais próximo (limitados à faixa). Não mexe no tier de origem. Só age enquanto
        /// <see cref="Equipamento.InfusoesUsadas"/> for maior que zero, então rodar de novo não
        /// muda nada.
        /// </summary>
        private static bool MigrarInfusaoParaFusao(Equipamento item)
        {
            if (item == null || item.InfusoesUsadas <= 0)
            {
                return false;
            }

            item.NivelFusao = Math.Max(
                item.NivelFusao,
                Math.Min(item.InfusoesUsadas, ConfigFusao.NivelMaximo)
            );
            item.InfusoesUsadas = 0;
            var atributos = item.Atributos ?? Array.Empty<AtributoItem>();
            for (var i = 0; i < atributos.Length; i++)
            {
                var atributo = atributos[i];
                var faixa = ConfigTier.Faixa(item.Tipo, atributo.Status);
                atributo.Valor =
                    faixa != null
                        ? faixa.Value.Limitar(atributo.Valor)
                        : Math.Round(atributo.Valor, MidpointRounding.AwayFromZero);
            }

            return true;
        }

        private static bool MigrarEscala(Equipamento item)
        {
            if (item == null || item.VersaoDosAtributos >= RegrasTier.VersaoAtual)
            {
                return false;
            }

            var atributos = item.Atributos ?? Array.Empty<AtributoItem>();
            for (var i = 0; i < atributos.Length; i++)
            {
                var atributo = atributos[i];
                var faixa = ConfigTier.Faixa(item.Tipo, atributo.Status);
                var inteiro = ParaEscalaInteira(atributo.Status, atributo.Valor);
                atributo.Valor = faixa != null ? faixa.Value.Limitar(inteiro) : inteiro;
            }

            item.Atributos = atributos;
            item.VersaoDosAtributos = RegrasTier.VersaoAtual;
            item.TierOrigem = RegrasTier.TierDe(item);
            return true;
        }

        /// <summary>
        /// Valor inteiro (0 a 100) equivalente a um valor da escala antiga do atributo.
        /// </summary>
        public static int ParaEscalaInteira(StatusItem status, double valorLegado)
        {
            var limitado = Math.Min(
                Math.Max(Converter(status, valorLegado), ConfigTier.EscalaMinima),
                ConfigTier.EscalaMaxima
            );
            return (int)Math.Round(limitado, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// O inverso de <see cref="ParaEscalaInteira"/>: o valor da escala antiga que corresponde a
        /// um valor de 0 a 100. <b>Provisório:</b> até a #623, o bônus das peças ainda soma nos
        /// atributos do personagem (DEC-001), e a conversão de volta mantém o combate na mesma
        /// ordem de grandeza de antes. Sai quando cada atributo tiver a conversão própria
        /// (ATRIB-003).
        /// </summary>
        public static double ParaEscalaLegada(StatusItem status, double valor) =>
            Math.Round(valor * ConfigTier.ReferenciaLegada(status) / ConfigTier.EscalaMaxima, 6);

        // Mesma conta sem arredondar nem limitar; a divisão vem por último para ser exata.
        private static double Converter(StatusItem status, double valorLegado) =>
            valorLegado * ConfigTier.EscalaMaxima / ConfigTier.ReferenciaLegada(status);
    }
}
