using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// A etiqueta de vida do boneco de treino, no mesmo desenho das etiquetas dos inimigos (nome e
    /// barra sobre a cabeça, #1188). As classes e o estilo são os do Mobs
    /// (<c>Resources/UI/EstiloMobs</c>), pelo caminho e pelos nomes das classes, sem referência de
    /// código entre as funcionalidades.
    /// </summary>
    internal sealed class EtiquetaDoBoneco
    {
        public const string CaminhoEstilo = "UI/EstiloMobs";
        public const string ClasseEtiqueta = "inimigo-etiqueta";
        public const string ClasseNome = "inimigo-nome";
        public const string ClasseBarra = "inimigo-barra";
        public const string ClassePreenchimento = "inimigo-preenchimento";

        private readonly VisualElement _raiz;
        private readonly VisualElement _preenchimento;

        public EtiquetaDoBoneco(VisualElement camada, string nome)
        {
            _raiz = new VisualElement { name = "combate.etiqueta-boneco" };
            _raiz.pickingMode = PickingMode.Ignore;
            var estilo = Resources.Load<StyleSheet>(CaminhoEstilo);
            if (estilo != null)
            {
                _raiz.styleSheets.Add(estilo);
            }

            _raiz.AddToClassList(ClasseEtiqueta);

            var rotulo = new Label(nome);
            rotulo.pickingMode = PickingMode.Ignore;
            rotulo.AddToClassList(ClasseNome);

            var barra = new VisualElement();
            barra.pickingMode = PickingMode.Ignore;
            barra.AddToClassList(ClasseBarra);

            _preenchimento = new VisualElement();
            _preenchimento.pickingMode = PickingMode.Ignore;
            _preenchimento.AddToClassList(ClassePreenchimento);
            barra.Add(_preenchimento);

            _raiz.Add(rotulo);
            _raiz.Add(barra);
            _raiz.style.display = DisplayStyle.None;
            camada.Add(_raiz);
        }

        /// <summary>A largura do preenchimento da barra, de 0 a 1.</summary>
        public void MostrarVida(float fracao) =>
            _preenchimento.style.width = Length.Percent(Mathf.Clamp01(fracao) * 100f);

        /// <summary>Põe a etiqueta sobre o ponto do mundo; some se ele está atrás da câmera.</summary>
        public void Posicionar(Camera camera, Vector3 mundo)
        {
            var painel = _raiz.panel;
            if (camera == null || painel == null)
            {
                _raiz.style.display = DisplayStyle.None;
                return;
            }

            var atras = camera.WorldToViewportPoint(mundo).z <= 0f;
            _raiz.style.display = atras ? DisplayStyle.None : DisplayStyle.Flex;
            if (atras)
            {
                return;
            }

            var ponto = RuntimePanelUtils.CameraTransformWorldToPanel(painel, mundo, camera);
            _raiz.style.left = ponto.x;
            _raiz.style.top = ponto.y;
        }

        public void Remover() => _raiz.RemoveFromHierarchy();
    }
}
