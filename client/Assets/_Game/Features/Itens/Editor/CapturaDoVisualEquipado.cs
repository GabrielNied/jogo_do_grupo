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
                Debug.Log($"[VisualEquipado] modelos postos: {postos}");
                foreach (var arma in jogador.GetComponentsInChildren<ArmaNoAvatar>())
                {
                    Debug.Log(
                        $"[VisualEquipado] {arma.name} em {arma.transform.parent.name}: "
                            + $"mundo {arma.transform.position} para cima {arma.transform.up}"
                    );
                }

                Foto(camera, jogador.transform, 0f, Path.Combine(saida, "visual-frente.png"));
                Foto(camera, jogador.transform, 180f, Path.Combine(saida, "visual-costas.png"));
                Foto(camera, jogador.transform, 90f, Path.Combine(saida, "visual-lado.png"));
            }
            finally
            {
                Object.DestroyImmediate(jogador);
                Object.DestroyImmediate(luz.gameObject);
                Object.DestroyImmediate(camera.gameObject);
            }
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
