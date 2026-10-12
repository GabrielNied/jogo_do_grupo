using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// A moldura vermelha que acende nas bordas da tela quando o jogador leva um golpe. Só
    /// desenha a intensidade que recebe; cor e espessura estão em <c>EstiloCombate.uss</c>.
    /// </summary>
    internal sealed class PainelImpacto
    {
        private readonly VisualElement _vinheta;

        public PainelImpacto(VisualElement raiz)
        {
            _vinheta = new VisualElement { name = "combate.vinheta" };
            _vinheta.pickingMode = PickingMode.Ignore;
            _vinheta.AddToClassList("vinheta-de-dano");
            _vinheta.style.opacity = 0f;
            raiz.Add(_vinheta);
        }

        /// <summary>Intensidade de 0 (apagada) a 1.</summary>
        public void Mostrar(float intensidade)
        {
            _vinheta.style.opacity = Mathf.Clamp01(intensidade);
        }
    }
}
