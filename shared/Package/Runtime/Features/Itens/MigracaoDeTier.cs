using System;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Migra o exemplar até a versão atual: da escala antiga do catálogo (valores fracionários com
    /// fator de grau) para a escala inteira de 0 a 100 (issue #615) e dos atributos do modelo
    /// anterior para os atributos próprios (#623, <see cref="MigracaoDeAtributos"/>). A regra da
    /// escala: <c>novo = round(legado x 100 /
    /// referência)</c>, limitado a 0 a 100; <see cref="ConfigTier.ReferenciaLegada"/> é a referência do
    /// atributo antigo. O <see cref="Equipamento.TierOrigem"/> do exemplar da escala antiga é o
    /// tier calculado com os atributos novos; o de quem já tinha um nunca muda (TIER-004). A conversão é determinística e só roda uma vez por
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
            if (item == null)
            {
                return false;
            }

            var mudou = false;
            var eraLegado = item.VersaoDosAtributos < VersaoDaEscala;
            if (eraLegado)
            {
                MigrarEscala(item);
                mudou = true;
            }

            mudou |= MigrarInfusaoParaFusao(item);
            if (item.VersaoDosAtributos < 2)
            {
                MigracaoDeAtributos.Aplicar(item);
                item.VersaoDosAtributos = 2;
                mudou = true;
            }

            if (item.VersaoDosAtributos < RegrasTier.VersaoAtual)
            {
                MigracaoDeAtributosV3.Aplicar(item);
                item.VersaoDosAtributos = RegrasTier.VersaoAtual;
                mudou = true;
            }

            // Tier de origem só depois da escala e dos mapas de atributo (v2 e v3), quando as faixas
            // batem com os stats do exemplar (TIER-004).
            if (eraLegado)
            {
                item.TierOrigem = RegrasTier.TierDe(item);
            }

            return mudou;
        }

        /// <summary>Versão do exemplar na escala de 0 a 100 dos atributos do modelo anterior (#615).</summary>
        public const int VersaoDaEscala = 1;

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

        // Escala antiga do catálogo -> inteiro de 0 a 100, atributo por atributo, pela referência do
        // atributo antigo. O tier de origem só sai depois, com os atributos novos.
        private static void MigrarEscala(Equipamento item)
        {
            var atributos = item.Atributos ?? Array.Empty<AtributoItem>();
            for (var i = 0; i < atributos.Length; i++)
            {
                atributos[i].Valor = ParaEscalaInteira(atributos[i].Status, atributos[i].Valor);
            }

            item.Atributos = atributos;
            item.VersaoDosAtributos = VersaoDaEscala;
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

        // Mesma conta sem arredondar nem limitar; a divisão vem por último para ser exata.
        private static double Converter(StatusItem status, double valorLegado) =>
            valorLegado * ConfigTier.EscalaMaxima / ConfigTier.ReferenciaLegada(status);
    }
}
