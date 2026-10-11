using System.IO;
using JogoDoGrupo.Client.Features.Mundo.Hub;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>
    /// Monta o hub numa cena nova, põe o personagem e as criaturas em campo aberto e salva uma
    /// folha de imagens (a mesma cena vista de quatro lados, para ver o sol de frente, de lado e
    /// de costas) em <c>%TEMP%</c>. Serve para conferir a iluminação dos modelos. A variável de
    /// ambiente <c>LUZ_TAG</c> dá o nome do arquivo (ex.: antes, depois).
    /// Com o editor fechado: <c>npm run unity:executar -- JogoDoGrupo.Client.Features.Mundo.Editor.CapturaDaIluminacao.Capturar</c>.
    /// </summary>
    public static class CapturaDaIluminacao
    {
        private const int Largura = 800;
        private const int Altura = 450;
        private const float DistanciaDaCamera = 10f;
        private const float AlturaDaCamera = 1.7f;
        private const float AlturaDoAlvo = 1.0f;

        // Campo aberto ao sul do hub, longe das casas.
        private static readonly Vector3 Centro = new Vector3(30f, 0f, -60f);

        private static readonly string[] Modelos =
        {
            "JavaliDoCampo",
            "PresaVelha",
            "GuardiaoDoReflexo",
        };

        private static readonly float[] Posicoes = { 1.8f, 3.6f, 7.5f };

        public static void Capturar()
        {
            var tag = System.Environment.GetEnvironmentVariable("LUZ_TAG");
            if (string.IsNullOrEmpty(tag))
            {
                tag = "captura";
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConstrutorHub.Montar();
            Colocar();

            var folha = new Texture2D(Largura * 2, Altura * 2, TextureFormat.RGB24, false);
            for (var i = 0; i < 4; i++)
            {
                var imagem = Fotografar(i * 90f);
                folha.SetPixels(
                    (i % 2) * Largura,
                    (1 - i / 2) * Altura,
                    Largura,
                    Altura,
                    imagem.GetPixels()
                );
                Object.DestroyImmediate(imagem);
            }

            var caminho = Path.Combine(Path.GetTempPath(), $"luz-{tag}.png");
            File.WriteAllBytes(caminho, folha.EncodeToPNG());
            Debug.Log($"[CapturaDaIluminacao] salvo em {caminho}");
        }

        private static void Colocar()
        {
            var jogador = Resources.Load<GameObject>("Personagem/Jogador_Valdren");
            if (jogador != null)
            {
                var j = (GameObject)Object.Instantiate(jogador, Centro, Quaternion.identity);
                j.name = "CapturaJogador";
            }

            for (var i = 0; i < Modelos.Length; i++)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/_Game/Features/Mobs/Art/Modelos/{Modelos[i]}.fbx"
                );
                if (fbx == null)
                {
                    Debug.LogWarning($"Sem modelo {Modelos[i]}");
                    continue;
                }

                var o = (GameObject)
                    Object.Instantiate(
                        fbx,
                        Centro + new Vector3(Posicoes[i], 0f, 0f),
                        Quaternion.Euler(0f, 180f, 0f)
                    );
                o.name = "Captura" + Modelos[i];
                var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    $"Assets/_Game/Features/Mobs/Art/Modelos/{Modelos[i]}_Cor.png"
                );
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetTexture("_BaseMap", textura);
                material.SetFloat("_Smoothness", 0f);
                foreach (var r in o.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterial = material;
                }
            }
        }

        private static Texture2D Fotografar(float azimute)
        {
            var alvo = Centro + new Vector3(3.5f, AlturaDoAlvo, 0f);
            var objeto = new GameObject("CameraDeCaptura");
            var camera = objeto.AddComponent<Camera>();
            camera.fieldOfView = 45f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.ambientSkyColor;
            camera.farClipPlane = 500f;
            var direcao = Quaternion.Euler(0f, azimute + 30f, 0f) * Vector3.back;
            objeto.transform.position =
                alvo + direcao * DistanciaDaCamera + Vector3.up * (AlturaDaCamera - AlturaDoAlvo);
            objeto.transform.LookAt(alvo);

            var rt = RenderTexture.GetTemporary(Largura, Altura, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var imagem = new Texture2D(Largura, Altura, TextureFormat.RGB24, false);
            imagem.ReadPixels(new Rect(0, 0, Largura, Altura), 0, 0);
            imagem.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(objeto);
            return imagem;
        }
    }
}
