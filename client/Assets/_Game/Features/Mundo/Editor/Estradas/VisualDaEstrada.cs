using System;
using System.Collections.Generic;
using System.IO;
using JogoDoGrupo.Client.Features.Mundo.Estradas;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace JogoDoGrupo.Client.Features.Mundo.Editor.Estradas
{
    /// <summary>Gera somente a apresentação; o Terrain continua sendo o chão físico.</summary>
    public static class VisualDaEstrada
    {
        private const string NomeDaSuperficie = "SuperficieEstrada";
        private const string NomeDosPedregulhos = "PedregulhosEstrada";
        private const string NomeDoShader = "JogoDoGrupo/Mundo/EstradaNativa";
        private const string TexturaDaTerra =
            "Assets/ThirdParty/PolyHaven/Resources/Mundo/Estrada/dirt_diff_2k.jpg";
        private const int FaixasDaBorda = 8;
        private const int FaixasDoLeito = 8;
        private const int FaixasTransversais = FaixasDaBorda * 2 + FaixasDoLeito;
        private const float FolgaVertical = 0.015f;
        public const int LimiteDePedregulhos = 8192;

        public static void Gerar(
            EstradaNativa estrada,
            IReadOnlyList<Vector3> pontos,
            Func<Vector3, float> alturaEm,
            string pastaAssets
        )
        {
            if (pontos == null || pontos.Count < 2)
            {
                throw new ArgumentException("A superfície precisa de pelo menos dois pontos.");
            }

            GarantirPasta(pastaAssets);
            var material =
                estrada.Material != null
                    ? estrada.Material
                    : ObterMaterial(estrada, pastaAssets + "/Estrada.mat", false);
            var malha = PersistirMalha(
                CriarSuperficie(estrada, pontos, alturaEm),
                pastaAssets + "/Superficie.asset"
            );
            AtualizarObjeto(estrada, NomeDaSuperficie, malha, material);

            if (estrada.Pedregulhos && estrada.DensidadeCascalhos > 0f)
            {
                var pedras = PersistirMalha(
                    CriarPedregulhos(estrada, pontos, alturaEm),
                    pastaAssets + "/Pedregulhos.asset"
                );
                AtualizarObjeto(
                    estrada,
                    NomeDosPedregulhos,
                    pedras,
                    ObterMaterial(estrada, pastaAssets + "/Pedregulhos.mat", true)
                );
            }
            else
            {
                RemoverFilho(estrada, NomeDosPedregulhos);
            }
        }

        public static void Remover(EstradaNativa estrada)
        {
            RemoverFilho(estrada, NomeDaSuperficie);
            RemoverFilho(estrada, NomeDosPedregulhos);
        }

        /// <summary>Captura a cena isolada de validação sem salvar câmera ou textura no projeto.</summary>
        public static void CapturarDemo(
            GameObject raiz,
            string caminho,
            Func<EstradaNativa, Vector3, float> alturaEm
        )
        {
            const int larguraPainel = 800;
            const int altura = 900;
            var estradas = raiz.GetComponentsInChildren<EstradaNativa>();
            if (estradas.Length != 2)
                throw new InvalidOperationException(
                    "A captura espera as duas estradas da demonstração."
                );
            Array.Sort(estradas, (a, b) => a.Tipo.CompareTo(b.Tipo));
            var objeto = new GameObject("Câmera temporária das estradas");
            var camera = objeto.AddComponent<Camera>();
            var render = RenderTexture.GetTemporary(
                larguraPainel,
                altura,
                24,
                RenderTextureFormat.ARGB32
            );
            var anterior = RenderTexture.active;
            Texture2D imagem = null;
            try
            {
                camera.fieldOfView = 52f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 250f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.35f, 0.48f, 0.62f);
                camera.targetTexture = render;
                imagem = new Texture2D(
                    larguraPainel * estradas.Length,
                    altura,
                    TextureFormat.RGB24,
                    false
                );
                for (var painel = 0; painel < estradas.Length; painel++)
                {
                    var estrada = estradas[painel];
                    var pontos = PerfilDaEstrada.Amostrar(estrada);
                    var meio = pontos.Count / 2;
                    var centro = pontos[meio];
                    centro.y = alturaEm(estrada, centro);
                    var direcao = pontos[Mathf.Min(meio + 1, pontos.Count - 1)] - pontos[meio - 1];
                    direcao.y = 0f;
                    direcao.Normalize();
                    var lateral = Vector3.Cross(Vector3.up, direcao);
                    // Mesmo enquadramento próximo para conferir terra, lajotas e a junção ao Terrain.
                    camera.transform.position =
                        centro - direcao * 9f + lateral * 5f + Vector3.up * 7f;
                    camera.transform.LookAt(centro);
                    Renderizar(camera, render);
                    RenderTexture.active = render;
                    imagem.ReadPixels(
                        new Rect(0, 0, larguraPainel, altura),
                        larguraPainel * painel,
                        0
                    );
                }
                imagem.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(caminho)));
                File.WriteAllBytes(caminho, imagem.EncodeToPNG());
                CapturarPanorama(camera, raiz, caminho);
                var shader = Shader.Find(NomeDoShader);
                if (shader == null || ShaderUtil.ShaderHasError(shader))
                {
                    var mensagens = new List<string>();
                    if (shader != null)
                    {
                        foreach (var mensagem in ShaderUtil.GetShaderMessages(shader))
                        {
                            mensagens.Add(mensagem.message);
                        }
                    }

                    throw new InvalidOperationException(
                        "Shader da estrada falhou: " + string.Join("\n", mensagens)
                    );
                }

                Debug.Log("[Estradas] captura e shader conferidos: " + caminho);
            }
            finally
            {
                RenderTexture.active = anterior;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(imagem);
                UnityEngine.Object.DestroyImmediate(objeto);
            }
        }

        private static void Renderizar(Camera camera, RenderTexture render)
        {
            var pedido = new RenderPipeline.StandardRequest { destination = render };
            if (RenderPipeline.SupportsRenderRequest(camera, pedido))
                RenderPipeline.SubmitRenderRequest(camera, pedido);
            else
                camera.Render();
        }

        private static void CapturarPanorama(Camera camera, GameObject raiz, string caminho)
        {
            const int largura = 1600;
            const int altura = 900;
            var limites = new Bounds(new Vector3(32f, 0f, 16f), Vector3.zero);
            foreach (var terreno in raiz.GetComponentsInChildren<Terrain>())
            {
                var dados = terreno.terrainData;
                var centro = terreno.transform.position + dados.size * 0.5f;
                centro.y = terreno.transform.position.y + terreno.SampleHeight(centro);
                limites.Encapsulate(
                    new Bounds(centro, new Vector3(dados.size.x, 0f, dados.size.z))
                );
            }

            var render = RenderTexture.GetTemporary(
                largura,
                altura,
                24,
                RenderTextureFormat.ARGB32
            );
            var alvoAnterior = camera.targetTexture;
            var ativoAnterior = RenderTexture.active;
            Texture2D imagem = null;
            try
            {
                // Mantém uma vista distante: os close-ups sozinhos não revelam erros do LOD do Terrain.
                camera.transform.position = limites.center + new Vector3(20f, 28f, -32f);
                camera.transform.LookAt(limites.center);
                camera.fieldOfView = 55f;
                camera.targetTexture = render;
                Renderizar(camera, render);
                RenderTexture.active = render;
                imagem = new Texture2D(largura, altura, TextureFormat.RGB24, false);
                imagem.ReadPixels(new Rect(0, 0, largura, altura), 0, 0);
                imagem.Apply();
                var panorama = Path.Combine(
                    Path.GetDirectoryName(caminho),
                    Path.GetFileNameWithoutExtension(caminho) + "-panorama.png"
                );
                File.WriteAllBytes(panorama, imagem.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = ativoAnterior;
                camera.targetTexture = alvoAnterior;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(imagem);
            }
        }

        internal static Mesh CriarSuperficie(
            EstradaNativa estrada,
            IReadOnlyList<Vector3> pontos,
            Func<Vector3, float> alturaEm
        )
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cores = new List<Color>();
            var triangulos = new List<int>();
            var distancias = Distancias(pontos);
            var colunas = FaixasTransversais + 1;
            var transicao = Mathf.Max(0.05f, estrada.TransicaoVisual);

            for (var i = 0; i < pontos.Count; i++)
            {
                var centro = pontos[i];
                var lateral = Lateral(pontos, i);
                var meiaLargura = PerfilDaEstrada.MeiaLargura(estrada, centro);
                var meiaLarguraVisual = meiaLargura + transicao;
                var opacidadeDasPontas = Mathf.Clamp01(
                    Mathf.Min(distancias[i], distancias[distancias.Length - 1] - distancias[i])
                        / transicao
                );

                for (var coluna = 0; coluna < colunas; coluna++)
                {
                    // Cada borda tem sua própria subdivisão, mesmo quando a mistura é maior que o leito.
                    var afastamento =
                        coluna <= FaixasDaBorda
                            ? -meiaLarguraVisual + transicao * coluna / FaixasDaBorda
                        : coluna <= FaixasDaBorda + FaixasDoLeito
                            ? Mathf.Lerp(
                                -meiaLargura,
                                meiaLargura,
                                (coluna - FaixasDaBorda) / (float)FaixasDoLeito
                            )
                        : meiaLargura
                            + transicao * (coluna - FaixasDaBorda - FaixasDoLeito) / FaixasDaBorda;
                    var posicao = centro + lateral * afastamento;
                    posicao.y = alturaEm(posicao) + FolgaVertical;
                    vertices.Add(estrada.transform.InverseTransformPoint(posicao));
                    uvs.Add(new Vector2(afastamento, distancias[i]));
                    // O shader suaviza uma única vez: repetir SmoothStep encurtava a mistura perceptível.
                    var opacidadeLateral = Mathf.Clamp01(
                        (meiaLarguraVisual - Mathf.Abs(afastamento)) / transicao
                    );
                    cores.Add(new Color(1f, 1f, 1f, opacidadeLateral * opacidadeDasPontas));
                }

                if (i == 0)
                {
                    continue;
                }

                var anterior = (i - 1) * colunas;
                for (var coluna = 0; coluna < FaixasTransversais; coluna++)
                {
                    var a = anterior + coluna;
                    triangulos.Add(a);
                    triangulos.Add(a + colunas);
                    triangulos.Add(a + 1);
                    triangulos.Add(a + 1);
                    triangulos.Add(a + colunas);
                    triangulos.Add(a + colunas + 1);
                }
            }

            return FinalizarMalha("Superfície da estrada", vertices, uvs, cores, triangulos);
        }

        public static int QuantidadeDePedregulhos(
            EstradaNativa estrada,
            IReadOnlyList<Vector3> pontos
        )
        {
            var area = 0f;
            for (var i = 1; i < pontos.Count; i++)
            {
                var comprimento = Vector2.Distance(
                    new Vector2(pontos[i - 1].x, pontos[i - 1].z),
                    new Vector2(pontos[i].x, pontos[i].z)
                );
                area +=
                    comprimento
                    * (
                        PerfilDaEstrada.MeiaLargura(estrada, pontos[i - 1])
                        + PerfilDaEstrada.MeiaLargura(estrada, pontos[i])
                    );
            }
            return Mathf.FloorToInt(area * estrada.DensidadeCascalhos);
        }

        internal static Mesh CriarPedregulhos(
            EstradaNativa estrada,
            IReadOnlyList<Vector3> pontos,
            Func<Vector3, float> alturaEm
        )
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cores = new List<Color>();
            var triangulos = new List<int>();
            var aleatorio = new System.Random(estrada.Semente);
            var distancias = Distancias(pontos);
            var comprimento = distancias[distancias.Length - 1];
            var quantidadeDesejada = QuantidadeDePedregulhos(estrada, pontos);
            var quantidade = Mathf.Min(LimiteDePedregulhos, quantidadeDesejada);
            if (quantidadeDesejada > quantidade)
                Debug.LogWarning(
                    "[Estradas] Densidade limitada a 8.192 cascalhos nesta estrada. Divida caminhos longos em trechos para manter o detalhe.",
                    estrada
                );
            var indices = new[]
            {
                4,
                0,
                1,
                4,
                1,
                2,
                4,
                2,
                3,
                4,
                3,
                0,
                5,
                1,
                0,
                5,
                2,
                1,
                5,
                3,
                2,
                5,
                0,
                3,
            };
            var trecho = 0;

            for (var i = 0; i < quantidade; i++)
            {
                var distancia =
                    (i + 0.25f + (float)aleatorio.NextDouble() * 0.5f) * comprimento / quantidade;
                while (trecho + 2 < pontos.Count && distancias[trecho + 1] < distancia)
                {
                    trecho++;
                }

                var t = Mathf.InverseLerp(distancias[trecho], distancias[trecho + 1], distancia);
                var centro = Vector3.Lerp(pontos[trecho], pontos[trecho + 1], t);
                var largura = PerfilDaEstrada.MeiaLargura(estrada, centro);
                var lado = aleatorio.Next(2) == 0 ? -1f : 1f;
                // Espalha também pelo leito, mantendo uma concentração discreta junto às margens.
                var afastamento =
                    largura
                    * (
                        aleatorio.NextDouble() < 0.45
                            ? Mathf.Lerp(0.65f, 1f, (float)aleatorio.NextDouble())
                            : (float)aleatorio.NextDouble()
                    );
                centro += Lateral(pontos, trecho) * afastamento * lado;
                centro.y = alturaEm(centro) + FolgaVertical;
                var raio = Mathf.Lerp(0.055f, 0.14f, (float)aleatorio.NextDouble());
                var giro = Quaternion.Euler(0f, (float)aleatorio.NextDouble() * 360f, 0f);
                var forma = new[]
                {
                    new Vector3(-1f, 0f, -0.2f),
                    new Vector3(-0.1f, 0f, 0.85f),
                    new Vector3(1f, 0f, 0.1f),
                    new Vector3(0.1f, 0f, -0.8f),
                    new Vector3(0.12f, 0.55f, 0f),
                    new Vector3(0f, -0.2f, 0.05f),
                };
                var inicio = vertices.Count;
                var tom = Mathf.Lerp(0.75f, 1.15f, (float)aleatorio.NextDouble());
                foreach (var vertice in forma)
                {
                    vertices.Add(
                        estrada.transform.InverseTransformPoint(centro + giro * vertice * raio)
                    );
                    uvs.Add(new Vector2(centro.x + vertice.x * raio, centro.z + vertice.z * raio));
                    cores.Add(new Color(tom, tom, tom, 1f));
                }

                foreach (var indice in indices)
                {
                    triangulos.Add(inicio + indice);
                }
            }

            return FinalizarMalha("Pedregulhos combinados", vertices, uvs, cores, triangulos);
        }

        private static float[] Distancias(IReadOnlyList<Vector3> pontos)
        {
            var distancias = new float[pontos.Count];
            for (var i = 1; i < pontos.Count; i++)
            {
                distancias[i] = distancias[i - 1] + Vector3.Distance(pontos[i - 1], pontos[i]);
            }

            return distancias;
        }

        private static Vector3 Lateral(IReadOnlyList<Vector3> pontos, int indice)
        {
            var anterior = pontos[Mathf.Max(0, indice - 1)];
            var seguinte = pontos[Mathf.Min(pontos.Count - 1, indice + 1)];
            var direcao = seguinte - anterior;
            direcao.y = 0f;
            return direcao.sqrMagnitude < 0.0001f
                ? Vector3.right
                : Vector3.Cross(Vector3.up, direcao).normalized;
        }

        private static Mesh FinalizarMalha(
            string nome,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<Color> cores,
            List<int> triangulos
        )
        {
            var malha = new Mesh { name = nome, indexFormat = IndexFormat.UInt32 };
            malha.SetVertices(vertices);
            malha.SetUVs(0, uvs);
            malha.SetColors(cores);
            malha.SetTriangles(triangulos, 0);
            malha.RecalculateNormals();
            malha.RecalculateBounds();
            return malha;
        }

        private static Mesh PersistirMalha(Mesh gerada, string caminho)
        {
            var salva = AssetDatabase.LoadAssetAtPath<Mesh>(caminho);
            if (salva == null)
            {
                AssetDatabase.CreateAsset(gerada, caminho);
                return gerada;
            }

            Undo.RegisterCompleteObjectUndo(salva, "Atualizar malha da estrada");
            EditorUtility.CopySerialized(gerada, salva);
            UnityEngine.Object.DestroyImmediate(gerada);
            EditorUtility.SetDirty(salva);
            return salva;
        }

        private static Material ObterMaterial(EstradaNativa estrada, string caminho, bool pedra)
        {
            var shader = Shader.Find(NomeDoShader);
            if (shader == null)
            {
                throw new InvalidOperationException("Shader da estrada nativa não foi importado.");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (material == null)
            {
                material = new Material(shader) { name = pedra ? "Pedregulhos" : "Estrada" };
                AssetDatabase.CreateAsset(material, caminho);
            }
            else
            {
                Undo.RecordObject(material, "Atualizar material da estrada");
            }

            material.SetFloat("_Tipo", pedra ? 2f : (float)estrada.Tipo);
            // Pedras são opacas e escrevem profundidade antes da faixa transparente da estrada.
            // Caso contrário a superfície pode cobri-las conforme a ordenação dos renderizadores.
            material.SetFloat("_ZWrite", pedra ? 1f : 0f);
            material.renderQueue = pedra
                ? (int)RenderQueue.Geometry + 1
                : (int)RenderQueue.Transparent - 20;
            material.SetFloat("_Cascos", estrada.IntensidadeCascos);
            material.SetFloat("_Semente", estrada.Semente % 10000);
            if (pedra)
                material.SetColor("_CorPedra", estrada.CorCascalhos);
            var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturaDaTerra);
            material.SetTexture("_BaseMap", textura);
            material.SetFloat("_UsarTextura", textura == null ? 0f : 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AtualizarObjeto(
            EstradaNativa estrada,
            string nome,
            Mesh malha,
            Material material
        )
        {
            var filho = estrada.transform.Find(nome);
            GameObject objeto;
            if (filho == null)
            {
                objeto = new GameObject(nome);
                Undo.RegisterCreatedObjectUndo(objeto, "Criar visual da estrada");
                Undo.SetTransformParent(objeto.transform, estrada.transform, "Agrupar estrada");
                objeto.transform.localPosition = Vector3.zero;
                objeto.transform.localRotation = Quaternion.identity;
                objeto.transform.localScale = Vector3.one;
            }
            else
            {
                objeto = filho.gameObject;
            }

            var filtro = objeto.GetComponent<MeshFilter>();
            if (filtro == null)
                filtro = Undo.AddComponent<MeshFilter>(objeto);
            var renderizador = objeto.GetComponent<MeshRenderer>();
            if (renderizador == null)
                renderizador = Undo.AddComponent<MeshRenderer>(objeto);
            Undo.RecordObject(filtro, "Atualizar visual da estrada");
            Undo.RecordObject(renderizador, "Atualizar visual da estrada");
            filtro.sharedMesh = malha;
            renderizador.sharedMaterial = material;
            renderizador.shadowCastingMode = ShadowCastingMode.Off;
            renderizador.receiveShadows = true;
            EditorUtility.SetDirty(objeto);
        }

        private static void RemoverFilho(EstradaNativa estrada, string nome)
        {
            var filho = estrada.transform.Find(nome);
            if (filho != null)
            {
                Undo.DestroyObjectImmediate(filho.gameObject);
            }
        }

        private static void GarantirPasta(string caminho)
        {
            if (
                string.IsNullOrEmpty(caminho)
                || !caminho.StartsWith("Assets/", StringComparison.Ordinal)
            )
            {
                throw new ArgumentException("A pasta da estrada precisa estar dentro de Assets.");
            }

            if (AssetDatabase.IsValidFolder(caminho))
            {
                return;
            }

            var separador = caminho.LastIndexOf('/');
            var pai = caminho.Substring(0, separador);
            if (pai != "Assets")
            {
                GarantirPasta(pai);
            }

            AssetDatabase.CreateFolder(pai, caminho.Substring(separador + 1));
        }
    }
}
