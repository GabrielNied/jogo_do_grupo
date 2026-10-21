using System;
using JogoDoGrupo.Shared.Features.Personagem;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>Renderiza o prefab real da raça em uma textura isolada das câmeras do mundo.</summary>
    internal sealed class PreviaVisualPersonagem : IDisposable
    {
        private const int CamadaDaPrevia = 30;

        // Grande para a seleção e a criação em tela cheia (o personagem no centro, como no WoW).
        private const int Largura = 720;
        private const int Altura = 1080;

        private GameObject _raiz;
        private GameObject _modelo;
        private Camera _camera;
        private RenderTexture _textura;
        private Raca? _raca;
        private AparenciaPersonagem _aparencia;

        public RenderTexture Textura => _textura;

        public bool Mostrar(Raca raca, AparenciaPersonagem aparencia = null)
        {
            var precisaRecriar = _raca != raca || _modelo == null;
            if (precisaRecriar)
            {
                LimparModelo();
                var prefab = Resources.Load<GameObject>(
                    RegistroVisualJogador.CaminhoDoPrefab(raca)
                );
                if (prefab == null)
                {
                    Debug.LogWarning($"[Personagem] Prévia da raça {raca} não encontrada.");
                    return false;
                }

                CriarCenaSeNecessario();
                _modelo = UnityEngine.Object.Instantiate(prefab, _raiz.transform);
                _modelo.name = "Modelo da prévia";
                _modelo.transform.localPosition = Vector3.zero;
                _modelo.transform.localRotation = Quaternion.identity;
                DefinirCamada(_modelo.transform);

                var renderizadores = _modelo.GetComponentsInChildren<Renderer>();
                if (renderizadores.Length == 0)
                {
                    LimparModelo();
                    return false;
                }

                var limites = renderizadores[0].bounds;
                for (var i = 1; i < renderizadores.Length; i++)
                {
                    limites.Encapsulate(renderizadores[i].bounds);
                }

                // Perspectiva fechada (teleobjetiva): enquadra de corpo inteiro, de leve de baixo.
                var altura =
                    Mathf.Max(limites.size.y, limites.size.x * Altura / Largura, 1.6f) * 1.18f;
                var distancia =
                    altura * 0.5f / Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                _camera.transform.position = limites.center + new Vector3(0.35f, -0.05f, distancia);
                _camera.transform.LookAt(limites.center + Vector3.up * 0.02f);
                _raca = raca;
            }

            _aparencia = aparencia;
            AplicadorAparencia.Aplicar(_modelo, aparencia);
            _camera.Render();
            return true;
        }

        private void CriarCenaSeNecessario()
        {
            if (_raiz != null)
            {
                return;
            }

            // Objeto da cena (só escondido): some junto com ela. Com DontSave sobrevivia ao fim do
            // Play no editor e o modelo esquecido aparecia misturado na prévia seguinte.
            _raiz = new GameObject("Prévia de personagem")
            {
                hideFlags = HideFlags.HideInHierarchy,
            };
            _raiz.transform.position = new Vector3(10000f, 10000f, 10000f);

            if (_textura != null)
            {
                _textura.Release();
                Destruir(_textura);
            }

            _textura = new RenderTexture(Largura, Altura, 24, RenderTextureFormat.ARGB32)
            {
                name = "Prévia de personagem",
                filterMode = FilterMode.Bilinear,
                antiAliasing = 4,
                hideFlags = HideFlags.HideAndDontSave,
            };
            _textura.Create();

            var objetoCamera = new GameObject("Câmera da prévia");
            objetoCamera.transform.SetParent(_raiz.transform, false);
            _camera = objetoCamera.AddComponent<Camera>();
            _camera.enabled = true;
            _camera.orthographic = false;
            _camera.fieldOfView = 20f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.cullingMask = 1 << CamadaDaPrevia;
            _camera.targetTexture = _textura;

            // Luz de estúdio: principal quente, contraluz fria para recortar a silhueta do fundo
            // escuro e um preenchimento fraco.
            CriarLuz(
                "Luz da prévia",
                new Vector3(30f, -40f, 0f),
                new Color(1f, 0.92f, 0.8f),
                1.35f
            );
            CriarLuz(
                "Contraluz da prévia",
                new Vector3(15f, 160f, 0f),
                new Color(0.62f, 0.75f, 1f),
                1.1f
            );
            CriarLuz(
                "Preenchimento da prévia",
                new Vector3(10f, 60f, 0f),
                new Color(1f, 0.85f, 0.7f),
                0.35f
            );
        }

        private void CriarLuz(string nome, Vector3 angulos, Color cor, float intensidade)
        {
            var objetoLuz = new GameObject(nome);
            objetoLuz.transform.SetParent(_raiz.transform, false);
            objetoLuz.transform.localRotation = Quaternion.Euler(angulos);
            var luz = objetoLuz.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.color = cor;
            luz.intensity = intensidade;
            luz.cullingMask = 1 << CamadaDaPrevia;
        }

        /// <summary>
        /// Monta o palco da prévia: o pedestal (sombra e anel sob os pés) atrás da imagem do
        /// personagem, para ele não flutuar sobre o fundo.
        /// </summary>
        public static UnityEngine.UIElements.VisualElement CriarPalco(
            UnityEngine.UIElements.Image imagem
        )
        {
            var palco = new UnityEngine.UIElements.VisualElement();
            palco.AddToClassList("personagem-palco");
            var pedestal = new UnityEngine.UIElements.VisualElement
            {
                pickingMode = UnityEngine.UIElements.PickingMode.Ignore,
            };
            pedestal.AddToClassList("personagem-pedestal");
            palco.Add(pedestal);
            palco.Add(imagem);
            return palco;
        }

        /// <summary>
        /// Liga o arrastar do mouse sobre <paramref name="alvo"/> ao giro do personagem, como na
        /// seleção do WoW. Vale para a prévia que estiver no campo quando o arrasto acontecer.
        /// </summary>
        public static void LigarArrasto(
            UnityEngine.UIElements.VisualElement alvo,
            System.Func<PreviaVisualPersonagem> previa
        )
        {
            var arrastando = false;
            alvo.pickingMode = UnityEngine.UIElements.PickingMode.Position;
            alvo.RegisterCallback<UnityEngine.UIElements.PointerDownEvent>(e =>
            {
                arrastando = true;
                UnityEngine.UIElements.PointerCaptureHelper.CapturePointer(alvo, e.pointerId);
            });
            alvo.RegisterCallback<UnityEngine.UIElements.PointerMoveEvent>(e =>
            {
                if (arrastando)
                    previa()?.Girar(-e.deltaPosition.x * GrausPorPixel);
            });
            alvo.RegisterCallback<UnityEngine.UIElements.PointerUpEvent>(e =>
            {
                arrastando = false;
                UnityEngine.UIElements.PointerCaptureHelper.ReleasePointer(alvo, e.pointerId);
            });
        }

        private const float GrausPorPixel = 0.6f;

        /// <summary>Gira o personagem da prévia (arrastar com o mouse na seleção e na criação).</summary>
        public void Girar(float graus)
        {
            if (_modelo != null)
            {
                _modelo.transform.Rotate(0f, graus, 0f, Space.World);
            }
        }

        private static void DefinirCamada(Transform objeto)
        {
            objeto.gameObject.layer = CamadaDaPrevia;
            foreach (Transform filho in objeto)
            {
                DefinirCamada(filho);
            }
        }

        private void LimparModelo()
        {
            _raca = null;
            if (_modelo != null)
            {
                Destruir(_modelo);
                _modelo = null;
            }
        }

        public void Dispose()
        {
            LimparModelo();
            if (_camera != null)
            {
                _camera.targetTexture = null;
            }

            if (_textura != null)
            {
                _textura.Release();
                Destruir(_textura);
                _textura = null;
            }

            if (_raiz != null)
            {
                Destruir(_raiz);
                _raiz = null;
                _camera = null;
            }
        }

        private static void Destruir(UnityEngine.Object objeto)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(objeto);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(objeto);
            }
        }
    }
}
