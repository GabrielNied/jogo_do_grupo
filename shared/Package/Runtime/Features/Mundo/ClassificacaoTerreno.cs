using System;

namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Classificação de superfície para texturas automáticas, shader e colocação de props.
    /// </summary>
    public enum TipoDeSuperficie
    {
        ChaoPadrao = 0,
        Praia = 1,
        MargemDeRio = 2,
        ParedaoRochoso = 3,
    }

    /// <summary>
    /// Regras de classificação geométrica da superfície com base na cota da água e inclinação.
    /// </summary>
    public static class ClassificacaoTerreno
    {
        /// <summary>Inclinação a partir da qual o relevo é considerado paredão de rocha (em graus).</summary>
        public const float InclinacaoMinimaParedaoGraus = 42f;

        /// <summary>Altura máxima acima do nível da água em que a margem é tratada como praia.</summary>
        public const float AlturaMaximaPraia = 1.2f;

        /// <summary>
        /// Determina o tipo de superfície para texturização com base na altura, inclinação e bioma.
        /// </summary>
        public static TipoDeSuperficie Classificar(
            float altura,
            float inclinacaoGraus,
            Bioma bioma,
            bool proximoRio
        )
        {
            // 1. Paredão rochoso agressivo tem prioridade visual em qualquer bioma
            if (inclinacaoGraus >= InclinacaoMinimaParedaoGraus)
            {
                return TipoDeSuperficie.ParedaoRochoso;
            }

            // 2. Margem de rio acentuada (não praia)
            if (proximoRio)
            {
                return TipoDeSuperficie.MargemDeRio;
            }

            // 3. Praia suave perto de águas calmas (pântano / lagos em cota baixa com declive suave)
            if (
                bioma == Bioma.Pantano
                && altura >= MapaDoMundo.NivelDaAgua
                && altura <= (MapaDoMundo.NivelDaAgua + AlturaMaximaPraia)
            )
            {
                return TipoDeSuperficie.Praia;
            }

            return TipoDeSuperficie.ChaoPadrao;
        }
    }
}
