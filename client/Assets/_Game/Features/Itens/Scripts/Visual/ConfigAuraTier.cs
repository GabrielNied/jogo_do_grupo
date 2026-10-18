using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens.Visual
{
    /// <summary>
    /// Configurações visuais de aura de tier no mundo 3D (issue #926, docs/features/itens.md#visual-de-tier).
    /// Tier B+ (B, A, S, S+) emite brilho e aura visível à distância no chão e nas armas visíveis no modelo 3D.
    /// D e C não possuem aura.
    /// </summary>
    public static class ConfigAuraTier
    {
        // Paleta de cores para aura no mundo (tabela em docs/features/itens.md#visual-de-tier):
        // B = azul, A = roxo, S = laranja, S+ = vermelho escuro
        public static readonly Color CorTierB = new Color(0.18f, 0.48f, 0.95f);
        public static readonly Color CorTierA = new Color(0.65f, 0.22f, 0.95f);
        public static readonly Color CorTierS = new Color(0.98f, 0.52f, 0.08f);
        public static readonly Color CorTierSMais = new Color(0.85f, 0.08f, 0.15f);

        /// <summary>Determina se o tier possui aura 3D no mundo (apenas B ou superior).</summary>
        public static bool TemAura(TierItem tier) => tier >= TierItem.B;

        /// <summary>Devolve a cor da aura correspondente ao tier; D e C devolvem Color.clear.</summary>
        public static Color Cor(TierItem tier)
        {
            switch (tier)
            {
                case TierItem.B:
                    return CorTierB;
                case TierItem.A:
                    return CorTierA;
                case TierItem.S:
                    return CorTierS;
                case TierItem.SMais:
                    return CorTierSMais;
                default:
                    return Color.clear;
            }
        }

        /// <summary>Intensidade da luz de aura no chão por tier.</summary>
        public static float IntensidadeChao(TierItem tier)
        {
            switch (tier)
            {
                case TierItem.B:
                    return 1.5f;
                case TierItem.A:
                    return 2.2f;
                case TierItem.S:
                    return 3.2f;
                case TierItem.SMais:
                    return 4.5f;
                default:
                    return 0f;
            }
        }

        /// <summary>Alcance/raio em metros da luz de aura no chão por tier.</summary>
        public static float AlcanceChao(TierItem tier)
        {
            switch (tier)
            {
                case TierItem.B:
                    return 1.8f;
                case TierItem.A:
                    return 2.4f;
                case TierItem.S:
                    return 3.0f;
                case TierItem.SMais:
                    return 3.8f;
                default:
                    return 0f;
            }
        }

        /// <summary>Intensidade da luz de aura na arma equipada por tier.</summary>
        public static float IntensidadeArma(TierItem tier)
        {
            switch (tier)
            {
                case TierItem.B:
                    return 0.8f;
                case TierItem.A:
                    return 1.3f;
                case TierItem.S:
                    return 1.9f;
                case TierItem.SMais:
                    return 2.8f;
                default:
                    return 0f;
            }
        }

        /// <summary>Alcance/raio em metros da luz de aura na arma equipada por tier.</summary>
        public static float AlcanceArma(TierItem tier)
        {
            switch (tier)
            {
                case TierItem.B:
                    return 0.9f;
                case TierItem.A:
                    return 1.2f;
                case TierItem.S:
                    return 1.5f;
                case TierItem.SMais:
                    return 1.9f;
                default:
                    return 0f;
            }
        }
    }
}
