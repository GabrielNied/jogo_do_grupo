using System.IO;
using JogoDoGrupo.Client.Features.Itens.Visual;
using JogoDoGrupo.Shared.Features.Itens;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens.Editor
{
    /// <summary>
    /// Confere sem o jogo rodando como a arma fica no personagem: monta o prefab do Valdren com
    /// uma arma na mão e outra nas costas e grava um PNG de frente e de costas. Uso (editor
    /// fechado): <c>npm run unity:executar -- JogoDoGrupo.Client.Features.Itens.Editor.CapturaDoVisualEquipado.Capturar</c>;
    /// <c>VISUAL_SAIDA</c> troca a pasta (padrão <c>prints/</c>).
    /// </summary>
    public static class CapturaDoVisualEquipado
    {
        private const string Prefab =
            "Assets/_Game/Features/Personagem/Resources/Personagem/Jogador_Valdren.prefab";

        public static void Capturar()
        {
            var saida = System.Environment.GetEnvironmentVariable("VISUAL_SAIDA") ?? "prints";
            Directory.CreateDirectory(saida);
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            var jogador = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            var luz = new GameObject("Luz").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.3f, 0.35f, 0.4f);
            try
            {
                var visual = new VisualEquipado
                {
                    PersonagemId = 1,
                    ConjuntoAtivo = 1,
                    Armas = new[]
                    {
                        new ArmaVisual
                        {
                            Posicao = PosicaoEquipamento.Arma1,
                            Tipo = TipoEquipamento.Espada,
                        },
                        new ArmaVisual
                        {
                            Posicao = PosicaoEquipamento.Arma2,
                            Tipo = TipoEquipamento.Arco,
                        },
                    },
                };
                var postos = ApresentadorDoVisual.Aplicar(jogador.transform, visual);
                var logBuffer = new System.Text.StringBuilder();
                logBuffer.AppendLine($"[VisualEquipado] modelos postos: {postos}");

                // Pega os clipes do FBX
                var assets = AssetDatabase.LoadAllAssetsAtPath(
                    Prefab.Replace(
                        "Resources/Personagem/Jogador_Valdren.prefab",
                        "Art/Modelos/Valdren.fbx"
                    )
                );
                AnimationClip atacarClip = null;
                AnimationClip idleClip = null;
                foreach (var a in assets)
                {
                    if (a is AnimationClip clip)
                    {
                        if (clip.name == "Atacar")
                            atacarClip = clip;
                        if (clip.name == "Idle")
                            idleClip = clip;
                    }
                }

                var alvoAnimacao = jogador.transform.Find("Valdren")?.gameObject ?? jogador;
                if (idleClip != null)
                {
                    idleClip.SampleAnimation(alvoAnimacao, 0f);
                    foreach (var t in jogador.GetComponentsInChildren<Transform>())
                    {
                        if (
                            t.name == "Weapon.R"
                            || t.name == "EncaixeMaoDireita"
                            || t.name == "Costas"
                            || t.name == "EncaixeCostas"
                        )
                        {
                            logBuffer.AppendLine(
                                $"[Idle 0s] {t.name}: worldPos={t.position}, worldFwd={t.forward}, worldUp={t.up}"
                            );
                        }
                    }
                }

                // Captura padrão (Espada na mão, Arco nas costas)
                var visualPadrao = new VisualEquipado
                {
                    PersonagemId = 1,
                    ConjuntoAtivo = 1,
                    Armas = new[]
                    {
                        new ArmaVisual
                        {
                            Posicao = PosicaoEquipamento.Arma1,
                            Tipo = TipoEquipamento.Espada,
                        },
                        new ArmaVisual
                        {
                            Posicao = PosicaoEquipamento.Arma2,
                            Tipo = TipoEquipamento.Arco,
                        },
                    },
                };
                ApresentadorDoVisual.Aplicar(jogador.transform, visualPadrao);
                logBuffer.AppendLine(Diagnostico(jogador.transform));
                Debug.Log(logBuffer.ToString());
                Foto(camera, jogador.transform, 0f, Path.Combine(saida, "visual-frente.png"));
                Foto(camera, jogador.transform, 90f, Path.Combine(saida, "visual-lado.png"));
                Foto(camera, jogador.transform, 180f, Path.Combine(saida, "visual-costas.png"));

                // Catálogo completo de armas para validação visual de cada tipo
                TipoEquipamento[] armas =
                {
                    TipoEquipamento.Espada,
                    TipoEquipamento.Martelo,
                    TipoEquipamento.Arco,
                    TipoEquipamento.Cajado,
                    TipoEquipamento.Escudo,
                };
                for (int i = 0; i < armas.Length; i++)
                {
                    var ativa = armas[i];
                    var inativa = armas[(i + 1) % armas.Length];
                    var visualArmas = new VisualEquipado
                    {
                        PersonagemId = 1,
                        ConjuntoAtivo = 1,
                        Armas = new[]
                        {
                            new ArmaVisual { Posicao = PosicaoEquipamento.Arma1, Tipo = ativa },
                            new ArmaVisual { Posicao = PosicaoEquipamento.Arma2, Tipo = inativa },
                        },
                    };
                    ApresentadorDoVisual.Aplicar(jogador.transform, visualArmas);
                    Foto(
                        camera,
                        jogador.transform,
                        0f,
                        Path.Combine(saida, $"teste-{ativa}-frente.png")
                    );
                    Foto(
                        camera,
                        jogador.transform,
                        90f,
                        Path.Combine(saida, $"teste-{ativa}-lado.png")
                    );
                    Foto(
                        camera,
                        jogador.transform,
                        180f,
                        Path.Combine(saida, $"teste-{ativa}-costas.png")
                    );
                }
            }
            finally
            {
                Object.DestroyImmediate(jogador);
                Object.DestroyImmediate(luz.gameObject);
                Object.DestroyImmediate(camera.gameObject);
            }
        }

        private static string Diagnostico(Transform jogador)
        {
            var texto = new System.Text.StringBuilder("[VisualEquipado] armas no avatar:");
            foreach (var arma in jogador.GetComponentsInChildren<ArmaNoAvatar>(true))
            {
                texto
                    .AppendLine()
                    .Append("  ")
                    .Append(arma.name)
                    .Append(" em ")
                    .Append(arma.transform.position.ToString("F2"))
                    .Append(" escala do mundo ")
                    .Append(arma.transform.lossyScale.ToString("F3"))
                    .Append(" pai ")
                    .Append(
                        arma.transform.parent != null
                            ? arma.transform.parent.name
                                + " "
                                + arma.transform.parent.lossyScale.ToString("F3")
                            : "nenhum"
                    );
                foreach (var r in arma.GetComponentsInChildren<Renderer>(true))
                {
                    texto
                        .AppendLine()
                        .Append("    ")
                        .Append(r.name)
                        .Append(" ativo=")
                        .Append(r.gameObject.activeInHierarchy)
                        .Append(" limites ")
                        .Append(r.bounds.center.ToString("F2"))
                        .Append(" tam ")
                        .Append(r.bounds.size.ToString("F2"));
                }
            }

            return texto.ToString();
        }

        private static void Foto(Camera camera, Transform alvo, float graus, string arquivo)
        {
            var centro = alvo.position + Vector3.up * 1f;
            var direcao = Quaternion.Euler(0f, graus, 0f) * Vector3.forward;
            camera.transform.position = centro + direcao * 3.2f;
            camera.transform.LookAt(centro);
            var textura = new RenderTexture(900, 900, 24);
            camera.targetTexture = textura;
            camera.Render();
            RenderTexture.active = textura;
            var imagem = new Texture2D(900, 900, TextureFormat.RGB24, false);
            imagem.ReadPixels(new Rect(0, 0, 900, 900), 0, 0);
            File.WriteAllBytes(arquivo, imagem.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(textura);
            Object.DestroyImmediate(imagem);
            Debug.Log($"[VisualEquipado] gravado {Path.GetFullPath(arquivo)}");
        }
    }
}
