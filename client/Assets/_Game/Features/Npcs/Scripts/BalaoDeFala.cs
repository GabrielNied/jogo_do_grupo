using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Npcs
{
    /// <summary>
    /// Balão de fala curto sobre a cabeça de um NPC ambiente (#1069), em UI Toolkit como as
    /// etiquetas de nome: texto nítido na fonte da interface, menor e mais apagado de longe, e
    /// some sozinho depois da duração pedida. Todos os balões dividem uma camada só.
    /// </summary>
    public sealed class BalaoDeFala : MonoBehaviour
    {
        private const float AlturaPadrao = 2.7f;
        private const float DistanciaMaxima = 30f;
        private const string CaminhoEstilo = "UI/EstiloNpcs";

        private static UIDocument _documento;
        private static VisualElement _camada;

        private Label _rotulo;
        private float _fimEm;
        private bool _visivel;

        public bool Visivel => _visivel;

        public static BalaoDeFala Criar(Transform pai, float altura = AlturaPadrao)
        {
            var objeto = new GameObject("BalaoDeFala");
            objeto.transform.SetParent(pai, false);
            objeto.transform.localPosition = new Vector3(0f, altura, 0f);
            var balao = objeto.AddComponent<BalaoDeFala>();
            balao.Esconder();
            return balao;
        }

        // A camada nasce na cena de quem fala e some com ela.
        private static VisualElement Camada(Scene cena)
        {
            if (_documento != null && _camada != null)
            {
                return _camada;
            }

            var objeto = new GameObject("Npcs.Baloes");
            SceneManager.MoveGameObjectToScene(objeto, cena);
            objeto.SetActive(false);
            _documento = objeto.AddComponent<UIDocument>();
            _documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel("PainelBaloes", -3.4f);
            objeto.SetActive(true);
            var raiz = _documento.rootVisualElement;
            EstiloCore.Aplicar(raiz);
            var estilo = Resources.Load<StyleSheet>(CaminhoEstilo);
            if (estilo != null)
            {
                raiz.styleSheets.Add(estilo);
            }

            _camada = new VisualElement { name = "npcs.baloes", pickingMode = PickingMode.Ignore };
            _camada.AddToClassList("npcs-camada");
            raiz.Add(_camada);
            return _camada;
        }

        /// <summary>Mostra o texto por <paramref name="duracao"/> segundos.</summary>
        public void Mostrar(string fala, float duracao)
        {
            if (string.IsNullOrEmpty(fala))
            {
                return;
            }

            if (_rotulo == null || _rotulo.panel == null)
            {
                _rotulo = new Label { pickingMode = PickingMode.Ignore };
                _rotulo.AddToClassList("npc-balao");
                Camada(gameObject.scene).Add(_rotulo);
            }

            _rotulo.text = fala;
            _rotulo.style.display = DisplayStyle.None;
            _visivel = true;
            _fimEm = Time.time + duracao;
        }

        public void Esconder()
        {
            _visivel = false;
            _fimEm = 0f;
            if (_rotulo != null)
            {
                _rotulo.style.display = DisplayStyle.None;
            }
        }

        private void LateUpdate()
        {
            if (!_visivel || _rotulo == null)
            {
                return;
            }

            if (Time.time >= _fimEm)
            {
                Esconder();
                return;
            }

            var camera = Camera.main;
            var painel = _rotulo.panel;
            var mundo = transform.position;
            var viewport = camera != null ? camera.WorldToViewportPoint(mundo) : Vector3.zero;
            var distancia =
                camera != null ? Vector3.Distance(camera.transform.position, mundo) : 0f;
            if (camera == null || painel == null || viewport.z <= 0f || distancia > DistanciaMaxima)
            {
                _rotulo.style.display = DisplayStyle.None;
                return;
            }

            var ponto = RuntimePanelUtils.CameraTransformWorldToPanel(painel, mundo, camera);
            _rotulo.style.display = DisplayStyle.Flex;
            _rotulo.style.left = ponto.x;
            _rotulo.style.top = ponto.y;
            var proximidade = 1f - Mathf.Clamp01((distancia - 6f) / (DistanciaMaxima - 6f));
            _rotulo.style.opacity = Mathf.Lerp(0.45f, 1f, proximidade);
            var escala = Mathf.Lerp(0.75f, 1f, proximidade);
            _rotulo.style.scale = new Scale(new Vector3(escala, escala, 1f));
        }

        private void OnDisable()
        {
            if (_rotulo != null)
            {
                _rotulo.style.display = DisplayStyle.None;
            }
        }

        private void OnDestroy()
        {
            _rotulo?.RemoveFromHierarchy();
            _rotulo = null;
        }

        /// <summary>Quebra o texto em linhas de até <paramref name="largura"/> caracteres, nas palavras.</summary>
        public static string Quebrar(string texto, int largura)
        {
            var palavras = texto.Split(' ');
            var resultado = new System.Text.StringBuilder();
            var linhaAtual = 0;
            for (var i = 0; i < palavras.Length; i++)
            {
                var palavra = palavras[i];
                if (linhaAtual > 0 && linhaAtual + 1 + palavra.Length > largura)
                {
                    resultado.Append('\n');
                    linhaAtual = 0;
                }
                else if (linhaAtual > 0)
                {
                    resultado.Append(' ');
                    linhaAtual++;
                }

                resultado.Append(palavra);
                linhaAtual += palavra.Length;
            }

            return resultado.ToString();
        }
    }
}
