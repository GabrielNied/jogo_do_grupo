using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Core;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Relato
{
    /// <summary>
    /// Guarda o passado recente do jogo para o report (F9 ou J): por
    /// <see cref="ConfigRelato.SegundosDaLinhaDoTempo"/>, o estado dos Animators do jogador e do
    /// mais próximo, o <c>timeScale</c>, o solavanco da câmera, as teclas e as mensagens do
    /// servidor; pelos últimos <see cref="ConfigRelato.SegundosGuardados"/>, a tela em miniatura.
    /// Guarda também a última mensagem de cada tipo e os avisos e erros do console. Só existe no
    /// editor e no build de desenvolvimento. Roda depois de todos (câmera, recuo e tremor já
    /// aplicados no quadro).
    /// </summary>
    [DefaultExecutionOrder(10000)]
    internal sealed class GravadorDeMomento : MonoBehaviour
    {
        private const string NomeObjeto = "Desenvolvimento.Relato";

        private static GravadorDeMomento _instancia;

        private readonly List<EventoDeRede> _eventos = new List<EventoDeRede>();
        private readonly Dictionary<ushort, EventoDeRede> _ultimas =
            new Dictionary<ushort, EventoDeRede>();
        private readonly List<LinhaDoConsole> _console = new List<LinhaDoConsole>();
        private readonly StringBuilder _entrada = new StringBuilder();

        private RenderTexture[] _anel;
        private RenderTexture[] _zoom;
        private int _proximaImagem;
        private int _imagens;
        private AmostraDeQuadro[] _amostras;
        private int _proximaAmostra;
        private int _guardadas;
        private string _janelaAberta;
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
        private PainelDeAjustes _ajustes;

        /// <summary>O gravador da sessão, ou <c>null</c> (build de release, ou antes de iniciar).</summary>
        public static GravadorDeMomento Instancia => _instancia;

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
            EnvioDeReporte.EnviarPendentes();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
        }

        private void Awake()
        {
            var imagens = Mathf.CeilToInt(
                ConfigRelato.SegundosGuardados * ConfigRelato.QuadrosPorSegundo
            );
            _anel = new RenderTexture[imagens];
            _zoom = new RenderTexture[imagens];
            _amostras = new AmostraDeQuadro[
                Mathf.CeilToInt(
                    ConfigRelato.SegundosDaLinhaDoTempo * ConfigRelato.QuadrosPorSegundo
                )
            ];
            _nomes = NomesDasMensagens();
            _f9 = new InputAction("Relato", InputActionType.Button, "<Keyboard>/f9");
            _f10 = new InputAction("Ajustes", InputActionType.Button, "<Keyboard>/f10");
            _f9.Enable();
            _f10.Enable();
            _ajustes = new PainelDeAjustes(this);
            BloqueioDeEntradaPorTexto.RegistrarBloqueioDeAtalhos(() => _ajustes.Aberto);
        }

        private void OnEnable()
        {
            _assinatura = ConexaoTempoReal.AssinarFaixa(
                new MessageIdRange(0, ushort.MaxValue),
                AoReceber
            );
            Application.logMessageReceived += AoLogar;
            StartCoroutine(CapturarNoFimDoQuadro());
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= AoLogar;
            _assinatura?.Dispose();
            _assinatura = null;
        }

        private void OnDestroy()
        {
            _f9?.Dispose();
            _f10?.Dispose();
            _ajustes?.Destruir();
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
            EnvioDeReporte.Atualizar();
            if (!EntradaNoJogo.NoJogo)
            {
                return;
            }

            var esc = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            _ajustes.Atualizar(esc);
            JanelaReporte.Atualizar();
            if (_f9.WasPressedThisFrame())
            {
                JanelaReporte.Alternar();
            }
            else if (_f10.WasPressedThisFrame())
            {
                _ajustes.Alternar();
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
            _janelaAberta = JanelasDoJogo.ChaveAberta;
            var jogador = EntradaNoJogo.Jogador;
            BuscarAlvo(jogador);
            _amostras[_proximaAmostra] = new AmostraDeQuadro
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
            _proximaAmostra = (_proximaAmostra + 1) % _amostras.Length;
            _guardadas = Math.Min(_guardadas + 1, _amostras.Length);
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
                CapturarTela(_proximaImagem);
                _proximaImagem = (_proximaImagem + 1) % _anel.Length;
                _imagens = Math.Min(_imagens + 1, _anel.Length);
            }
        }

        /// <summary>Para de guardar (o painel abriu) ou volta a guardar do zero.</summary>
        public void DefinirGravando(bool gravando)
        {
            if (gravando && !Gravando)
            {
                _guardadas = 0;
                _proximaAmostra = 0;
                _imagens = 0;
                _proximaImagem = 0;
                _eventos.Clear();
                _quadrosDeCamera = 0;
            }

            Gravando = gravando;
            _capturarNoFim = false;
        }

        /// <summary>
        /// O que está guardado, do mais antigo ao mais novo. As imagens são dos últimos quadros
        /// (veja <see cref="Momento.PrimeiraImagem"/>). Quem recebe chama
        /// <see cref="Momento.Liberar"/> no fim.
        /// </summary>
        public Momento Congelar()
        {
            var quadros = new List<AmostraDeQuadro>(_guardadas);
            var inicio = (_proximaAmostra - _guardadas + _amostras.Length) % _amostras.Length;
            for (var i = 0; i < _guardadas; i++)
            {
                quadros.Add(_amostras[(inicio + i) % _amostras.Length]);
            }

            // Cada quadro guardado também ganhou imagem, então as imagens são dos últimos.
            var comImagem = Math.Min(_imagens, quadros.Count);
            var texturas = new List<RenderTexture>(comImagem);
            var zooms = new List<RenderTexture>(comImagem);
            var primeira = (_proximaImagem - comImagem + _anel.Length) % _anel.Length;
            for (var i = 0; i < comImagem; i++)
            {
                texturas.Add(_anel[(primeira + i) % _anel.Length]);
                zooms.Add(_zoom[(primeira + i) % _anel.Length]);
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

            var ultimas = new List<EventoDeRede>(_ultimas.Values);
            ultimas.Sort((a, b) => string.CompareOrdinal(a.Nome, b.Nome));
            return new Momento(quadros, texturas, zooms, eventos)
            {
                Ultimas = ultimas,
                Console = new List<LinhaDoConsole>(_console),
                Tela = CopiarTela(),
                JanelaAberta = _janelaAberta,
                Agora = Time.realtimeSinceStartupAsDouble,
                QuandoUtc = DateTime.UtcNow,
            };
        }

        // A tela inteira do último quadro guardado (o report ainda não estava aberto nele).
        private Texture2D CopiarTela()
        {
            if (_telaCheia == null || _imagens == 0)
            {
                return null;
            }

            var anterior = RenderTexture.active;
            RenderTexture.active = _telaCheia;
            var tela = new Texture2D(
                _telaCheia.width,
                _telaCheia.height,
                TextureFormat.RGB24,
                false
            );
            tela.ReadPixels(new Rect(0, 0, _telaCheia.width, _telaCheia.height), 0, 0);
            RenderTexture.active = anterior;

            // Como no Reduzir: com a origem da textura no topo (Direct3D, Metal, Vulkan) a
            // captura vem de ponta-cabeça.
            if (SystemInfo.graphicsUVStartsAtTop)
            {
                Desvirar(tela);
            }

            tela.Apply();
            return tela;
        }

        private static void Desvirar(Texture2D imagem)
        {
            var pixels = imagem.GetPixels32();
            var largura = imagem.width;
            var linha = new Color32[largura];
            for (int cima = 0, baixo = imagem.height - 1; cima < baixo; cima++, baixo--)
            {
                Array.Copy(pixels, cima * largura, linha, 0, largura);
                Array.Copy(pixels, baixo * largura, pixels, cima * largura, largura);
                Array.Copy(linha, 0, pixels, baixo * largura, largura);
            }

            imagem.SetPixels32(pixels);
        }

        private void AoLogar(string mensagem, string pilha, LogType tipo)
        {
            if (tipo == LogType.Log)
            {
                return;
            }

            _console.Add(
                new LinhaDoConsole(Time.realtimeSinceStartupAsDouble, tipo, mensagem, pilha)
            );
            if (_console.Count > ConfigRelato.LinhasDoConsole)
            {
                _console.RemoveAt(0);
            }
        }

        private void AoReceber(MensagemTempoReal mensagem)
        {
            var agora = Time.realtimeSinceStartupAsDouble;
            var evento = new EventoDeRede(
                agora,
                mensagem.Id,
                _nomes.TryGetValue(mensagem.Id, out var nome) ? nome : "Id" + mensagem.Id,
                mensagem.Json
            );
            _ultimas[mensagem.Id] = evento;
            if (!Gravando)
            {
                return;
            }

            _eventos.Add(evento);
            var limite = agora - ConfigRelato.SegundosDaLinhaDoTempo - 1.0;
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

    /// <summary>O que estava guardado quando a pessoa abriu o report, do mais antigo ao mais novo.</summary>
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

        /// <summary>Imagem dos últimos quadros: a de índice i é do quadro <c>PrimeiraImagem + i</c>.</summary>
        public IReadOnlyList<RenderTexture> Texturas { get; }

        /// <summary>O centro da tela ampliado (jogador e alvo), na mesma ordem das texturas.</summary>
        public IReadOnlyList<RenderTexture> Zooms { get; }
        public IReadOnlyList<EventoDeRede> Eventos { get; }

        /// <summary>Índice do primeiro quadro com imagem.</summary>
        public int PrimeiraImagem => Quadros.Count - Texturas.Count;

        /// <summary>A última mensagem recebida de cada tipo na sessão, por nome.</summary>
        public IReadOnlyList<EventoDeRede> Ultimas { get; set; } = new List<EventoDeRede>();

        /// <summary>Os últimos avisos e erros do console, do mais antigo ao mais novo.</summary>
        public IReadOnlyList<LinhaDoConsole> Console { get; set; } = new List<LinhaDoConsole>();

        /// <summary>A tela inteira antes do report (ou <c>null</c>).</summary>
        public Texture2D Tela { get; set; }

        /// <summary>A janela do jogo aberta antes do report (ou <c>null</c>).</summary>
        public string JanelaAberta { get; set; }

        /// <summary>Relógio real do congelamento, na escala de <see cref="AmostraDeQuadro.Tempo"/>.</summary>
        public double Agora { get; set; }

        public DateTime QuandoUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Solta a cópia da tela (as texturas do anel continuam do gravador).</summary>
        public void Liberar()
        {
            if (Tela != null)
            {
                UnityEngine.Object.Destroy(Tela);
                Tela = null;
            }
        }
    }
}
