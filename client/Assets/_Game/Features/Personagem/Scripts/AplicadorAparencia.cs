using JogoDoGrupo.Shared.Features.Personagem;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Aplica as escolhas de aparência básica (tom de pele, cabelo e cor de cabelo)
    /// aos renderizadores do modelo 3D.
    /// </summary>
    internal static class AplicadorAparencia
    {
        private static readonly int IdCorBase = Shader.PropertyToID("_BaseColor");

        /// <summary>Cores multiplicadoras para cada tom de pele básico.</summary>
        public static readonly Color[] TonsDePele = new Color[]
        {
            Color.white, // 0: Padrão da raça
            new Color(1.04f, 0.97f, 0.93f), // 1: Claro
            new Color(0.88f, 0.76f, 0.62f), // 2: Bronze
            new Color(0.56f, 0.42f, 0.32f), // 3: Escuro
        };

        /// <summary>
        /// Aplica o tom de pele através de MaterialPropertyBlock para não alterar os assets compartilhados.
        /// </summary>
        public static void Aplicar(GameObject modelo, AparenciaPersonagem aparencia)
        {
            if (modelo == null)
            {
                return;
            }

            var indiceTom = aparencia?.TomDePele ?? 0;
            if (indiceTom < 0 || indiceTom >= TonsDePele.Length)
            {
                indiceTom = 0;
            }

            var cor = TonsDePele[indiceTom];
            var renderizadores = modelo.GetComponentsInChildren<Renderer>(true);
            if (renderizadores == null || renderizadores.Length == 0)
            {
                return;
            }

            var bloco = new MaterialPropertyBlock();
            for (var i = 0; i < renderizadores.Length; i++)
            {
                var rend = renderizadores[i];
                rend.GetPropertyBlock(bloco);
                bloco.SetColor(IdCorBase, cor);
                rend.SetPropertyBlock(bloco);
            }
        }
    }
}
