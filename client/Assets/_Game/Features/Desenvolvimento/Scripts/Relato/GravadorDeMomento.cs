using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Core;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Relato
{
    /// <summary>
    /// Guarda os últimos <see cref="ConfigRelato.SegundosGuardados"/> do jogo para o relato F9:
    /// a tela em miniatura, o estado dos Animators do jogador e do mais próximo, o
    /// <c>timeScale</c>, o solavanco da câmera, as teclas e as mensagens do servidor. Só existe
    /// no editor e no build de desenvolvimento. Roda depois de todos (câmera, recuo e tremor já
    /// aplicados no quadro).
    /// </summary>
    [DefaultExecutionOrder(10000)]
    internal sealed class GravadorDeMomento : MonoBehaviour
    {
        private const string NomeObjeto = "Desenvolvimento.Relato";

        private static GravadorDeMomento _instancia;

        private readonly List<EventoDeRede> _eventos = new List<EventoDeRede>();
        private readonly StringBuilder _entrada = new StringBuilder();

        private RenderTexture[] _anel;
        private RenderTexture[] _zoom;
        private AmostraDeQuadro[] _amostras;
        private int _proximo;
        private int _guardados;
        private RenderTexture _telaCheia;
        private double _ultimaCaptura = double.NegativeInfinity;
        private bool _capturarNoFim;
        private Animator _alvo;
        private float _proximaBusca;
        private Vector3 _cameraAnterior;
        private Vector3 _cameraAntesDaAnterior;
        private int _quadrosDeCamera;
        private IDisposable _assinatura;
        private Dictionary<ushort, string> _nomes;
        private InputAction _f9;
        private InputAction _f10;
        private PainelDoRelato _painel;

        /// <summary>Se está guardando (para enquanto um painel de Desenvolvimento está aberto).</summary>
        public bool Gravando { get; private set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (!ComandosDeTesteNoClient.Permite || _instancia != null)
            {
                return;
            }

            var objeto = new GameObject(NomeObjeto);
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<GravadorDeMomento>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
        }

        private void Awake()
        {
            var capacidade = Mathf.CeilToInt(
                ConfigRelato.SegundosGuardados * ConfigRelato.QuadrosPorSegundo
            );
            _anel = new RenderTexture[capacidade];
            _zoom = new RenderTexture[capacidade];
            _amostras = new AmostraDeQuadro[capacidade];
            _nomes = NomesDasMensagens();
            _f9 = new InputAction("Relato", InputActionType.Button, "<Keyboard>/f9");
            _f10 = new InputAction("Ajustes", InputActionType.Button, "<Keyboard>/f10");
            _f9.Enable();
            _f10.Enable();
            _painel = new PainelDoRelato(this);
            BloqueioDeEntradaPorTexto.RegistrarBloqueioDeAtalhos(() => _painel.Aberto);
        }

        private void OnEnable()
        {
            _assinatura = ConexaoTempoReal.AssinarFaixa(
                new MessageIdRange(0, ushort.MaxValue),
                AoReceber
            );
            StartCoroutine(CapturarNoFimDoQuadro());
        }

        private void OnDisable()
        {
            _assinatura?.Dispose();
            _assinatura = null;
        }

        private void OnDestroy()
        {
            _f9?.Dispose();
            _f10?.Dispose();
            _painel?.Destruir();
            foreach (var textura in _anel)
            {
                Liberar(textura);
            }

            foreach (var textura in _zoom)
            {
                Liberar(textura);
            }

            Liberar(_telaCheia);
        }

        private void Update()
        {
            if (!EntradaNoJogo.NoJogo)
            {
                return;
            }

            var esc = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            _painel.Atualizar(esc);
            if (_f9.WasPressedThisFrame())
            {
                _painel.AlternarRelato();
            }
            else if (_f10.WasPressedThisFrame())
            {
                _painel.AlternarAjustes();
            }
        }

        private void LateUpdate()
        {
            AnotarEntrada();
            if (!Gravando || !EntradaNoJogo.NoJogo)
            {
                _entrada.Clear();
                return;
            }

            var agora = Time.realtimeSinceStartupAsDouble;
            var camera = Camera.main;
            var solavanco = MedirSolavanco(camera);
            if (agora - _ultimaCaptura < 1.0 / ConfigRelato.QuadrosPorSegundo * 0.9)
            {
                return;
            }

            _ultimaCaptura = agora;
            var jogador = EntradaNoJogo.Jogador;
            BuscarAlvo(jogador);
            _amostras[_proximo] = new AmostraDeQuadro
            {
                Frame = Time.frameCount,
                Tempo = agora,
                TimeScale = Time.timeScale,
                Jogador = Ler(
                    jogador != null ? jogador.GetComponentInChildren<Animator>() : null,
                    jogador
                ),
                Alvo = Ler(_alvo, jogador),
                Solavanco = solavanco,
                Entrada = _entrada.ToString(),
            };
            _entrada.Clear();
            _capturarNoFim = true;
        }

        private IEnumerator CapturarNoFimDoQuadro()
        {
            var fimDoQuadro = new WaitForEndOfFrame();
            while (true)
            {
                yield return fimDoQuadro;
                if (!_capturarNoFim)
                {
                    continue;
                }

                _capturarNoFim = false;
                CapturarTela(_proximo);
                _proximo = (_proximo + 1) % _anel.Length;
                _guardados = Math.Min(_guardados + 1, _anel.Length);
            }
        }

        /// <summary>Para de guardar (o painel abriu) ou volta a guardar do zero.</summary>
        public void DefinirGravando(bool gravando)
        {
            if (gravando && !Gravando)
            {
                _guardados = 0;
                _proximo = 0;
                _eventos.Clear();
                _quadrosDeCamera = 0;
            }

            Gravando = gravando;
            _capturarNoFim = false;
        }

        /// <summary>O que está guardado, do mais antigo ao mais novo.</summary>
        public Momento Congelar()
        {
            var quadros = new List<AmostraDeQuadro>(_guardados);
            var texturas = new List<RenderTexture>(_guardados);
            var zooms = new List<RenderTexture>(_guardados);
            var inicio = (_proximo - _guardados + _anel.Length) % _anel.Length;
            for (var i = 0; i < _guardados; i++)
            {
                var indice = (inicio + i) % _anel.Length;
                quadros.Add(_amostras[indice]);
                texturas.Add(_anel[indice]);
                zooms.Add(_zoom[indice]);
            }

            var eventos = new List<EventoDeRede>();
            var desde = quadros.Count > 0 ? quadros[0].Tempo : 0;
            foreach (var evento in _eventos)
            {
                if (evento.Tempo >= desde)
                {
                    eventos.Add(evento);
                }
            }

            return new Momento(quadros, texturas, zooms, eventos);
        }

        private void AoReceber(MensagemTempoReal mensagem)
        {
            if (!Gravando)
            {
                return;
            }

            var agora = Time.realtimeSinceStartupAsDouble;
            _eventos.Add(
                new EventoDeRede(
                    agora,
                    mensagem.Id,
                    _nomes.TryGetValue(mensagem.Id, out var nome) ? nome : "Id" + mensagem.Id,
                    mensagem.Json
                )
            );

            var limite = agora - ConfigRelato.SegundosGuardados - 1.0;
            var velhos = 0;
            while (velhos < _eventos.Count && _eventos[velhos].Tempo < limite)
            {
                velhos++;
            }

            if (velhos > 0)
            {
                _eventos.RemoveRange(0, velhos);
            }
        }

        private void CapturarTela(int indice)
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            if (
                _telaCheia == null
                || _telaCheia.width != Screen.width
                || _telaCheia.height != Screen.height
            )
            {
                Liberar(_telaCheia);
                _telaCheia = new RenderTexture(
                    Screen.width,
                    Screen.height,
                    0,
                    RenderTextureFormat.ARGB32
                );
            }

            _anel[indice] = _anel[indice] ?? NovoQuadro();
            _zoom[indice] = _zoom[indice] ?? NovoQuadro();
            ScreenCapture.CaptureScreenshotIntoRenderTexture(_telaCheia);
            Reduzir(_anel[indice], 1f, 0.5f, 0.5f);
            Reduzir(
                _zoom[indice],
                ConfigRelato.FracaoDoZoom,
                ConfigRelato.CentroXDoZoom,
                ConfigRelato.CentroYDoZoomDoTopo
            );
        }

        // Copia para o quadro guardado a região da tela de largura e altura "fracao", centrada em
        // (centroX, centroYDoTopo), em fração da tela. Em APIs com a origem da textura no topo
        // (Direct3D, Metal, Vulkan) a captura vem de ponta-cabeça: desvira ao reduzir.
        private void Reduzir(
            RenderTexture destino,
            float fracao,
            float centroX,
            float centroYDoTopo
        )
        {
            var deslocamentoX = centroX - fracao / 2f;
            if (SystemInfo.graphicsUVStartsAtTop)
            {
                Graphics.Blit(
                    _telaCheia,
                    destino,
                    new Vector2(fracao, -fracao),
                    new Vector2(deslocamentoX, centroYDoTopo + fracao / 2f)
                );
            }
            else
            {
                Graphics.Blit(
                    _telaCheia,
                    destino,
                    new Vector2(fracao, fracao),
                    new Vector2(deslocamentoX, 1f - centroYDoTopo - fracao / 2f)
                );
            }
        }

        private static RenderTexture NovoQuadro()
        {
            return new RenderTexture(
                ConfigRelato.LarguraDoQuadro,
                ConfigRelato.AlturaDoQuadro,
                0,
                RenderTextureFormat.ARGB32
            );
        }

        private float MedirSolavanco(Camera camera)
        {
            if (camera == null)
            {
                _quadrosDeCamera = 0;
                return 0f;
            }

            var posicao = camera.transform.position;
            var solavanco =
                _quadrosDeCamera >= 2
                    ? (posicao - 2f * _cameraAnterior + _cameraAntesDaAnterior).magnitude
                    : 0f;
            _cameraAntesDaAnterior = _cameraAnterior;
            _cameraAnterior = posicao;
            _quadrosDeCamera = Math.Min(_quadrosDeCamera + 1, 2);
            return solavanco;
        }

        private void BuscarAlvo(GameObject jogador)
        {
            if (jogador == null || Time.unscaledTime < _proximaBusca)
            {
                return;
            }

            _proximaBusca = Time.unscaledTime + ConfigRelato.IntervaloDaBuscaDoAlvo;
            _alvo = null;
            var centro = jogador.transform.position;
            var melhor = ConfigRelato.RaioDoAlvo;
            foreach (var animator in FindObjectsByType<Animator>(FindObjectsInactive.Exclude))
            {
                if (!animator.isActiveAndEnabled || animator.transform.IsChildOf(jogador.transform))
                {
                    continue;
                }

                var distancia = Vector3.Distance(centro, animator.transform.position);
                if (distancia <= melhor)
                {
                    melhor = distancia;
                    _alvo = animator;
                }
            }
        }

        private static EstadoDoAnimator Ler(Animator animator, GameObject jogador)
        {
            if (
                animator == null
                || !animator.isActiveAndEnabled
                || animator.runtimeAnimatorController == null
            )
            {
                return default;
            }

            var estado = new EstadoDoAnimator
            {
                Existe = true,
                Objeto = animator.transform.root.name,
                Velocidade = animator.speed,
                Normalizado = animator.GetCurrentAnimatorStateInfo(0).normalizedTime,
                Clipe = "(sem clipe)",
                Distancia =
                    jogador != null
                        ? Vector3.Distance(jogador.transform.position, animator.transform.position)
                        : 0f,
            };

            estado.Clipe = ClipeDaCamada(animator, 0) ?? "(sem clipe)";
            // Camadas de cima com peso (ex.: o golpe no tronco) entram depois de "+".
            for (var camada = 1; camada < animator.layerCount; camada++)
            {
                if (animator.GetLayerWeight(camada) <= 0.01f)
                {
                    continue;
                }

                var clipe = ClipeDaCamada(animator, camada);
                if (clipe != null)
                {
                    estado.Clipe += " + " + clipe;
                }
            }

            return estado;
        }

        // Clipe de maior peso na camada, com "*" se ela está em transição; null sem clipe.
        private static string ClipeDaCamada(Animator animator, int camada)
        {
            string nome = null;
            var peso = -1f;
            foreach (var clipe in animator.GetCurrentAnimatorClipInfo(camada))
            {
                if (clipe.clip != null && clipe.weight > peso)
                {
                    peso = clipe.weight;
                    nome = clipe.clip.name;
                }
            }

            return nome != null && animator.IsInTransition(camada) ? nome + "*" : nome;
        }

        private void AnotarEntrada()
        {
            var teclado = Keyboard.current;
            if (teclado != null)
            {
                foreach (var tecla in teclado.allKeys)
                {
                    Anotar(tecla, tecla.keyCode == Key.F9 || tecla.keyCode == Key.F10);
                }
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                Anotar(mouse.leftButton, false, "Mouse 1");
                Anotar(mouse.rightButton, false, "Mouse 2");
                Anotar(mouse.middleButton, false, "Mouse 3");
            }
        }

        private void Anotar(ButtonControl botao, bool ignorar, string nome = null)
        {
            if (botao == null || ignorar)
            {
                return;
            }

            if (botao.wasPressedThisFrame)
            {
                Separar().Append('↓').Append(nome ?? botao.displayName);
            }
            else if (botao.wasReleasedThisFrame)
            {
                Separar().Append('↑').Append(nome ?? botao.displayName);
            }
        }

        private StringBuilder Separar()
        {
            return _entrada.Length > 0 ? _entrada.Append(' ') : _entrada;
        }

        // Nome de cada Id pelas constantes dos *MessageIds do shared (ex.: 6002 → AtaqueAcertou).
        private static Dictionary<ushort, string> NomesDasMensagens()
        {
            var nomes = new Dictionary<ushort, string>();
            foreach (var tipo in typeof(CombateMessageIds).Assembly.GetTypes())
            {
                if (!tipo.Name.EndsWith("MessageIds", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var campo in tipo.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (campo.IsLiteral && campo.FieldType == typeof(ushort))
                    {
                        nomes[(ushort)campo.GetRawConstantValue()] = campo.Name;
                    }
                }
            }

            return nomes;
        }

        private static void Liberar(RenderTexture textura)
        {
            if (textura == null)
            {
                return;
            }

            textura.Release();
            Destroy(textura);
        }
    }

    /// <summary>O que estava guardado quando a pessoa apertou F9, do mais antigo ao mais novo.</summary>
    internal sealed class Momento
    {
        public Momento(
            IReadOnlyList<AmostraDeQuadro> quadros,
            IReadOnlyList<RenderTexture> texturas,
            IReadOnlyList<RenderTexture> zooms,
            IReadOnlyList<EventoDeRede> eventos
        )
        {
            Quadros = quadros;
            Texturas = texturas;
            Zooms = zooms;
            Eventos = eventos;
        }

        public IReadOnlyList<AmostraDeQuadro> Quadros { get; }
        public IReadOnlyList<RenderTexture> Texturas { get; }

        /// <summary>O centro da tela ampliado (jogador e alvo), na mesma ordem.</summary>
        public IReadOnlyList<RenderTexture> Zooms { get; }
        public IReadOnlyList<EventoDeRede> Eventos { get; }
    }
}
