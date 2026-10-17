using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using JogoDoGrupo.Client.Core.Visual;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Personagem.Editor.Playground
{
    /// <summary>Exercita o input real e os componentes reais em frames de Play Mode, sem teclado/mouse do desktop.</summary>
    public static class ValidacaoRuntimePlayground
    {
        [Serializable]
        sealed class Relatorio
        {
            public string status = "EM_EXECUCAO";
            public string erro;
            public string diagnostico;
            public List<string> verificacoes = new List<string>();
            public List<string> avisos = new List<string>();
            public List<string> amostras = new List<string>();
        }

        public static async Task Executar(string pasta)
        {
            if (!RuntimeCharacterPlayground.Ativo)
                throw new InvalidOperationException("Entre em Play Mode no Playground.");
            Directory.CreateDirectory(pasta);
            var relatorio = new Relatorio();
            var originais = AssetDatabase
                .GetDependencies(CharacterPlayground.PrefabInicial, true)
                .SelectMany(p => new[] { p, p + ".meta" })
                .Where(File.Exists)
                .ToDictionary(p => p, Hash);
            // Impedir que movimento fisico/warp do cursor altere a orbita medida pelo teste.
            var fisicos = InputSystem
                .devices.Where(d => d.enabled && (d is Keyboard || d is Mouse))
                .ToArray();
            foreach (var dispositivo in fisicos)
            {
                // Disabled devices continuam nos bindings de compositos. Neutralizar
                // teclas retidas antes de criar novas acoes durante Reset.
                if (dispositivo is Keyboard)
                    InputSystem.ResetDevice(dispositivo);
                InputSystem.DisableDevice(dispositivo);
            }
            var teclado = InputSystem.AddDevice<Keyboard>("PlaygroundValidacaoTeclado");
            var mouse = InputSystem.AddDevice<Mouse>("PlaygroundValidacaoMouse");
            var estadoTeclado = new KeyboardState();
            MouseState? estadoMouse = null;
            // Eventos disparados por continuacoes async podem ser consumidos pelo
            // update Editor. Injetar no Dynamic preserva as acoes reais do Play.
            Action injetar = () =>
            {
                if (InputState.currentUpdateType != InputUpdateType.Dynamic)
                    return;
                InputSystem.QueueStateEvent(teclado, estadoTeclado);
                if (estadoMouse.HasValue)
                {
                    InputSystem.QueueStateEvent(mouse, estadoMouse.Value);
                    estadoMouse = null;
                }
            };
            InputSystem.onBeforeUpdate += injetar;
            Application.LogCallback registrar = (texto, pilha, tipo) =>
            {
                if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Warning)
                    relatorio.avisos.Add(tipo + ": " + texto);
            };
            Application.logMessageReceived += registrar;
            try
            {
                var game = EditorWindow.GetWindow(
                    typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")
                );
                game.Focus();
                await Quadros(5);
                Exigir(
                    RuntimeCharacterPlayground.Erro == null
                        && RuntimeCharacterPlayground.Controle != null
                        && RuntimeCharacterPlayground.Camera != null,
                    "Contexto conecta os componentes reais sem sessao"
                );
                // Clique virtual e o gesto existente para retomar input depois do Esc.
                estadoMouse = new MouseState().WithButton(MouseButton.Left);
                await Quadros(3);
                estadoMouse = new MouseState();
                await Quadros(3);
                Exigir(RuntimeCharacterPlayground.Controle.InputAtivo, "Input ativo no Game View");
                var casos = new[]
                {
                    ("frente", new[] { Key.W }, Vector3.forward),
                    ("tras", new[] { Key.S }, Vector3.back),
                    ("esquerda", new[] { Key.A }, Vector3.left),
                    ("direita", new[] { Key.D }, Vector3.right),
                    ("diagonal-direita", new[] { Key.W, Key.D }, new Vector3(1, 0, 1).normalized),
                    ("diagonal-esquerda", new[] { Key.W, Key.A }, new Vector3(-1, 0, 1).normalized),
                };
                foreach (var (nome, teclas, direcao) in casos)
                {
                    estadoTeclado = new KeyboardState();
                    CharacterPlayground.Reset(0);
                    game.Focus();
                    await Quadros(4);
                    var antes = CharacterPlayground.Atual.transform.position;
                    var cameraAntes = RuntimeCharacterPlayground.Camera.transform.position;
                    estadoTeclado = new KeyboardState(teclas);
                    await Quadros(22);
                    var delta = CharacterPlayground.Atual.transform.position - antes;
                    delta.y = 0;
                    relatorio.amostras.Add(
                        $"{nome}: delta={delta:F4}; velocidade={RuntimeCharacterPlayground.Controle.VelocidadeHorizontal:F3}; input={RuntimeCharacterPlayground.Controle.InputAtivo}; foco={EditorWindow.focusedWindow?.GetType().FullName}; appFoco={Application.isFocused}; yaw={RuntimeCharacterPlayground.Camera.Horizontal:F2}"
                    );
                    Exigir(
                        delta.magnitude > 0.2f && Vector3.Dot(delta.normalized, direcao) > 0.9f,
                        "Movimento " + nome + " via Input System"
                    );
                    Exigir(
                        Vector3.Dot(CharacterPlayground.Atual.transform.forward, direcao) > 0.85f,
                        "Rotacao acompanha " + nome
                    );
                    Exigir(
                        RuntimeCharacterPlayground.Controle.VelocidadeHorizontal
                            <= RuntimeCharacterPlayground.Controle.Movimento.VelocidadeCorrer
                                + 0.01f,
                        "Velocidade limitada inclusive em " + nome
                    );
                    Exigir(
                        Vector3.Distance(
                            cameraAntes,
                            RuntimeCharacterPlayground.Camera.transform.position
                        ) > 0.1f,
                        "Camera acompanha " + nome
                    );
                    relatorio.amostras.Add(
                        $"{nome}: posicao={CharacterPlayground.Atual.transform.position:F4}; velocidade={RuntimeCharacterPlayground.Controle.VelocidadeHorizontal:F3}; grounded={RuntimeCharacterPlayground.Controle.NoChao}"
                    );
                    if (nome == "frente" || nome == "diagonal-direita")
                        Capturar(pasta, nome);
                }
                estadoTeclado = new KeyboardState(Key.S);
                await Quadros(24);
                Exigir(
                    Vector3.Dot(CharacterPlayground.Atual.transform.forward, Vector3.back) > 0.9f,
                    "Mudanca rapida de direcao"
                );
                estadoTeclado = new KeyboardState();
                await Quadros(35);
                Exigir(
                    RuntimeCharacterPlayground.Controle.VelocidadeHorizontal < 0.02f,
                    "Parar desacelera ate zero"
                );
                CharacterPlayground.Reset(0);
                await Quadros(5);
                estadoTeclado = new KeyboardState(Key.W, Key.LeftCtrl);
                await Quadros(22);
                Exigir(
                    Mathf.Abs(
                        RuntimeCharacterPlayground.Controle.VelocidadeHorizontal
                            - RuntimeCharacterPlayground.Controle.Movimento.VelocidadeAndar
                    ) < 0.05f,
                    "Ctrl reutiliza caminhada a 2 m/s"
                );
                estadoTeclado = new KeyboardState();
                CharacterPlayground.Reset(0);
                await Quadros(6);
                Exigir(
                    RuntimeCharacterPlayground.Controle.NoChao,
                    "Grounded pelo CharacterController no collider local"
                );
                var yInicial = CharacterPlayground.Atual.transform.position.y;
                estadoTeclado = new KeyboardState(Key.Space);
                await Quadros(6);
                estadoTeclado = new KeyboardState();
                Exigir(
                    CharacterPlayground.Atual.transform.position.y > yInicial + 0.1f
                        && !RuntimeCharacterPlayground.Controle.NoChao,
                    "Pulo existente via Espaco"
                );
                Capturar(pasta, "pulo");
                await Quadros(100);
                Exigir(
                    RuntimeCharacterPlayground.Controle.NoChao
                        && Mathf.Abs(CharacterPlayground.Atual.transform.position.y - yInicial)
                            < 0.06f,
                    "Gravidade retorna ao chao"
                );
                var horizontal = RuntimeCharacterPlayground.Camera.Horizontal;
                estadoMouse = new MouseState { delta = new Vector2(300, 20) };
                await Quadros(4);
                Exigir(
                    Mathf.Abs(
                        Mathf.DeltaAngle(horizontal, RuntimeCharacterPlayground.Camera.Horizontal)
                    ) > 1f,
                    "Orbita real responde ao mouse virtual"
                );
                var distancia = RuntimeCharacterPlayground.Camera.Orbita.Distancia;
                estadoMouse = new MouseState { scroll = new Vector2(0, 120) };
                await Quadros(4);
                Exigir(
                    RuntimeCharacterPlayground.Camera.Orbita.Distancia < distancia,
                    "Roda reutiliza zoom da camera real"
                );
                Capturar(pasta, "orbita");
                var antigo = CharacterPlayground.Atual;
                var corpo = antigo.GetComponent<CharacterController>();
                corpo.enabled = false;
                antigo.transform.position = new Vector3(20, -5, 20);
                corpo.enabled = true;
                CharacterPlayground.Reset(0);
                await Quadros(5);
                var atual = CharacterPlayground.Atual;
                Exigir(
                    antigo == null
                        && Vector3.Distance(
                            atual.transform.position,
                            CharacterPlayground.Raiz("Spawn Point").position
                        ) < 0.06f
                        && RuntimeCharacterPlayground.Controle.VelocidadeHorizontal < 0.01f,
                    "Reset recupera queda e zera estado do controle"
                );
                Exigir(
                    CharacterPlayground
                        .Raiz("PersonagemAtual")
                        .Cast<Transform>()
                        .Count(t => t.gameObject.activeSelf) == 1,
                    "Uma instancia controlada apos repetidos resets"
                );
                var animator = atual.GetComponentInChildren<Animator>();
                Exigir(
                    animator != null
                        && animator.avatar.isValid
                        && animator.isHuman
                        && atual.transform.lossyScale == Vector3.one,
                    "Escala Avatar e Humanoid preservados"
                );
                Exigir(
                    CharacterPlayground.Sockets.All(s =>
                        CharacterPlayground.Socket(atual, s) != null
                    ),
                    "Quatro sockets preservados em runtime"
                );
                Exigir(
                    animator.runtimeAnimatorController != null && !animator.applyRootMotion,
                    "Locomocao local sem root motion e sem dependencia da Idle antiga"
                );
                Exigir(
                    originais.All(p => Hash(p.Key) == p.Value),
                    "FBX prefab Avatar skeleton pesos materiais e texturas originais intactos"
                );
                Capturar(pasta, "reset");
                Exigir(
                    Object
                        .FindObjectsByType<Camera>()
                        .Count(c =>
                            c.gameObject.scene == atual.scene
                            && c.enabled
                            && c.gameObject.activeInHierarchy
                        ) == 1,
                    "Somente a camera de gameplay fica ativa em Play Mode"
                );
                estadoTeclado = new KeyboardState(Key.Escape);
                await Quadros(3);
                estadoTeclado = new KeyboardState();
                Exigir(
                    !RuntimeCharacterPlayground.Controle.InputAtivo
                        && Cursor.lockState == CursorLockMode.None,
                    "Esc libera cursor e suspende input"
                );
                estadoMouse = new MouseState().WithButton(MouseButton.Left);
                await Quadros(3);
                estadoMouse = new MouseState();
                Exigir(
                    RuntimeCharacterPlayground.Controle.InputAtivo,
                    "Clique no Game View retoma input"
                );
                var janela = ScriptableObject.CreateInstance<JanelaCharacterPlayground>();
                try
                {
                    ValidacaoCharacterPlayground.FotografarPainel(
                        janela,
                        pasta,
                        new Vector2Int(640, 2400),
                        ConfigVisual.Tipografia.Leitura
                    );
                }
                finally
                {
                    Object.DestroyImmediate(janela);
                }
                relatorio.diagnostico = RuntimeCharacterPlayground.Diagnosticar();
                Exigir(relatorio.avisos.Count == 0, "Nenhum warning ou erro durante o teste");
                relatorio.status = "PASSOU";
            }
            catch (Exception erro)
            {
                relatorio.status = "FALHOU";
                relatorio.erro = erro.ToString();
                throw;
            }
            finally
            {
                InputSystem.onBeforeUpdate -= injetar;
                estadoTeclado = new KeyboardState();
                estadoMouse = new MouseState();
                InputSystem.RemoveDevice(teclado);
                InputSystem.RemoveDevice(mouse);
                foreach (var dispositivo in fisicos)
                    InputSystem.EnableDevice(dispositivo);
                Application.logMessageReceived -= registrar;
                File.WriteAllText(
                    Path.Combine(pasta, "validacao-runtime.json"),
                    JsonUtility.ToJson(relatorio, true)
                );
            }
            void Exigir(bool passou, string descricao)
            {
                if (!passou)
                    throw new InvalidOperationException("Playground runtime: " + descricao);
                relatorio.verificacoes.Add(descricao);
            }
        }

        internal static async Task Quadros(int quantidade)
        {
            var final = Time.frameCount + quantidade;
            var tempoFinal = Time.timeAsDouble + quantidade / 60.0;
            var inicio = EditorApplication.timeSinceStartup;
            while (Time.frameCount < final || Time.timeAsDouble < tempoFinal)
            {
                if (
                    !Application.isPlaying
                    || EditorApplication.isPaused
                    || EditorApplication.timeSinceStartup - inicio > 15
                )
                    throw new InvalidOperationException(
                        "Play Mode interrompido, pausado ou sem frames."
                    );
                await Task.Delay(10);
            }
        }

        static string Hash(string caminho)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(caminho)));
        }

        internal static void Capturar(string pasta, string nome)
        {
            var camera = RuntimeCharacterPlayground.Camera.GetComponent<Camera>();
            var rt = RenderTexture.GetTemporary(1280, 720, 24);
            var anterior = camera.targetTexture;
            var ativo = RenderTexture.active;
            var imagem = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt;
                camera.Render();
                camera.Render();
                RenderTexture.active = rt;
                imagem.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                imagem.Apply();
                File.WriteAllBytes(Path.Combine(pasta, nome + ".png"), imagem.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = anterior;
                RenderTexture.active = ativo;
                RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(imagem);
            }
        }
    }
}
