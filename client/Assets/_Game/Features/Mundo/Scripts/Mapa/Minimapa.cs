using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Visual;
using JogoDoGrupo.Client.Features.Mundo.Presenca;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Mundo
{
    /// <summary>
    /// Minimapa no canto superior direito: câmera de cima num disco de 40 m, norte fixo,
    /// seta do jogador e pontos de quem a presença já informou. Some com a cena do mundo.
    /// </summary>
    public sealed class Minimapa : MonoBehaviour
    {
        private const int Textura = 256;

        private Camera _camera;
        private RenderTexture _textura;
        private VisualElement _raiz;
        private VisualElement _vista;
        private VisualElement _marcadores;
        private readonly List<MarcadorDoMapa> _outros = new List<MarcadorDoMapa>();
        private float _rotacao;
        private bool _sobOChao;

        private void Start()
        {
            _textura = new RenderTexture(Textura, Textura, 16) { name = "Minimapa" };
            _textura.Create();

            var suporte = new GameObject("CameraDoMinimapa");
            suporte.transform.SetParent(transform, false);
            _camera = suporte.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.orthographic = true;
            _camera.orthographicSize = ProjecaoDoMapa.RaioDoMinimapa;
            _camera.targetTexture = _textura;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = EstiloVisual.Cor(ConfigVisual.Cores.Trilho);
            _camera.nearClipPlane = 0.3f;
            _camera.farClipPlane = 400f;
            _camera.depth = -50f;
            _camera.allowMSAA = false;
            _camera.enabled = true;

            var regiao = Hud.Regiao(RegiaoDoHud.SuperiorDireita);
            if (regiao == null)
            {
                return;
            }

            _raiz = new VisualElement { name = "mundo.minimapa" };
            _raiz.pickingMode = PickingMode.Ignore;
            _raiz.AddToClassList("mundo-minimapa");

            _vista = new VisualElement();
            _vista.pickingMode = PickingMode.Ignore;
            _vista.style.position = Position.Absolute;
            _vista.style.left = 0;
            _vista.style.right = 0;
            _vista.style.top = 0;
            _vista.style.bottom = 0;
            _vista.style.backgroundImage = Background.FromRenderTexture(_textura);
            _raiz.Add(_vista);

            _marcadores = new VisualElement();
            _marcadores.pickingMode = PickingMode.Ignore;
            _marcadores.style.position = Position.Absolute;
            _marcadores.style.left = 0;
            _marcadores.style.right = 0;
            _marcadores.style.top = 0;
            _marcadores.style.bottom = 0;
            _marcadores.generateVisualContent += DesenharMarcadores;
            _raiz.Add(_marcadores);

            var norte = new Label("N");
            norte.pickingMode = PickingMode.Ignore;
            norte.AddToClassList("mundo-minimapa-norte");
            _raiz.Add(norte);

            regiao.Add(_raiz);
        }

        private void LateUpdate()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (jogador == null || _camera == null)
            {
                return;
            }

            var posicao = jogador.transform.position;
            _rotacao = jogador.transform.eulerAngles.y;
            _sobOChao = ProjecaoDoMapa.SobOChao(posicao.x, posicao.y, posicao.z);
            _camera.enabled = !_sobOChao;
            if (_vista != null)
            {
                _vista.style.display = _sobOChao ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (!_sobOChao)
            {
                _camera.transform.SetPositionAndRotation(
                    new Vector3(posicao.x, posicao.y + 160f, posicao.z),
                    Quaternion.Euler(90f, 0f, 0f)
                );
            }

            PresencaNoMundo.Atual?.CopiarOutros(_outros);
            _marcadores?.MarkDirtyRepaint();
        }

        private void OnDestroy()
        {
            if (_camera != null)
            {
                _camera.targetTexture = null;
            }

            if (_textura != null)
            {
                _textura.Release();
                Destroy(_textura);
            }

            _raiz?.RemoveFromHierarchy();
        }

        private void DesenharMarcadores(MeshGenerationContext contexto)
        {
            var ret = _marcadores.contentRect;
            if (ret.width < 1f)
            {
                return;
            }

            var centro = new Vector2(ret.width * 0.5f, ret.height * 0.5f);
            var escala = ret.width * 0.5f;
            var pincel = contexto.painter2D;
            var jogador = EntradaNoJogo.Jogador;
            if (jogador != null)
            {
                var origem = jogador.transform.position;
                foreach (var outro in _outros)
                {
                    if (!ProjecaoDoMapa.NaAreaAtual(outro.Area, ProjecaoDoMapa.AreaDaCidade))
                    {
                        continue;
                    }

                    if (
                        !ProjecaoDoMapa.NoMinimapa(
                            outro.X - origem.x,
                            outro.Z - origem.z,
                            out var x,
                            out var y
                        )
                    )
                    {
                        continue;
                    }

                    Losango(
                        pincel,
                        new Vector2(centro.x + x * escala, centro.y - y * escala),
                        ConfigVisual.Dimensoes.MarcadorAliado,
                        EstiloVisual.Cor(ConfigVisual.Cores.Marfim)
                    );
                }
            }

            Seta(
                pincel,
                centro,
                ConfigVisual.Dimensoes.MarcadorMinimapa,
                ProjecaoDoMapa.AnguloDaSeta(_rotacao),
                EstiloVisual.Cor(ConfigVisual.Cores.Essencia)
            );
        }

        private static void Losango(Painter2D pincel, Vector2 centro, float raio, Color cor)
        {
            pincel.fillColor = cor;
            pincel.BeginPath();
            pincel.MoveTo(new Vector2(centro.x, centro.y - raio));
            pincel.LineTo(new Vector2(centro.x + raio, centro.y));
            pincel.LineTo(new Vector2(centro.x, centro.y + raio));
            pincel.LineTo(new Vector2(centro.x - raio, centro.y));
            pincel.ClosePath();
            pincel.Fill();
        }

        private static void Seta(
            Painter2D pincel,
            Vector2 centro,
            float raio,
            float graus,
            Color cor
        )
        {
            var ponta = Girar(new Vector2(0f, -raio), graus);
            var esquerda = Girar(new Vector2(-raio * 0.55f, raio * 0.7f), graus);
            var direita = Girar(new Vector2(raio * 0.55f, raio * 0.7f), graus);
            pincel.fillColor = cor;
            pincel.BeginPath();
            pincel.MoveTo(centro + ponta);
            pincel.LineTo(centro + esquerda);
            pincel.LineTo(centro + direita);
            pincel.ClosePath();
            pincel.Fill();
        }

        private static Vector2 Girar(Vector2 ponto, float graus)
        {
            var radianos = graus * Mathf.Deg2Rad;
            var c = Mathf.Cos(radianos);
            var s = Mathf.Sin(radianos);
            return new Vector2(c * ponto.x - s * ponto.y, s * ponto.x + c * ponto.y);
        }
    }
}
