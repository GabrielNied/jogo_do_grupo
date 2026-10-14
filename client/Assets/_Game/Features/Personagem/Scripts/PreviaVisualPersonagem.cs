using System;
using JogoDoGrupo.Shared.Features.Personagem;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>Renderiza o prefab real da raça em uma textura isolada das câmeras do mundo.</summary>
    internal sealed class PreviaVisualPersonagem : IDisposable
    {
        private const int CamadaDaPrevia = 30;
        private const int Largura = 384;
        private const int Altura = 512;

        private GameObject _raiz;
        private GameObject _modelo;
        private Camera _camera;
        private RenderTexture _textura;
        private Raca? _raca;

        public RenderTexture Textura => _textura;

        public bool Mostrar(Raca raca)
        {
            if (_raca == raca && _modelo != null)
            {
                return true;
            }

            LimparModelo();
            var prefab = Resources.Load<GameObject>(RegistroVisualJogador.CaminhoDoPrefab(raca));
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

            _camera.orthographicSize = Mathf.Max(
                limites.extents.y * 1.25f,
                limites.extents.x * Altura / Largura * 1.25f,
                0.8f
            );
            _camera.transform.position = limites.center + new Vector3(0.6f, 0.2f, 5f);
            _camera.transform.LookAt(limites.center);
            _camera.Render();
            _raca = raca;
            return true;
        }

        private void CriarCenaSeNecessario()
        {
            if (_raiz != null)
            {
                return;
            }

            _raiz = new GameObject("Prévia de personagem")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _raiz.transform.position = new Vector3(10000f, 10000f, 10000f);

            _textura = new RenderTexture(Largura, Altura, 24, RenderTextureFormat.ARGB32)
            {
                name = "Prévia de personagem",
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            _textura.Create();

            var objetoCamera = new GameObject("Câmera da prévia");
            objetoCamera.transform.SetParent(_raiz.transform, false);
            _camera = objetoCamera.AddComponent<Camera>();
            _camera.enabled = true;
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.cullingMask = 1 << CamadaDaPrevia;
            _camera.targetTexture = _textura;

            var objetoLuz = new GameObject("Luz da prévia");
            objetoLuz.transform.SetParent(_raiz.transform, false);
            objetoLuz.transform.localRotation = Quaternion.Euler(35f, -35f, 0f);
            var luz = objetoLuz.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.5f;
            luz.cullingMask = 1 << CamadaDaPrevia;
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
