using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Client.Core.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Personagem.Editor.Playground
{
    /// <summary>Validacao reproduzivel em Editor: gera assets pelo Unity e falha se um contrato quebrar.</summary>
    public static class ValidacaoCharacterPlayground
    {
        [Serializable]
        sealed class Relatorio
        {
            public string status;
            public string diagnostico;
            public string hashFbx;
            public float altura;
            public float baseDaMalha;
            public List<string> verificacoes = new List<string>();
            public List<string> avisos = new List<string>();
        }

        [MenuItem("Jogo do Grupo/Personagem/Character Playground/Validar fundacao")]
        public static void Executar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Validar fora do Play Mode.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var pasta =
                Environment.GetEnvironmentVariable("CHARACTER_PLAYGROUND_OUTPUT")
                ?? Path.Combine(Path.GetTempPath(), "character-playground04");
            Directory.CreateDirectory(pasta);
            var relatorio = new Relatorio();
            Application.LogCallback registrar = (texto, pilha, tipo) =>
            {
                if (tipo == LogType.Warning || tipo == LogType.Error || tipo == LogType.Exception)
                    relatorio.avisos.Add(tipo + ": " + texto);
            };
            Application.logMessageReceived += registrar;
            try
            {
                var originais = AssetDatabase
                    .GetDependencies(CharacterPlayground.PrefabInicial, true)
                    .SelectMany(p => new[] { p, p + ".meta" })
                    .Where(File.Exists)
                    .ToDictionary(p => p, Hash);
                var builds = string.Join(
                    "\n",
                    EditorBuildSettings.scenes.Select(c => c.path + c.enabled)
                );
                var fontes = Directory
                    .GetFiles("Assets/_Game/Core/UI/Visual/Resources/Lumora/Fontes", "*-SDF.asset")
                    .ToDictionary(p => p, HashFonte);
                var textosDasFontes = fontes.Keys.ToDictionary(p => p, File.ReadAllText);
                CharacterPlayground.Gerar();
                EditorSceneManager.OpenScene(CharacterPlayground.Cena);
                Exigir(CharacterPlayground.PodeEditar, "Cena abre diretamente e isolada");
                var antigo = CharacterPlayground.Atual;
                var primeiro = CharacterPlayground.Spawn(0);
                var segundo = CharacterPlayground.Spawn(0);
                Exigir(
                    antigo == null
                        && primeiro == null
                        && segundo != null
                        && CharacterPlayground.Raiz("PersonagemAtual").childCount == 1,
                    "Spawn repetido substitui e destroi instancias anteriores"
                );
                segundo.transform.SetPositionAndRotation(
                    new Vector3(3, 2, 1),
                    Quaternion.Euler(30, 60, 10)
                );
                CharacterPlayground.Reset(0);
                var atual = CharacterPlayground.Atual;
                var spawn = CharacterPlayground.Raiz("Spawn Point");
                Exigir(
                    segundo == null
                        && atual.transform.position == spawn.position
                        && Quaternion.Angle(atual.transform.rotation, spawn.rotation) < 0.001f
                        && atual.transform.lossyScale == Vector3.one,
                    "Reset restaura instancia, posicao, orientacao e escala"
                );
                var animator = atual.GetComponentInChildren<Animator>();
                Exigir(
                    animator != null
                        && animator.avatar != null
                        && animator.avatar.isValid
                        && animator.isHuman,
                    "Animator e Avatar Humanoid validos"
                );
                Exigir(
                    animator.runtimeAnimatorController == null,
                    "Sem dependencia de Idle ou controller local"
                );
                var bounds = CharacterPlayground.Medir(atual);
                relatorio.altura = bounds.size.y;
                relatorio.baseDaMalha = bounds.min.y;
                Exigir(
                    Mathf.Abs(bounds.size.y - 1.864161f) < 0.001f
                        && Mathf.Abs(bounds.min.y) < 0.001f,
                    "Altura 1.864161 m e pes no chao (tolerancia 1 mm)"
                );
                foreach (var nome in CharacterPlayground.Sockets)
                    Exigir(CharacterPlayground.Socket(atual, nome) != null, "Socket " + nome);
                Exigir(
                    atual
                        .GetComponentsInChildren<Renderer>()
                        .All(r =>
                            r.sharedMaterials.All(m =>
                                m != null && m.shader.name == "Universal Render Pipeline/Lit"
                            )
                        ),
                    "Material original URP Lit"
                );
                Exigir(
                    atual.GetComponentsInChildren<MonoBehaviour>().Length == 0,
                    "Instancia sem scripts de gameplay ou rede"
                );
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                try
                {
                    CharacterPlayground.Spawn(0);
                    throw new Exception("Spawn fora da cena foi aceito.");
                }
                catch (InvalidOperationException)
                {
                    Exigir(
                        SceneManager.GetActiveScene().rootCount == 0,
                        "Spawn fora do Playground recusado sem alterar cena alheia"
                    );
                }
                EditorSceneManager.OpenScene(CharacterPlayground.Cena);
                atual = CharacterPlayground.Atual;
                // Falha de catalogo nao deve destruir o personagem existente.
                try
                {
                    CharacterPlayground.Spawn(-1);
                    throw new Exception("Indice invalido foi aceito.");
                }
                catch (InvalidOperationException)
                {
                    Exigir(
                        CharacterPlayground.Atual == atual,
                        "Entrada invalida preserva instancia atual"
                    );
                }
                var camera = CharacterPlayground.Raiz("Camera de inspecao").GetComponent<Camera>();
                for (var i = 0; i < 4; i++)
                {
                    CharacterPlayground.Inspecionar(i, 1f);
                    Fotografar(
                        camera,
                        pasta,
                        "personagem-" + new[] { "frente", "direita", "costas", "esquerda" }[i],
                        1280,
                        1440
                    );
                }
                CharacterPlayground.Inspecionar(0, 2f);
                Exigir(
                    Vector3.Distance(camera.transform.position, bounds.center)
                        < bounds.size.y * 1.8f,
                    "Zoom aproxima a camera"
                );
                camera.transform.position = new Vector3(8, 7, 9);
                camera.transform.LookAt(new Vector3(0, 0.5f, 1.5f));
                Fotografar(camera, pasta, "cena-geral", 1920, 1080);
                CharacterPlayground.Reset(0);
                var janela = ScriptableObject.CreateInstance<JanelaCharacterPlayground>();
                try
                {
                    foreach (
                        var tamanho in new[]
                        {
                            new Vector2Int(640, 720),
                            new Vector2Int(960, 720),
                            new Vector2Int(1280, 720),
                            new Vector2Int(1920, 1080),
                            new Vector2Int(2560, 1440),
                            new Vector2Int(640, 1600),
                        }
                    )
                        FotografarPainel(janela, pasta, tamanho, ConfigVisual.Tipografia.Leitura);
                    FotografarPainel(
                        janela,
                        pasta,
                        new Vector2Int(640, 720),
                        ConfigVisual.Tipografia.LeituraMaxima
                    );
                }
                finally
                {
                    Object.DestroyImmediate(janela);
                }
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                EditorSceneManager.OpenScene(CharacterPlayground.Cena);
                Exigir(
                    CharacterPlayground.Atual != null
                        && CharacterPlayground.Raiz("PersonagemAtual").childCount == 1,
                    "Cena reaberta preserva unica instancia"
                );
                var dependencias = AssetDatabase
                    .GetDependencies(CharacterPlayground.Cena, true)
                    .Concat(AssetDatabase.GetDependencies(CharacterPlayground.Catalogo, true))
                    .ToArray();
                Exigir(
                    !dependencias.Any(p =>
                        p.Contains("ValidacaoLocal")
                        || p.Contains("OldMan")
                        || p.Contains("ThirdParty")
                    ),
                    "Sem assets externos ou animacao local nas dependencias"
                );
                Exigir(
                    !EditorBuildSettings.scenes.Any(c => c.path == CharacterPlayground.Cena)
                        && builds
                            == string.Join(
                                "\n",
                                EditorBuildSettings.scenes.Select(c => c.path + c.enabled)
                            ),
                    "Build settings preservados e cena fora do build"
                );
                Exigir(
                    originais.All(p => Hash(p.Key) == p.Value),
                    "Prefab, FBX, Avatar, materiais e texturas originais intactos por SHA256"
                );
                relatorio.hashFbx = originais.First(p => p.Key.EndsWith(".fbx")).Value;
                relatorio.diagnostico = JanelaCharacterPlayground.Diagnosticar();
                AssetDatabase.SaveAssets();
                foreach (var par in fontes.Where(p => HashFonte(p.Key) != p.Value))
                {
                    File.WriteAllText(
                        Path.Combine(pasta, Path.GetFileName(par.Key) + ".antes.txt"),
                        textosDasFontes[par.Key]
                    );
                    File.WriteAllText(
                        Path.Combine(pasta, Path.GetFileName(par.Key) + ".depois.txt"),
                        File.ReadAllText(par.Key)
                    );
                }
                Exigir(
                    fontes.All(p => HashFonte(p.Key) == p.Value),
                    "Captura nao grava cache nas fontes compartilhadas de Core"
                );
                relatorio.status = "PASSOU";
            }
            finally
            {
                Application.logMessageReceived -= registrar;
                File.WriteAllText(
                    Path.Combine(pasta, "validacao-playground.json"),
                    JsonUtility.ToJson(relatorio, true)
                );
            }
            void Exigir(bool passou, string descricao)
            {
                if (!passou)
                    throw new InvalidOperationException("Playground: " + descricao);
                relatorio.verificacoes.Add(descricao);
            }
        }

        static string Hash(string caminho)
        {
            using var sha = SHA256.Create();
            return BitConverter
                .ToString(sha.ComputeHash(File.ReadAllBytes(caminho)))
                .Replace("-", "")
                .ToLowerInvariant();
        }

        static string HashFonte(string caminho)
        {
            // Unity normaliza CRLF ao salvar YAML; o contrato protege os dados dos glifos.
            using var sha = SHA256.Create();
            return BitConverter.ToString(
                sha.ComputeHash(
                    System.Text.Encoding.UTF8.GetBytes(
                        File.ReadAllText(caminho).Replace("\r\n", "\n")
                    )
                )
            );
        }

        static void Fotografar(Camera camera, string pasta, string nome, int largura, int altura)
        {
            var rt = RenderTexture.GetTemporary(largura, altura, 24);
            var anterior = camera.targetTexture;
            try
            {
                camera.targetTexture = rt;
                camera.Render();
                camera.Render();
                Gravar(rt, Path.Combine(pasta, nome + ".png"));
            }
            finally
            {
                camera.targetTexture = anterior;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        static void FotografarPainel(
            JanelaCharacterPlayground janela,
            string pasta,
            Vector2Int tamanho,
            int leitura
        )
        {
            var configuracao = EstiloCore.CriarConfiguracaoDoPainel("CapturaPlayground", 0f);
            configuracao.scaleMode = PanelScaleMode.ConstantPixelSize;
            var destino = new RenderTexture(tamanho.x, tamanho.y, 0);
            destino.Create();
            configuracao.targetTexture = destino;
            var objeto = new GameObject("Captura temporaria do painel")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            objeto.SetActive(false);
            var documento = objeto.AddComponent<UIDocument>();
            documento.panelSettings = configuracao;
            objeto.SetActive(true);
            try
            {
                janela.Montar(documento.rootVisualElement);
                EstiloVisual.DefinirTamanhoTexto(documento.rootVisualElement, leitura);
                var tipo = typeof(UIDocument).Assembly.GetType(
                    "UnityEngine.UIElements.UIElementsRuntimeUtility"
                );
                foreach (
                    var nome in new[] { "UpdatePanels", "RepaintPanels", "RenderOffscreenPanels" }
                )
                {
                    var parametros =
                        nome == "RepaintPanels" ? new object[] { true } : Array.Empty<object>();
                    var metodo = tipo.GetMethod(
                        nome,
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                        null,
                        parametros.Select(p => p.GetType()).ToArray(),
                        null
                    );
                    if (metodo == null)
                        throw new InvalidOperationException(
                            "Captura UI Toolkit indisponivel: " + nome
                        );
                    metodo.Invoke(null, parametros);
                }
                Gravar(
                    destino,
                    Path.Combine(pasta, $"painel-{tamanho.x}x{tamanho.y}-texto{leitura}.png")
                );
            }
            finally
            {
                Object.DestroyImmediate(objeto);
                destino.Release();
                Object.DestroyImmediate(destino);
                Object.DestroyImmediate(configuracao);
            }
        }

        static void Gravar(RenderTexture rt, string caminho)
        {
            var anterior = RenderTexture.active;
            var imagem = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = rt;
                imagem.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                imagem.Apply();
                File.WriteAllBytes(caminho, imagem.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = anterior;
                Object.DestroyImmediate(imagem);
            }
        }
    }
}
