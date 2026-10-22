using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Pvp
{
    /// <summary>
    /// Classes do USS próprio de PvP (<c>UI/Resources/Pvp/EstiloPvp.uss</c>). O resto do visual
    /// usa as classes do <c>EstiloCore</c>.
    /// </summary>
    internal static class EstiloPvp
    {
        /// <summary>Caminho em <c>Resources</c> do USS de PvP.</summary>
        public const string Caminho = "Pvp/EstiloPvp";

        /// <summary>Convite de duelo por cima do jogo.</summary>
        public const string ConviteNaTela = "pvp-convite-tela";

        /// <summary>Bloco da janela (bandeira, arena, duelo) com espaço embaixo.</summary>
        public const string Secao = "pvp-secao";

        /// <summary>Linha do ranking: posição e nome à esquerda, pontos à direita.</summary>
        public const string LinhaRanking = "pvp-linha-ranking";

        public static void Aplicar(VisualElement raiz)
        {
            var estilo = Resources.Load<StyleSheet>(Caminho);
            if (estilo != null)
            {
                raiz.styleSheets.Add(estilo);
            }
            else
            {
                Debug.LogWarning(
                    $"[PvP] Estilo nao encontrado em Resources/{Caminho}.uss; usando só o do Core."
                );
            }
        }

        public static void Mostrar(VisualElement elemento, bool visivel)
        {
            elemento.style.display = visivel ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
