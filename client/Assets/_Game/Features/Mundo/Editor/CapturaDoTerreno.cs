using System.Diagnostics;
using System.IO;
using System.Linq;
using JogoDoGrupo.Client.Features.Mundo.Hub;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>
    /// Monta o hub numa cena nova e fotografa o chão no campo, na neve, na lava e no pântano, ao
    /// nível do personagem (1,7 m) e em vista elevada. Salva a folha em <c>%TEMP%</c> como
    /// <c>terreno-&lt;TERRENO_TAG&gt;.png</c> e escreve no log o tempo de montagem e as contagens
    /// de objetos e materiais (para o antes/depois dos PRs). Com o editor fechado:
    /// <c>npm run unity:executar -- JogoDoGrupo.Client.Features.Mundo.Editor.CapturaDoTerreno.Capturar</c>.
    /// </summary>
    public static class CapturaDoTerreno
    {
        private const int Largura = 640;
        private const int Altura = 360;

        public static void Capturar()
        {
            var tag = System.Environment.GetEnvironmentVariable("TERRENO_TAG");
            if (string.IsNullOrEmpty(tag))
            {
                tag = "captura";
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var relogio = Stopwatch.StartNew();
            var hub = ConstrutorHub.Montar();
            relogio.Stop();
            Relatar(hub, relogio.ElapsedMilliseconds);

            var locais = new[]
            {
                ("campo", Achar(Bioma.Campo, 0.98f, 1f, 8f, false)),
                ("neve", Achar(Bioma.Neve, 0.98f, 10f, 60f, false)),
                ("lava", Achar(Bioma.Lava, 0.8f, -5f, 0f, true)),
                ("pantano", Achar(Bioma.Pantano, 0.8f, -5f, -0.4f, true)),
            };

            var folha = new Texture2D(Largura * 4, Altura * 2, TextureFormat.RGB24, false);
            for (var k = 0; k < locais.Length; k++)
            {
                var (nome, ponto) = locais[k];
                Debug.Log($"[CapturaDoTerreno] {nome} em {ponto}");
                Colar(folha, Fotografar(ponto, 22f, 1.7f, 0f), k, 1);
                Colar(folha, Fotografar(ponto, 45f, 40f, 25f), k, 0);
            }

            var caminho = Path.Combine(Path.GetTempPath(), $"terreno-{tag}.png");
            File.WriteAllBytes(caminho, folha.EncodeToPNG());
            Debug.Log($"[CapturaDoTerreno] salvo em {caminho}");
        }

        private static void Relatar(GameObject hub, long milissegundos)
        {
            var renderizadores = hub.GetComponentsInChildren<Renderer>(true);
            var materiais = renderizadores
                .SelectMany(r => r.sharedMaterials)
                .Where(m => m != null)
                .Distinct()
                .Count();
            var objetos = hub.GetComponentsInChildren<Transform>(true).Length;
            var colisores = hub.GetComponentsInChildren<Collider>(true).Length;
            var vegetacao = hub.GetComponentInChildren<BlocosDeVegetacao>(true);
            if (vegetacao != null)
            {
                var origem = new Vector3(450f, 5f, 450f);
                Debug.Log(
                    $"[CapturaDoTerreno] vegetacao: {vegetacao.Instancias} instancias em "
                        + $"{vegetacao.Blocos} blocos, {vegetacao.MateriaisDistintos} materiais, "
                        + $"{vegetacao.GruposTotais} grupos; chamadas de desenho a partir de "
                        + $"{origem}: {vegetacao.ContarChamadas(origem)}"
                );
            }

            Debug.Log(
                $"[CapturaDoTerreno] montagem sincrona {milissegundos} ms; objetos {objetos}; "
                    + $"renderizadores {renderizadores.Length}; materiais distintos {materiais}; "
                    + $"colisores {colisores}"
            );
        }

        // Primeiro ponto do bioma (peso mínimo) cuja altura cai na faixa pedida. Nos líquidos,
        // devolve um ponto com chão abaixo do nível, onde a malha do líquido existe.
        private static Vector3 Achar(
            Bioma bioma,
            float pesoMinimo,
            float alturaMin,
            float alturaMax,
            bool preferirBorda
        )
        {
            var melhor = Vector3.zero;
            var achou = false;
            for (var z = -MapaDoMundo.Metade + 60f; z < MapaDoMundo.Metade - 60f; z += 20f)
            {
                for (var x = -MapaDoMundo.Metade + 60f; x < MapaDoMundo.Metade - 60f; x += 20f)
                {
                    var pesos = MapaDoMundo.PesosEm(x, z);
                    if (pesos.De(bioma) < pesoMinimo)
                    {
                        continue;
                    }

                    var altura = MapaDoMundo.AlturaEm(x, z);
                    if (altura < alturaMin || altura > alturaMax)
                    {
                        continue;
                    }

                    // Líquido: só vale onde há uma extensão larga (sem paredão em volta).
                    if (preferirBorda && !Largo(x, z, alturaMax))
                    {
                        continue;
                    }

                    // Perto do centro do mundo é mais fácil de ver; quem vem primeiro ao
                    // varrer de sul para norte não importa, só o que cai na faixa.
                    var candidato = new Vector3(x, altura, z);
                    if (!achou || candidato.sqrMagnitude < melhor.sqrMagnitude)
                    {
                        melhor = candidato;
                        achou = true;
                    }
                }
            }

            if (!achou)
            {
                Debug.LogWarning($"[CapturaDoTerreno] Sem ponto em {bioma}.");
            }

            return melhor;
        }

        private static bool Largo(float x, float z, float alturaMax)
        {
            const float raio = 12f;
            return MapaDoMundo.AlturaEm(x + raio, z) <= alturaMax
                && MapaDoMundo.AlturaEm(x - raio, z) <= alturaMax
                && MapaDoMundo.AlturaEm(x, z + raio) <= alturaMax
                && MapaDoMundo.AlturaEm(x, z - raio) <= alturaMax;
        }

        private static void Colar(Texture2D folha, Texture2D imagem, int coluna, int linha)
        {
            folha.SetPixels(coluna * Largura, linha * Altura, Largura, Altura, imagem.GetPixels());
            Object.DestroyImmediate(imagem);
        }

        private static Texture2D Fotografar(
            Vector3 alvo,
            float distancia,
            float altura,
            float inclinacao
        )
        {
            var objeto = new GameObject("CameraDeCaptura");
            var camera = objeto.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.ambientSkyColor;
            camera.farClipPlane = 500f;
            var nivel = Mathf.Max(alvo.y, MapaDoMundo.NivelDaAgua);
            objeto.transform.position = new Vector3(alvo.x, nivel + altura, alvo.z - distancia);
            objeto.transform.rotation = Quaternion.Euler(inclinacao, 0f, 0f);
            // A cena de captura não roda o loop de jogo: o desenho da vegetação é pedido aqui.
            foreach (
                var blocos in Object.FindObjectsByType<BlocosDeVegetacao>(FindObjectsSortMode.None)
            )
            {
                blocos.Desenhar(objeto.transform.position, camera);
            }

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
