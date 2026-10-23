using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Ensaio de material em memoria, sem cena salva ou entrada de menu.</summary>
    public static class ValidacaoDasEncostas
    {
        public static void Executar()
        {
            var pasta = "Assets/__ValidacaoEncostas_" + Guid.NewGuid().ToString("N");
            var cena = EditorSceneManager.NewPreviewScene();
            var ativo = RenderTexture.active;
            Material nativo = null;
            Texture2D imagem = null;
            RenderTexture render = null;
            try
            {
                AssetDatabase.CreateFolder("Assets", pasta.Substring("Assets/".Length));
                var camadas = Enumerable.Range(0, 5).Select(i => CriarCamada(pasta, i)).ToArray();
                var dados = new TerrainData
                {
                    heightmapResolution = 33,
                    alphamapResolution = 32,
                    baseMapResolution = 32,
                    size = new Vector3(16, 32, 16),
                    terrainLayers = camadas,
                };
                var alturas = new float[33, 33];
                for (var z = 0; z < 33; z++)
                for (var x = 0; x < 33; x++)
                    alturas[z, x] = x / 32f * 0.9f;
                dados.SetHeights(0, 0, alturas);
                AssetDatabase.CreateAsset(dados, pasta + "/terreno.asset");
                var objeto = Terrain.CreateTerrainGameObject(dados);
                SceneManager.MoveGameObjectToScene(objeto, cena);
                var terreno = objeto.GetComponent<Terrain>();
                terreno.drawInstanced = true;
                terreno.basemapDistance = 200;
                nativo = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
                terreno.materialTemplate = nativo;
                var luz = new GameObject("Luz temporaria");
                SceneManager.MoveGameObjectToScene(luz, cena);
                luz.transform.rotation = Quaternion.Euler(40, -35, 0);
                var direcional = luz.AddComponent<Light>();
                direcional.type = LightType.Directional;
                direcional.intensity = 1;
                var objetoCamera = new GameObject("Camera temporaria");
                SceneManager.MoveGameObjectToScene(objetoCamera, cena);
                var camera = objetoCamera.AddComponent<Camera>();
                camera.scene = cena;
                camera.transform.position = new Vector3(-22, 20, -12);
                camera.transform.LookAt(new Vector3(8, 13, 8));
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.1f, 0.15f, 0.2f);
                render = RenderTexture.GetTemporary(320, 240, 24, RenderTextureFormat.ARGB32);
                imagem = new Texture2D(320, 240, TextureFormat.RGB24, false);

                Pintar(dados, 0.7f);
                var referencia = Capturar(camera, terreno, render, imagem);
                var material = MaterialDeEncostas.Criar(pasta + "/encostas.mat", camadas[0]);
                MaterialDeEncostas.Aplicar(new[] { terreno }, material, camadas[0]);
                Verificar(
                    AssetDatabase.Contains(terreno.materialTemplate),
                    "Material do Terrain nao e persistente."
                );
                var triplanar = Capturar(camera, terreno, render, imagem);
                var diferenca = Diferenca(referencia, triplanar);
                Verificar(
                    diferenca > 0.001f,
                    "A camada selecionada nao mudou a projecao no paredao."
                );

                // A quinta camada passa pelo AddPass nativo, sem projecao ou cor perdidas.
                Pintar(dados, 0);
                var adicional = Capturar(camera, terreno, render, imagem);
                var materialAplicado = terreno.materialTemplate;
                terreno.materialTemplate = nativo;
                var adicionalNativo = Capturar(camera, terreno, render, imagem);
                Verificar(
                    Diferenca(adicional, adicionalNativo) < 0.025f,
                    "Camada adicional divergiu do Terrain nativo."
                );
                terreno.materialTemplate = materialAplicado;

                // Depois da distancia do basemap, ambos devem usar a mesma projecao nativa.
                Pintar(dados, 0.7f);
                terreno.basemapDistance = 1;
                var distante = Capturar(camera, terreno, render, imagem);
                terreno.materialTemplate = nativo;
                var distanteNativo = Capturar(camera, terreno, render, imagem);
                Verificar(
                    Diferenca(distante, distanteNativo) < 0.025f,
                    "Basemap distante divergiu do nativo."
                );
                var shader = Shader.Find(MaterialDeEncostas.NomeDoShader);
                Verificar(
                    shader != null && !ShaderUtil.ShaderHasError(shader),
                    "Shader de encostas apresentou erro."
                );
                Debug.Log(
                    "[Encostas] material renderizado: projecao rochosa, normal, quinta camada e basemap conferidos; temporarios removidos ao concluir."
                );
            }
            finally
            {
                RenderTexture.active = ativo;
                EditorSceneManager.ClosePreviewScene(cena);
                if (render != null)
                    RenderTexture.ReleaseTemporary(render);
                if (imagem != null)
                    Object.DestroyImmediate(imagem);
                if (nativo != null)
                    Object.DestroyImmediate(nativo);
                if (AssetDatabase.IsValidFolder(pasta))
                    AssetDatabase.DeleteAsset(pasta);
            }
        }

        private static TerrainLayer CriarCamada(string pasta, int indice)
        {
            var textura = new Texture2D(16, 16, TextureFormat.RGBA32, true);
            for (var y = 0; y < 16; y++)
            for (var x = 0; x < 16; x++)
                textura.SetPixel(
                    x,
                    y,
                    indice == 0
                        ? (
                            (x / 4 + y / 4) % 2 == 0
                                ? new Color(0.7f, 0.6f, 0.35f)
                                : new Color(0.2f, 0.3f, 0.4f)
                        )
                        : new Color(0.1f + indice * 0.12f, 0.3f, 0.1f)
                );
            textura.Apply();
            AssetDatabase.CreateAsset(textura, pasta + "/cor" + indice + ".asset");
            var normal = new Texture2D(4, 4, TextureFormat.RGB24, false);
            for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
                normal.SetPixel(x, y, new Color(0.58f, 0.42f, 1));
            normal.Apply();
            var caminho = pasta + "/normal" + indice + ".png";
            try
            {
                File.WriteAllBytes(caminho, normal.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(normal);
            }
            AssetDatabase.ImportAsset(caminho);
            var importador = (TextureImporter)AssetImporter.GetAtPath(caminho);
            importador.textureType = TextureImporterType.NormalMap;
            importador.SaveAndReimport();
            var camada = new TerrainLayer
            {
                diffuseTexture = textura,
                normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(caminho),
                normalScale = 0.6f,
                tileSize = new Vector2(3, 3),
                smoothness = 0.1f,
            };
            AssetDatabase.CreateAsset(camada, pasta + "/camada" + indice + ".terrainlayer");
            return camada;
        }

        private static void Pintar(TerrainData dados, float rocha)
        {
            var pesos = new float[32, 32, 5];
            for (var z = 0; z < 32; z++)
            for (var x = 0; x < 32; x++)
            {
                pesos[z, x, 0] = rocha;
                pesos[z, x, 4] = 1 - rocha;
            }
            dados.SetAlphamaps(0, 0, pesos);
            dados.SetBaseMapDirty();
        }

        private static Color32[] Capturar(
            Camera camera,
            Terrain terreno,
            RenderTexture render,
            Texture2D imagem
        )
        {
            terreno.Flush();
            camera.targetTexture = render;
            var pedido = new RenderPipeline.StandardRequest { destination = render };
            for (var i = 0; i < 2; i++)
                if (RenderPipeline.SupportsRenderRequest(camera, pedido))
                    RenderPipeline.SubmitRenderRequest(camera, pedido);
                else
                    camera.Render();
            RenderTexture.active = render;
            imagem.ReadPixels(new Rect(0, 0, 320, 240), 0, 0);
            imagem.Apply();
            var pixels = imagem.GetPixels32();
            Verificar(
                pixels.Count(p => p.r > 200 && p.b > 200 && p.g < 50) < pixels.Length / 100,
                "Material magenta: shader nao renderizou corretamente."
            );
            return pixels;
        }

        private static float Diferenca(Color32[] a, Color32[] b)
        {
            var soma = 0f;
            for (var i = 0; i < a.Length; i++)
                soma +=
                    Math.Abs(a[i].r - b[i].r)
                    + Math.Abs(a[i].g - b[i].g)
                    + Math.Abs(a[i].b - b[i].b);
            return soma / (a.Length * 3f * 255f);
        }

        private static void Verificar(bool condicao, string mensagem)
        {
            if (!condicao)
                throw new InvalidOperationException(mensagem);
        }
    }
}
