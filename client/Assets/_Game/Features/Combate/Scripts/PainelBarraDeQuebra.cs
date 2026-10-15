using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Barra de quebra (stagger) do chefe no HUD (UI Toolkit).
    /// Oculta quando o valor é 0 e o chefe não está quebrado (#171, combate.md).
    /// </summary>
    internal sealed class PainelBarraDeQuebra
    {
        private const string ClasseVulneravel = "quebra-boss-vulneravel";

        private readonly VisualElement _raiz;
        private readonly Label _rotulo;
        private readonly VisualElement _preenchimento;

        public VisualElement Raiz => _raiz;

        public PainelBarraDeQuebra(VisualElement pai)
        {
            var estilo = Resources.Load<StyleSheet>(PainelRecursos.CaminhoEstilo);
            if (estilo != null && !pai.styleSheets.Contains(estilo))
            {
                pai.styleSheets.Add(estilo);
            }

            _raiz = new VisualElement { name = "combate.quebra-boss" };
            _raiz.pickingMode = PickingMode.Ignore;
            _raiz.AddToClassList("painel-quebra-boss");
            _raiz.style.display = DisplayStyle.None;

            _rotulo = new Label();
            _rotulo.pickingMode = PickingMode.Ignore;
            _rotulo.AddToClassList("quebra-boss-rotulo");

            var trilho = new VisualElement();
            trilho.pickingMode = PickingMode.Ignore;
            trilho.AddToClassList("quebra-boss-trilho");

            _preenchimento = new VisualElement();
            _preenchimento.pickingMode = PickingMode.Ignore;
            _preenchimento.AddToClassList("quebra-boss-preenchimento");

            trilho.Add(_preenchimento);
            _raiz.Add(_rotulo);
            _raiz.Add(trilho);

            pai.Add(_raiz);
        }

        public void Atualizar(ItemBarraDeQuebra quebra)
        {
            if (quebra.Atual <= 0 && !quebra.Quebrado)
            {
                _raiz.style.display = DisplayStyle.None;
                return;
            }

            _raiz.style.display = DisplayStyle.Flex;
            _rotulo.text = quebra.Texto;
            _preenchimento.style.width = Length.Percent(quebra.Fracao * 100f);

            if (quebra.Quebrado)
            {
                _preenchimento.AddToClassList(ClasseVulneravel);
            }
            else
            {
                _preenchimento.RemoveFromClassList(ClasseVulneravel);
            }
        }

        public void Descartar()
        {
            _raiz.RemoveFromHierarchy();
        }
    }
}
