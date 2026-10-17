using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JogoDoGrupo.Client.Core.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Personagem.Editor.Playground
{
    public static class ValidacaoLocomocaoPlayground
    {
        [Serializable]
        sealed class Relatorio
        {
            public string escopo = "VALIDACAO TECNICA; aprovacao visual independente e pendente";
            public string status = "EM_EXECUCAO";
            public string erro;
            public List<string> verificacoes = new List<string>();
            public List<string> amostras = new List<string>();
            public List<string> avisos = new List<string>();
        }

        // Retarget isolado antes da validacao do Blend Tree completo.
        public static async Task Retarget(string pasta)
        {
            if (!RuntimeCharacterPlayground.Ativo)
                throw new InvalidOperationException("Entre no Playground em Play.");
            Directory.CreateDirectory(pasta);
            var r = new Relatorio();
            var alvo = CharacterPlayground.Atual;
            var animator = alvo.GetComponentInChildren<Animator>();
            var controller = animator.runtimeAnimatorController;
            var camera = RuntimeCharacterPlayground.Camera.GetComponent<Camera>();
            var posCamera = camera.transform.position;
            var rotCamera = camera.transform.rotation;
            var controle = RuntimeCharacterPlayground.Controle;
            var orbita = RuntimeCharacterPlayground.Camera;
            controle.enabled = false;
            orbita.enabled = false;
            var inicio = alvo.transform.position;
            PlayableGraph graph = default;
            try
            {
                animator.runtimeAnimatorController = null;
                foreach (var nome in new[] { "Idle", "Walking", "Running" })
                {
                    var clip = AssetDatabase
                        .LoadAllAssetsAtPath(
                            CharacterPlayground.Pasta + "/Locomotion/" + nome + ".fbx"
                        )
                        .OfType<AnimationClip>()
                        .First(c => !c.name.StartsWith("__preview"));
                    Exigir(clip.isHumanMotion && clip.isLooping, nome + ": Humanoid loop", r);
                    graph = PlayableGraph.Create("RetargetPlayground");
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var playable = AnimationClipPlayable.Create(graph, clip);
                    playable.SetApplyFootIK(false);
                    var output = AnimationPlayableOutput.Create(graph, "Visual", animator);
                    output.SetSourcePlayable(playable);
                    graph.Play();
                    var minPe = float.PositiveInfinity;
                    var maxPe = float.NegativeInfinity;
                    var maxDistancia = 0f;
                    var pes = new List<Vector3>();
                    for (int q = 0; q < Mathf.CeilToInt(clip.length * 30 * 3); q++)
                    {
                        graph.Evaluate(1f / 30);
                        var pe = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                        pes.Add(alvo.transform.InverseTransformPoint(pe));
                        minPe = Mathf.Min(minPe, pe.y);
                        maxPe = Mathf.Max(maxPe, pe.y);
                        maxDistancia = Mathf.Max(
                            maxDistancia,
                            Vector3.Distance(inicio, alvo.transform.position)
                        );
                    }
                    Exigir(
                        maxDistancia < 0.001f && !animator.applyRootMotion,
                        nome + ": tres ciclos sem deslocar o root",
                        r
                    );
                    Exigir(maxPe - minPe > 0.0001f, nome + ": pose animada", r);
                    r.amostras.Add(
                        $"{nome}: root drift={maxDistancia:F6}m; pe y={minPe:F4}..{maxPe:F4}m"
                    );
                    var minLocal = pes.Min(p => p.y);
                    var velocidades = Enumerable
                        .Range(1, pes.Count - 1)
                        .Where(j => pes[j].y < minLocal + 0.04f)
                        .Select(j => -(pes[j].z - pes[j - 1].z) * 30f)
                        .Where(v => v > 0.05f && v < 15f)
                        .ToArray();
                    r.amostras.Add(
                        $"{nome}: velocidade aparente do apoio={(velocidades.Length > 0 ? velocidades.Average() : 0):F3} m/s"
                    );
                    var bones = new[]
                    {
                        HumanBodyBones.Hips,
                        HumanBodyBones.Head,
                        HumanBodyBones.LeftHand,
                        HumanBodyBones.RightHand,
                        HumanBodyBones.LeftFoot,
                        HumanBodyBones.RightFoot,
                    };
                    playable.SetTime(0);
                    graph.Evaluate(0);
                    var poseInicial = bones
                        .Select(b => animator.GetBoneTransform(b).position)
                        .ToArray();
                    playable.SetTime(clip.length - 0.001);
                    graph.Evaluate(0);
                    var saltoLoop = bones
                        .Select(
                            (b, j) =>
                                Vector3.Distance(
                                    poseInicial[j],
                                    animator.GetBoneTransform(b).position
                                )
                        )
                        .Max();
                    r.amostras.Add($"{nome}: maior diferenca na costura do loop={saltoLoop:F4} m");
                    Exigir(saltoLoop < 0.15f, nome + ": continuidade na costura do loop", r);
                    foreach (var vista in new[] { "frente", "lateral", "costas", "diagonal" })
                    {
                        playable.SetTime(clip.length * 0.28);
                        graph.Evaluate(0);
                        var deslocamento =
                            vista == "frente" ? Vector3.forward
                            : vista == "lateral" ? Vector3.right
                            : vista == "diagonal" ? new Vector3(1, 0, 1).normalized
                            : Vector3.back;
                        camera.transform.position = inicio + Vector3.up * 1.1f + deslocamento * 3f;
                        camera.transform.LookAt(inicio + Vector3.up * 0.95f);
                        await ValidacaoRuntimePlayground.Quadros(2);
                        ValidacaoRuntimePlayground.Capturar(pasta, nome + "-" + vista);
                    }
                    graph.Destroy();
                }
                r.status = "PASSOU";
            }
            catch (Exception e)
            {
                r.status = "FALHOU";
                r.erro = e.ToString();
                throw;
            }
            finally
            {
                if (graph.IsValid())
                    graph.Destroy();
                animator.runtimeAnimatorController = controller;
                animator.Rebind();
                controle.enabled = true;
                orbita.enabled = true;
                camera.transform.SetPositionAndRotation(posCamera, rotCamera);
                File.WriteAllText(
                    Path.Combine(pasta, "retarget.json"),
                    JsonUtility.ToJson(r, true)
                );
            }
        }

        public static async Task Executar(string pasta)
        {
            if (!RuntimeCharacterPlayground.Ativo)
                throw new InvalidOperationException("Entre no Playground em Play.");
            Directory.CreateDirectory(pasta);
            var r = new Relatorio();
            var fisicos = InputSystem
                .devices.Where(d => d.enabled && (d is Keyboard || d is Mouse))
                .ToArray();
            foreach (var d in fisicos)
            {
                if (d is Keyboard)
                    InputSystem.ResetDevice(d);
                InputSystem.DisableDevice(d);
            }
            var teclado = InputSystem.AddDevice<Keyboard>("PlaygroundLocomocaoTeclado");
            var estadoTeclado = new KeyboardState();
            Action injetar = () =>
            {
                if (InputState.currentUpdateType == InputUpdateType.Dynamic)
                    InputSystem.QueueStateEvent(teclado, estadoTeclado);
            };
            InputSystem.onBeforeUpdate += injetar;
            Application.LogCallback log = (m, p, t) =>
            {
                if (t == LogType.Warning || t == LogType.Error || t == LogType.Exception)
                    r.avisos.Add(t + ": " + m);
            };
            Application.logMessageReceived += log;
            try
            {
                var game = EditorWindow.GetWindow(
                    typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")
                );
                CharacterPlayground.Reset(0);
                game.Focus();
                await ValidacaoRuntimePlayground.Quadros(6);
                var alvo = CharacterPlayground.Atual;
                var a = alvo.GetComponentInChildren<Animator>();
                Exigir(
                    SceneManager.GetActiveScene().path == CharacterPlayground.Cena,
                    "Play permanece no Playground",
                    r
                );
                Exigir(
                    Navegacao.ChaveAtual == "personagem.playground.editor"
                        && GameObject.Find("JogoDoGrupo.Interface") == null,
                    "Abertura normal nao cobre o Playground",
                    r
                );
                Exigir(
                    a.avatar.isValid
                        && a.isHuman
                        && a.runtimeAnimatorController != null
                        && !a.applyRootMotion,
                    "Avatar controller e autoridade fisica",
                    r
                );
                foreach (
                    var caso in new[]
                    {
                        ("Idle", new Key[0], 0f),
                        ("Walking", new[] { Key.W, Key.LeftCtrl }, 2f),
                        ("Running", new[] { Key.W }, 5.5f),
                        ("Walking", new[] { Key.W, Key.LeftCtrl }, 2f),
                        ("Idle", new Key[0], 0f),
                        ("Running", new[] { Key.W }, 5.5f),
                        ("Idle", new Key[0], 0f),
                    }
                )
                {
                    estadoTeclado = new KeyboardState(caso.Item2);
                    if (alvo.transform.position.magnitude > 3f)
                    {
                        var cc = alvo.GetComponent<CharacterController>();
                        cc.enabled = false;
                        alvo.transform.position = Vector3.zero;
                        cc.enabled = true;
                    }
                    await ValidacaoRuntimePlayground.Quadros(40);
                    var clips = a.GetCurrentAnimatorClipInfo(0);
                    var predominante = clips.OrderByDescending(c => c.weight).First();
                    Exigir(
                        predominante.clip.name == caso.Item1 && predominante.weight > 0.95f,
                        "Transicao para " + caso.Item1,
                        r
                    );
                    Exigir(
                        Mathf.Abs(a.GetFloat("Velocidade") - caso.Item3) < 0.15f,
                        "Parametro em m/s: " + caso.Item1,
                        r
                    );
                    Exigir(
                        a.GetBool("NoChao") && RuntimeCharacterPlayground.Controle.NoChao,
                        "Grounded animado: " + caso.Item1,
                        r
                    );
                    r.amostras.Add(
                        $"{caso.Item1}: Velocidade={a.GetFloat("Velocidade"):F3}; peso={predominante.weight:F3}; root={alvo.transform.position:F3}"
                    );
                    ValidacaoRuntimePlayground.Capturar(pasta, "runtime-" + caso.Item1);
                }
                estadoTeclado = new KeyboardState(Key.W);
                await ValidacaoRuntimePlayground.Quadros(4);
                var mistura = a.GetCurrentAnimatorClipInfo(0);
                Exigir(
                    mistura.Count(c => c.weight > 0.01f) >= 2,
                    "Blend gradual entre velocidades",
                    r
                );
                estadoTeclado = new KeyboardState();
                CharacterPlayground.Reset(0);
                game.Focus();
                await ValidacaoRuntimePlayground.Quadros(8);
                a = CharacterPlayground.Atual.GetComponentInChildren<Animator>();
                Exigir(
                    a.GetFloat("Velocidade") < 0.01f
                        && a.GetCurrentAnimatorClipInfo(0)
                            .OrderByDescending(c => c.weight)
                            .First()
                            .clip.name == "Idle",
                    "Reset retorna Idle",
                    r
                );
                var janela = ScriptableObject.CreateInstance<JanelaCharacterPlayground>();
                try
                {
                    ValidacaoCharacterPlayground.FotografarPainel(
                        janela,
                        pasta,
                        new Vector2Int(640, 2600),
                        JogoDoGrupo.Client.Core.Visual.ConfigVisual.Tipografia.Leitura
                    );
                }
                finally
                {
                    Object.DestroyImmediate(janela);
                }
                Exigir(r.avisos.Count == 0, "Sem novos warnings ou errors", r);
                r.status = "PASSOU";
            }
            catch (Exception e)
            {
                r.status = "FALHOU";
                r.erro = e.ToString();
                throw;
            }
            finally
            {
                InputSystem.RemoveDevice(teclado);
                InputSystem.onBeforeUpdate -= injetar;
                foreach (var d in fisicos)
                    InputSystem.EnableDevice(d);
                Application.logMessageReceived -= log;
                File.WriteAllText(
                    Path.Combine(pasta, "locomocao.json"),
                    JsonUtility.ToJson(r, true)
                );
            }
        }

        static void Exigir(bool valor, string descricao, Relatorio r)
        {
            if (!valor)
                throw new InvalidOperationException(descricao);
            r.verificacoes.Add(descricao);
        }
    }
}
