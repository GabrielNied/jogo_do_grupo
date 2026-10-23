using System.Collections.Generic;
using System.IO;
using JogoDoGrupo.Client.Features.Mundo.Hub;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>
    /// Conserta o <c>TerrenoDoMundo.asset</c> publicado (#1172, #1179): camadas de terreno sem
    /// textura (ou que não existem no repositório) viram xadrez cinza no jogo, e protótipos de
    /// árvore sem prefab enchem o Console de "Tree prefab at index N is missing". Troca só as
    /// referências quebradas por assets que estão no repositório, sem mexer em alturas, pesos das
    /// camadas, buracos, detalhes nem na posição das árvores já pintadas.
    /// <para>
    /// Linha de comando:
    /// <c>npm run unity:executar -- JogoDoGrupo.Client.Features.Mundo.Editor.ReparoDoTerrenoPublicado.Reparar</c>.
    /// </para>
    /// </summary>
    public static class ReparoDoTerrenoPublicado
    {
        public const string CaminhoTerreno =
            "Assets/_Game/Features/Mundo/Resources/Mundo/TerrenoDoMundo.asset";

        private const string PastaCamadas =
            "Assets/3D Haven/Free Fantasy Terrain Textures/Demo/Terrains/Terrain Layers/";

        // Camadas de reserva, na ordem de preferência para cada uso.
        private static readonly string[] CamadaDaCidade =
        {
            PastaCamadas + "3DH FTT Path_001 2K.terrainlayer",
        };
        private static readonly string[] CamadaDeEstrada =
        {
            PastaCamadas + "3DH FTT Dirt_002a 2K.terrainlayer",
        };

        private const string PastaArvores =
            "Assets/ThirdParty/Quaternius/StylizedNatureMegaKit/Resources/Mundo/MataEstilizada/";

        private static readonly string[] Arvores =
        {
            PastaArvores + "CommonTree_1.fbx",
            PastaArvores + "CommonTree_2.fbx",
            PastaArvores + "CommonTree_3.fbx",
            PastaArvores + "CommonTree_4.fbx",
            PastaArvores + "CommonTree_5.fbx",
        };

        /// <summary>Raio dos muros da cidade murada (m), centrado na origem.</summary>
        public const float RaioMuroCidade = LayoutDoHub.Metade;

        /// <summary>Largura da transição suave da borda interna dos muros para o exterior (m).</summary>
        public const float TransicaoMuro = 4f;

        [MenuItem("Jogo do Grupo/Mundo/Reparar terreno publicado")]
        public static void Reparar()
        {
            var dados = AssetDatabase.LoadAssetAtPath<TerrainData>(CaminhoTerreno);
            if (dados == null)
            {
                Debug.LogError("[ReparoDoTerreno] TerrainData não encontrado: " + CaminhoTerreno);
                return;
            }

            var relatorio = new List<string>();
            RepararCamadas(dados, relatorio);
            RepararArvores(dados, relatorio);
            MateriaisDasArvoresDaMata.Consertar(relatorio);
            RepararPavimentoDaPraca(dados, relatorio);

            if (relatorio.Count == 0)
            {
                Debug.Log("[ReparoDoTerreno] Nada a consertar.");
                return;
            }

            EditorUtility.SetDirty(dados);
            AssetDatabase.SaveAssets();
            Debug.Log("[ReparoDoTerreno] " + string.Join("; ", relatorio));
            CapturarVistaAerea();
        }

        [MenuItem("Jogo do Grupo/Mundo/Capturar vista aérea da cidade")]
        public static void CapturarVistaAerea()
        {
            var hub = ConstrutorHub.Montar();
            var cameraGo = new GameObject("CameraAerea");
            var camera = cameraGo.AddComponent<Camera>();
            try
            {
                camera.transform.position = new Vector3(0f, 150f, 35f);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                camera.orthographic = true;
                camera.orthographicSize = 65f;
                camera.clearFlags = CameraClearFlags.Color;
                camera.backgroundColor = new Color(0.2f, 0.3f, 0.15f);

                var rt = RenderTexture.GetTemporary(1280, 720, 24);
                camera.targetTexture = rt;
                camera.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();

                RenderTexture.active = null;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(rt);

                var bytes = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);

                Directory.CreateDirectory("prints");
                var caminho = Path.Combine("prints", "pavimento_praca_saida_norte.png");
                File.WriteAllBytes(caminho, bytes);
                Debug.Log("[ReparoDoTerreno] Print aéreo salvo em " + caminho);
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
                if (hub != null)
                {
                    Object.DestroyImmediate(hub);
                }
            }
        }

        private static void RepararPavimentoDaPraca(TerrainData dados, List<string> relatorio)
        {
            var camadas = dados.terrainLayers;
            var indicePedra = -1;
            var indiceGrama = 0;

            for (var i = 0; i < camadas.Length; i++)
            {
                var nome = camadas[i] != null ? camadas[i].name : "";
                if (nome.Contains("Path_001") || nome.ToLowerInvariant().Contains("stone"))
                {
                    indicePedra = i;
                    break;
                }
            }

            if (indicePedra < 0)
                return;

            var alphamap = dados.GetAlphamaps(0, 0, dados.alphamapWidth, dados.alphamapHeight);
            var res = dados.alphamapResolution;
            var tamanhoX = dados.size.x;
            var tamanhoZ = dados.size.z;
            var meioX = tamanhoX * 0.5f;
            var meioZ = tamanhoZ * 0.5f;

            var pontosAjustados = 0;
            var pontosForaZerados = 0;

            for (var z = 0; z < res; z++)
            {
                var wz = (z / (float)(res - 1)) * tamanhoZ - meioZ;
                for (var x = 0; x < res; x++)
                {
                    var pesoPedra = alphamap[z, x, indicePedra];
                    if (pesoPedra <= 0.0001f)
                        continue;

                    var wx = (x / (float)(res - 1)) * tamanhoX - meioX;
                    var d = Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz));

                    float fator;
                    if (d <= RaioMuroCidade - TransicaoMuro)
                    {
                        fator = 1f;
                    }
                    else if (d >= RaioMuroCidade)
                    {
                        fator = 0f;
                    }
                    else
                    {
                        var t = (d - (RaioMuroCidade - TransicaoMuro)) / TransicaoMuro;
                        fator = Mathf.SmoothStep(1f, 0f, t);
                    }

                    if (fator >= 0.9999f)
                        continue;

                    var novoPeso = pesoPedra * fator;
                    var diferenca = pesoPedra - novoPeso;

                    alphamap[z, x, indicePedra] = novoPeso;
                    alphamap[z, x, indiceGrama] += diferenca;

                    var soma = 0f;
                    for (var i = 0; i < camadas.Length; i++)
                        soma += alphamap[z, x, i];

                    if (soma > 0.0001f)
                    {
                        for (var i = 0; i < camadas.Length; i++)
                            alphamap[z, x, i] /= soma;
                    }
                    else
                    {
                        alphamap[z, x, indiceGrama] = 1f;
                    }

                    pontosAjustados++;
                    if (fator <= 0.0001f)
                        pontosForaZerados++;
                }
            }

            if (pontosAjustados > 0)
            {
                dados.SetAlphamaps(0, 0, alphamap);
                relatorio.Add(
                    $"pavimento da praça limitado aos muros ({RaioMuroCidade} m, transição {TransicaoMuro} m): {pontosAjustados} pontos corrigidos ({pontosForaZerados} zerados fora dos muros)"
                );
                SincronizarBlocos(dados, alphamap, relatorio);
            }
        }

        private static void SincronizarBlocos(
            TerrainData origem,
            float[,,] pintura,
            List<string> relatorio
        )
        {
            var grade = Resources.Load<GradeDeTerrenosDoMundo>(
                GradeDeTerrenosDoMundo.CaminhoNosResources
            );
            if (
                grade == null
                || grade.Blocos == null
                || grade.Blocos.Length != GradeDeTerrenosDoMundo.Quantidade
            )
            {
                return;
            }

            var blocosAtualizados = 0;
            for (var bz = 0; bz < GradeDeTerrenosDoMundo.BlocosPorLado; bz++)
            for (var bx = 0; bx < GradeDeTerrenosDoMundo.BlocosPorLado; bx++)
            {
                var indice = bz * GradeDeTerrenosDoMundo.BlocosPorLado + bx;
                var bloco = grade.Blocos[indice];
                if (bloco == null)
                    continue;

                var res = bloco.alphamapResolution;
                var camadas = bloco.terrainLayers;
                var novaPintura = new float[res, res, camadas.Length];

                for (var z = 0; z < res; z++)
                for (var x = 0; x < res; x++)
                {
                    var px = Coordenada(bx, x, res) * (pintura.GetLength(1) - 1);
                    var pz = Coordenada(bz, z, res) * (pintura.GetLength(0) - 1);
                    var x0 = Mathf.FloorToInt(px);
                    var z0 = Mathf.FloorToInt(pz);
                    var x1 = Mathf.Min(x0 + 1, pintura.GetLength(1) - 1);
                    var z1 = Mathf.Min(z0 + 1, pintura.GetLength(0) - 1);
                    var tx = px - x0;
                    var tz = pz - z0;

                    var soma = 0f;
                    var limite = Mathf.Min(camadas.Length, pintura.GetLength(2));
                    for (var i = 0; i < limite; i++)
                    {
                        var valor = Mathf.Lerp(
                            Mathf.Lerp(pintura[z0, x0, i], pintura[z0, x1, i], tx),
                            Mathf.Lerp(pintura[z1, x0, i], pintura[z1, x1, i], tx),
                            tz
                        );
                        novaPintura[z, x, i] = valor;
                        soma += valor;
                    }

                    if (soma > 0.0001f)
                    {
                        for (var i = 0; i < limite; i++)
                            novaPintura[z, x, i] /= soma;
                    }
                    else if (camadas.Length > 0)
                    {
                        novaPintura[z, x, 0] = 1f;
                    }
                }

                bloco.SetAlphamaps(0, 0, novaPintura);
                EditorUtility.SetDirty(bloco);
                blocosAtualizados++;
            }

            if (blocosAtualizados > 0)
            {
                relatorio.Add($"{blocosAtualizados} blocos sincronizados com a pintura corrigida");
            }
        }

        private static float Coordenada(int bloco, int indice, int resolucao) =>
            (bloco * (resolucao - 1) + indice)
            / (float)(GradeDeTerrenosDoMundo.BlocosPorLado * (resolucao - 1));

        private static void RepararCamadas(TerrainData dados, List<string> relatorio)
        {
            var camadas = dados.terrainLayers;
            var mudou = false;
            for (var i = 0; i < camadas.Length; i++)
            {
                var camada = camadas[i];
                if (camada != null && camada.diffuseTexture != null)
                    continue;

                // A camada de pedras foi pintada só na praça da cidade; a outra quebrada, na
                // estrada e no platô a noroeste. Pelo nome dá para saber qual é qual.
                var ehCidade = camada != null && camada.name.Contains("stone");
                var reserva = Carregar<TerrainLayer>(ehCidade ? CamadaDaCidade : CamadaDeEstrada);
                if (reserva == null)
                    continue;

                relatorio.Add(
                    "camada "
                        + i
                        + " ("
                        + (camada != null ? camada.name : "ausente")
                        + ") → "
                        + reserva.name
                );
                camadas[i] = reserva;
                mudou = true;
            }

            if (mudou)
                dados.terrainLayers = camadas;
        }

        private static void RepararArvores(TerrainData dados, List<string> relatorio)
        {
            var prototipos = dados.treePrototypes;
            var mudou = false;
            for (var i = 0; i < prototipos.Length; i++)
            {
                if (prototipos[i].prefab != null)
                    continue;

                var arvore = AssetDatabase.LoadAssetAtPath<GameObject>(Arvores[i % Arvores.Length]);
                if (arvore == null)
                    continue;

                prototipos[i].prefab = arvore;
                relatorio.Add("árvore " + i + " → " + arvore.name);
                mudou = true;
            }

            if (!mudou)
                return;

            // Guardar as instâncias antes: trocar os protótipos não pode apagar as árvores pintadas.
            var instancias = dados.treeInstances;
            dados.treePrototypes = prototipos;
            dados.RefreshPrototypes();
            dados.SetTreeInstances(instancias, true);
        }

        private static T Carregar<T>(string[] caminhos)
            where T : Object
        {
            foreach (var caminho in caminhos)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(caminho);
                if (asset != null)
                    return asset;
            }

            return null;
        }
    }
}
