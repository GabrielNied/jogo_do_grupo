using System;
using System.Reflection;
using JogoDoGrupo.Client.Core.Jogo.Controle;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Personagem.Editor.Playground
{
    /// <summary>Contexto local do Editor; os componentes reais continuam donos do input, movimento e camera.</summary>
    [InitializeOnLoad]
    public static class RuntimeCharacterPlayground
    {
        const string ChaveIndice = "CharacterPlayground.Indice";
        static IDisposable _acoes;
        static Func<bool> _esc;
        static Func<bool> _clique;
        static GameObject _alvo;
        static GameObject _objetoCamera;
        static Camera _inspecao;
        static bool _inspecaoEstavaAtiva;
        static bool _cursorSolto;
        static CursorLockMode _cursorAnterior;
        static bool _cursorVisivelAnterior;
        public static ControleTerceiraPessoa Controle { get; private set; }
        public static CameraTerceiraPessoa Camera { get; private set; }
        public static string Erro { get; private set; }
        public static int Indice
        {
            get => SessionState.GetInt(ChaveIndice, 0);
            set => SessionState.SetInt(ChaveIndice, value);
        }
        public static bool Ativo =>
            Application.isPlaying && SceneManager.GetActiveScene().path == CharacterPlayground.Cena;

        static RuntimeCharacterPlayground()
        {
            EditorApplication.update += Atualizar;
            EditorApplication.playModeStateChanged += estado =>
            {
                if (
                    estado == PlayModeStateChange.ExitingPlayMode
                    || estado == PlayModeStateChange.EnteredEditMode
                )
                    Liberar();
                if (estado == PlayModeStateChange.EnteredPlayMode)
                    Erro = null;
            };
            AssemblyReloadEvents.beforeAssemblyReload += Liberar;
        }

        static void Atualizar()
        {
            if (!Ativo)
            {
                if (_acoes != null)
                    Liberar();
                return;
            }
            if (Erro != null)
                return;
            try
            {
                if (_alvo == null || _alvo != CharacterPlayground.Atual)
                {
                    if (CharacterPlayground.Atual == null)
                        CharacterPlayground.Spawn(Indice);
                    else
                        Conectar(CharacterPlayground.Atual);
                }
                if (Controle == null || Camera == null)
                    return;
                var foco =
                    EditorWindow.focusedWindow != null
                    && EditorWindow.focusedWindow.GetType().FullName == "UnityEditor.GameView";
                var ler = foco && !_cursorSolto && Application.isFocused;
                Controle.InputAtivo = ler;
                Camera.InputAtivo = ler;
                Cursor.lockState = ler ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !ler;
            }
            catch (Exception erro)
            {
                Liberar();
                Erro = erro.GetBaseException().Message;
                Debug.LogError("[CharacterPlayground] " + Erro);
            }
        }

        public static void Conectar(GameObject personagem)
        {
            if (!Ativo)
                throw new InvalidOperationException(
                    "Contexto de movimento permitido somente no Playground em Play Mode."
                );
            Liberar();
            // A sessao real exige rede e UI. Sua composicao e interna ao Core; refletir
            // somente estas assinaturas, uma vez ao nascer, evita abrir Core ou copiar logica.
            var assembly = typeof(ControleTerceiraPessoa).Assembly;
            var tipoAcoes = assembly.GetType(
                "JogoDoGrupo.Client.Core.Jogo.Controle.AcoesJogador",
                true
            );
            var configurarControle = typeof(ControleTerceiraPessoa).GetMethod(
                "Configurar",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { tipoAcoes, typeof(CameraTerceiraPessoa), typeof(AnimadorDoVisual) },
                null
            );
            var configurarCamera = typeof(CameraTerceiraPessoa).GetMethod(
                "Configurar",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { tipoAcoes, typeof(Transform) },
                null
            );
            var ativar = tipoAcoes.GetMethod("Ativar", Type.EmptyTypes);
            var esc = tipoAcoes.GetProperty("MenuApertado")?.GetMethod;
            var clique = tipoAcoes.GetProperty("ClicarApertado")?.GetMethod;
            if (
                configurarControle == null
                || configurarCamera == null
                || ativar == null
                || esc == null
                || clique == null
                || !typeof(IDisposable).IsAssignableFrom(tipoAcoes)
            )
                throw new InvalidOperationException(
                    "A API interna do Core mudou. Rever o adaptador antes de controlar o personagem."
                );
            _acoes = (IDisposable)Activator.CreateInstance(tipoAcoes);
            _esc = (Func<bool>)esc.CreateDelegate(typeof(Func<bool>), _acoes);
            _clique = (Func<bool>)clique.CreateDelegate(typeof(Func<bool>), _acoes);
            _alvo = personagem;
            _cursorAnterior = Cursor.lockState;
            _cursorVisivelAnterior = Cursor.visible;
            _cursorSolto = false;
            var bounds = CharacterPlayground.Medir(personagem);
            var animator = personagem.GetComponentInChildren<Animator>();
            // Configurar somente a instancia local; prefab e Avatar originais intactos.
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(
                CharacterPlayground.PrefabInicial
            );
            if (
                animator != null
                && original != null
                && animator.avatar == original.GetComponentInChildren<Animator>().avatar
            )
            {
                animator.runtimeAnimatorController =
                    AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        CharacterPlayground.Pasta + "/Locomotion/Locomotion.controller"
                    );
                animator.applyRootMotion = false;
                animator.Rebind();
            }
            var corpo = personagem.GetComponent<CharacterController>();
            if (corpo == null)
                corpo = personagem.AddComponent<CharacterController>();
            corpo.height = bounds.size.y;
            corpo.radius = Mathf.Min(bounds.size.y / 6f, 0.35f);
            corpo.skinWidth = 0.03f;
            corpo.center = new Vector3(0, bounds.size.y / 2f + corpo.skinWidth, 0);
            corpo.stepOffset = 0.3f;
            corpo.slopeLimit = 45f;
            Controle = personagem.GetComponent<ControleTerceiraPessoa>();
            if (Controle == null)
                Controle = personagem.AddComponent<ControleTerceiraPessoa>();
            _inspecao = CharacterPlayground.Raiz("Camera de inspecao")?.GetComponent<Camera>();
            if (_inspecao != null)
            {
                _inspecaoEstavaAtiva = _inspecao.enabled;
                _inspecao.enabled = false;
            }
            _objetoCamera = new GameObject("CameraPlaygroundGameplay");
            SceneManager.MoveGameObjectToScene(_objetoCamera, personagem.scene);
            _objetoCamera.tag = "MainCamera";
            var camera = _objetoCamera.AddComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.fieldOfView = 60f;
            _objetoCamera.AddComponent<AudioListener>();
            Camera = _objetoCamera.AddComponent<CameraTerceiraPessoa>();
            configurarControle.Invoke(
                Controle,
                new object[]
                {
                    _acoes,
                    Camera,
                    new AnimadorDoVisual(personagem.GetComponentInChildren<Animator>()),
                }
            );
            configurarCamera.Invoke(Camera, new object[] { _acoes, personagem.transform });
            ativar.Invoke(_acoes, null);
            InputSystem.onAfterUpdate += LerGestos;
            Erro = null;
        }

        static void LerGestos()
        {
            // WasPressedThisFrame deve ser lido no ciclo do Input System, nao no
            // EditorApplication.update, que pode ocorrer depois do frame que o gerou.
            if (_esc != null && _esc())
                _cursorSolto = true;
            var foco =
                EditorWindow.focusedWindow != null
                && EditorWindow.focusedWindow.GetType().FullName == "UnityEditor.GameView";
            if (foco && _clique != null && _clique())
                _cursorSolto = false;
        }

        public static void PrepararCena()
        {
            if (!CharacterPlayground.PodeEditar)
                throw new InvalidOperationException("Abra o Playground em Edit Mode.");
            var chao = CharacterPlayground.Raiz("Ambiente metrico")?.Find("Chao 12 x 12 m");
            if (chao == null)
                throw new InvalidOperationException("Chao metrico ausente.");
            if (chao.GetComponent<BoxCollider>() == null)
                Undo.AddComponent<BoxCollider>(chao.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene()
            );
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        public static void Liberar()
        {
            InputSystem.onAfterUpdate -= LerGestos;
            var tinhaContexto = _acoes != null;
            if (Controle != null)
                Controle.InputAtivo = false;
            if (Camera != null)
                Camera.enabled = false;
            _acoes?.Dispose();
            _acoes = null;
            _esc = null;
            _clique = null;
            if (_objetoCamera != null)
            {
                _objetoCamera.SetActive(false);
                Object.DestroyImmediate(_objetoCamera);
            }
            if (_inspecao != null)
                _inspecao.enabled = _inspecaoEstavaAtiva;
            if (tinhaContexto)
            {
                Cursor.lockState = _cursorAnterior;
                Cursor.visible = _cursorVisivelAnterior;
            }
            _alvo = null;
            Controle = null;
            Camera = null;
        }

        public static string Diagnosticar()
        {
            if (!Application.isPlaying)
                return "Play Mode: não";
            if (!Ativo)
                return "Play Mode: outra cena (contexto desligado).";
            if (Erro != null)
                return "Falha de contexto: " + Erro;
            if (Controle == null || Camera == null)
                return "Preparando contexto local...";
            var corpo = Controle.GetComponent<CharacterController>();
            var animator = Controle.GetComponentInChildren<Animator>();
            var animacao =
                animator == null ? "ausente"
                : animator.runtimeAnimatorController == null ? "T-pose (sem clips de locomoção)"
                : animator.GetCurrentAnimatorStateInfo(0).IsName("Locomocao")
                    ? "Locomocao (Blend Tree)"
                : animator.GetCurrentAnimatorStateInfo(0).fullPathHash.ToString();
            var clips =
                animator == null || animator.runtimeAnimatorController == null
                    ? ""
                    : string.Join(
                        " • ",
                        System.Linq.Enumerable.Select(
                            animator.GetCurrentAnimatorClipInfo(0),
                            c => $"{c.clip.name}: {c.weight:P0}"
                        )
                    );
            var temParametros =
                animator != null
                && animator.runtimeAnimatorController != null
                && System.Linq.Enumerable.Any(
                    animator.parameters,
                    p =>
                        p.name == AnimadorDoVisual.ParametroVelocidade
                        && p.type == AnimatorControllerParameterType.Float
                )
                && System.Linq.Enumerable.Any(
                    animator.parameters,
                    p =>
                        p.name == AnimadorDoVisual.ParametroNoChao
                        && p.type == AnimatorControllerParameterType.Bool
                );
            var parametros = !temParametros
                ? ""
                : $"\nVelocidade: {animator.GetFloat(AnimadorDoVisual.ParametroVelocidade):F3} m/s\nNoChao: {animator.GetBool(AnimadorDoVisual.ParametroNoChao)}\nClips: {clips}";
            return $"Play Mode: sim\nControlado: {Controle.name}\nGrounded: {Controle.NoChao}\nVelocidade alvo: {Controle.VelocidadeHorizontal:F3} m/s\nVelocidade real: {corpo.velocity.magnitude:F3} m/s\nDireção real: {corpo.velocity.normalized}\nAnimator: {animacao}\nController: {animator?.runtimeAnimatorController?.name}{parametros}\nCâmera real: {Camera.name}\nInput ativo: {Controle.InputAtivo}\nWASD/setas • Ctrl: andar • Espaço: pular\nMouse: órbita • roda: zoom • Esc: liberar cursor\nClique no Game View para retomar.";
        }
    }
}
