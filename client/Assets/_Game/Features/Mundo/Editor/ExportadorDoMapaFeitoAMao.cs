using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using JogoDoGrupo.Client.Features.Mundo.Autoria;
using JogoDoGrupo.Client.Features.Mundo.Hub;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Cria a cena de autoria e publica uma grade compacta que cliente e servidor leem.</summary>
    public static class ExportadorDoMapaFeitoAMao
    {
        private const string CaminhoDoTerrain =
            "Assets/_Game/Features/Mundo/Resources/Mundo/TerrenoDoMundo.asset";
        private const string CaminhoDaCena =
            "Assets/_Game/Features/Mundo/Scenes/AutoriaDoMapa.unity";
        private const string CaminhoDoPrefabPublicado =
            "Assets/_Game/Features/Mundo/Resources/Mundo/MapaPublicado.prefab";

        [MenuItem("Jogo do Grupo/Mundo/Criar cena de autoria do mapa")]
        public static void CriarCenaDeAutoria()
        {
            if (File.Exists(CaminhoDaCena))
            {
                EditorSceneManager.OpenScene(CaminhoDaCena, OpenSceneMode.Single);
                return;
            }

            var dados = AssetDatabase.LoadAssetAtPath<TerrainData>(CaminhoDoTerrain);
            if (dados == null)
            {
                throw new InvalidOperationException(
                    "Gere o Terrain nativo antes de criar a cena de autoria."
                );
            }

            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var mapa = ConstrutorHub.Montar();
            mapa.name = ConstrutorHub.NomeRaiz;
            var terreno = mapa.GetComponentInChildren<Terrain>();
            if (terreno == null)
            {
                throw new InvalidOperationException(
                    "A cópia inicial do mapa não recebeu o Terrain."
                );
            }

            var grade = Resources.Load<GradeDeTerrenosDoMundo>(
                GradeDeTerrenosDoMundo.CaminhoNosResources
            );
            foreach (var bloco in mapa.GetComponentsInChildren<Terrain>())
            {
                var posicao = bloco.transform.position;
                bloco.terrainData =
                    grade != null
                        ? grade.Blocos[GradeDeTerrenosDoMundo.Indice(posicao.x, posicao.z)]
                        : dados;
                bloco.GetComponent<TerrainCollider>().terrainData = bloco.terrainData;
                bloco.gameObject.AddComponent<ConfiguracaoDeAutoriaDoMapa>();
            }
            GradeDeTerrenosDoMundo.Conectar(mapa.GetComponentsInChildren<Terrain>());
            new GameObject("Áreas do mapa");
            var pontos = new GameObject("Pontos do mapa");
            pontos.AddComponent<MarcadorDePontoDoMapa>();
            pontos.GetComponent<MarcadorDePontoDoMapa>().Id = "nascimento";
            pontos.GetComponent<MarcadorDePontoDoMapa>().Tipo = TipoDePontoMarcadoDoMapa.Nascimento;
            pontos.transform.position = new Vector3(0f, 0f, -24f);

            GarantirPastaDaCena();

            EditorSceneManager.SaveScene(cena, CaminhoDaCena);
            SalvarPrefabPublicado();
            Selection.activeGameObject = terreno.gameObject;
        }

        [MenuItem("Jogo do Grupo/Mundo/Limpar vegetação procedural antiga")]
        public static void LimparVegetacaoAntiga()
        {
            if (
                !Application.isBatchMode
                && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()
            )
            {
                return;
            }
            var cena = EditorSceneManager.OpenScene(CaminhoDaCena, OpenSceneMode.Single);
            var mapa = GameObject.Find(ConstrutorHub.NomeRaiz);
            if (mapa == null)
            {
                throw new InvalidOperationException("A cena de autoria precisa ter a raiz 'Hub'.");
            }
            ConstrutorHub.RemoverVegetacaoProcedural(mapa.transform);
            SalvarPrefabPublicado();
            EditorSceneManager.SaveScene(cena);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = mapa.GetComponentInChildren<Terrain>().gameObject;
            Debug.Log("[Mundo] Vegetação antiga removida. Pinte árvores e detalhes no Terrain.");
        }

        [MenuItem("Jogo do Grupo/Mundo/Publicar mapa feito à mão")]
        public static void Publicar()
        {
            var terrenos = EncontrarTerrenos();
            var alturas = new short[MapaDoMundo.PontosPorLado * MapaDoMundo.PontosPorLado];
            var biomas = new byte[alturas.Length];
            var areas = new byte[alturas.Length];
            var marcadoresDeArea = Object.FindObjectsByType<MarcadorDeAreaDoMapa>(
                FindObjectsInactive.Exclude
            );

            for (var z = 0; z < MapaDoMundo.PontosPorLado; z++)
            {
                for (var x = 0; x < MapaDoMundo.PontosPorLado; x++)
                {
                    var indice = z * MapaDoMundo.PontosPorLado + x;
                    var mundoX = MapaDoMundo.CoordenadaDaGrade(x);
                    var mundoZ = MapaDoMundo.CoordenadaDaGrade(z);
                    var terrain =
                        terrenos.Length == 1
                            ? terrenos[0]
                            : terrenos[GradeDeTerrenosDoMundo.Indice(mundoX, mundoZ)];
                    var configuracao = terrain.GetComponent<ConfiguracaoDeAutoriaDoMapa>();
                    alturas[indice] = (short)
                        Mathf.RoundToInt(
                            (
                                terrain.SampleHeight(new Vector3(mundoX, 0f, mundoZ))
                                + terrain.transform.position.y
                            ) * 100f
                        );
                    var bioma = BiomaDoTerrain(terrain, configuracao, mundoX, mundoZ);
                    var area = AreasPadrao(bioma, alturas[indice] / 100f);
                    foreach (var marcador in marcadoresDeArea)
                    {
                        if (!marcador.Contem(mundoX, mundoZ))
                        {
                            continue;
                        }

                        bioma = marcador.Bioma;
                        area |= marcador.Areas;
                    }

                    biomas[indice] = (byte)bioma;
                    areas[indice] = (byte)area;
                }
            }

            EscreverDados(alturas, biomas, areas, CopiarPontos());
            SalvarPrefabPublicado();
            AssetDatabase.Refresh();
            CompilationPipeline.RequestScriptCompilation();
            Debug.Log("[Mundo] Mapa feito à mão publicado para o shared.");
        }

        internal static Terrain[] EncontrarTerrenos()
        {
            var mapa = GameObject.Find(ConstrutorHub.NomeRaiz);
            if (mapa == null && File.Exists(CaminhoDaCena))
            {
                EditorSceneManager.OpenScene(CaminhoDaCena, OpenSceneMode.Single);
                mapa = GameObject.Find(ConstrutorHub.NomeRaiz);
            }
            if (mapa == null)
                throw new InvalidOperationException(
                    "Abra a cena de autoria do mapa antes de publicar."
                );
            return OrdenarEValidarTerrenos(mapa.GetComponentsInChildren<Terrain>());
        }

        internal static Terrain[] OrdenarEValidarTerrenos(Terrain[] terrenos)
        {
            if (
                terrenos.Length == 1
                && terrenos[0].terrainData != null
                && terrenos[0].terrainData.size.x == MapaDoMundo.Lado
                && terrenos[0].terrainData.size.z == MapaDoMundo.Lado
            )
                return terrenos;
            if (terrenos.Length != GradeDeTerrenosDoMundo.Quantidade)
                throw new InvalidOperationException(
                    "A autoria precisa ter os 36 blocos de terreno."
                );
            var ordenados = new Terrain[GradeDeTerrenosDoMundo.Quantidade];
            foreach (var terreno in terrenos)
            {
                var posicao = terreno.transform.position;
                var indice = GradeDeTerrenosDoMundo.Indice(posicao.x, posicao.z);
                var dados = terreno.terrainData;
                var origem = GradeDeTerrenosDoMundo.Origem(
                    indice % GradeDeTerrenosDoMundo.BlocosPorLado,
                    indice / GradeDeTerrenosDoMundo.BlocosPorLado
                );
                if (
                    dados == null
                    || ordenados[indice] != null
                    || Vector3.Distance(posicao, origem) > 0.001f
                    || dados.size
                        != new Vector3(
                            GradeDeTerrenosDoMundo.LadoDoBloco,
                            MapaDoMundo.AlturaMaxima - MapaDoMundo.AlturaMinima,
                            GradeDeTerrenosDoMundo.LadoDoBloco
                        )
                    || terreno.transform.lossyScale != Vector3.one
                    || terreno.transform.rotation != Quaternion.identity
                )
                    throw new InvalidOperationException(
                        "Bloco ausente, duplicado, deslocado ou com dimensões inválidas: "
                            + terreno.name
                    );
                ordenados[indice] = terreno;
            }
            return ordenados;
        }

        private static Bioma BiomaDoTerrain(
            Terrain terrain,
            ConfiguracaoDeAutoriaDoMapa configuracao,
            float x,
            float z
        )
        {
            if (configuracao == null || !configuracao.UsarBiomasPintados)
            {
                return MapaDoMundo.BiomaEm(x, z);
            }

            var dados = terrain.terrainData;
            var amostras = dados.GetAlphamaps(
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        (x - terrain.transform.position.x)
                            / dados.size.x
                            * (dados.alphamapWidth - 1)
                    ),
                    0,
                    dados.alphamapWidth - 1
                ),
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        (z - terrain.transform.position.z)
                            / dados.size.z
                            * (dados.alphamapHeight - 1)
                    ),
                    0,
                    dados.alphamapHeight - 1
                ),
                1,
                1
            );
            var camada = 0;
            for (var indice = 1; indice < dados.alphamapLayers; indice++)
            {
                if (amostras[0, 0, indice] > amostras[0, 0, camada])
                {
                    camada = indice;
                }
            }

            return camada < configuracao.BiomasDasCamadas.Length
                ? configuracao.BiomasDasCamadas[camada]
                : Bioma.Campo;
        }

        private static AreaEspecialDoMapa AreasPadrao(Bioma bioma, float altura)
        {
            var areas =
                bioma == Bioma.Cidade ? AreaEspecialDoMapa.Cidade : AreaEspecialDoMapa.Nenhuma;
            if (bioma == Bioma.Pantano && altura < MapaDoMundo.NivelDaAgua)
            {
                areas |= AreaEspecialDoMapa.Agua | AreaEspecialDoMapa.Pantano;
            }

            if (bioma == Bioma.Lava && altura < MapaDoMundo.NivelDaLava)
            {
                areas |= AreaEspecialDoMapa.Lava;
            }

            return areas;
        }

        private static PontoMarcadoDoMapa[] CopiarPontos()
        {
            var marcadores = Object.FindObjectsByType<MarcadorDePontoDoMapa>(
                FindObjectsInactive.Exclude
            );
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pontos = new List<PontoMarcadoDoMapa>();
            foreach (var marcador in marcadores)
            {
                if (string.IsNullOrWhiteSpace(marcador.Id) || !ids.Add(marcador.Id.Trim()))
                {
                    throw new InvalidOperationException(
                        "Todo marcador precisa de um identificador único."
                    );
                }

                var posicao = marcador.transform.position;
                pontos.Add(
                    new PontoMarcadoDoMapa(
                        marcador.Id.Trim(),
                        marcador.Tipo,
                        posicao.x,
                        posicao.z,
                        posicao.y
                    )
                );
            }

            return pontos.ToArray();
        }

        private static void EscreverDados(
            short[] alturas,
            byte[] biomas,
            byte[] areas,
            PontoMarcadoDoMapa[] pontos
        )
        {
            var caminho = Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "..",
                    "..",
                    "shared",
                    "Package",
                    "Runtime",
                    "Features",
                    "Mundo",
                    "DadosPublicadosDoMapa.cs"
                )
            );
            var texto = new StringBuilder();
            texto.AppendLine("namespace JogoDoGrupo.Shared.Features.Mundo");
            texto.AppendLine("{");
            texto.AppendLine("    // Gerado pelo Unity. Não editar à mão.");
            texto.AppendLine("    public static class DadosPublicadosDoMapa");
            texto.AppendLine("    {");
            EscreverVetor(texto, "short", "AlturasEmCentimetros", alturas);
            EscreverVetor(texto, "byte", "Biomas", biomas);
            EscreverVetor(texto, "byte", "Areas", areas);
            texto.AppendLine("        public static readonly PontoMarcadoDoMapa[] Pontos =");
            texto.AppendLine("        {");
            foreach (var ponto in pontos)
            {
                texto.Append("            new PontoMarcadoDoMapa(\"");
                texto.Append(ponto.Id.Replace("\\", "\\\\").Replace("\"", "\\\""));
                texto.Append("\", TipoDePontoMarcadoDoMapa.").Append(ponto.Tipo);
                texto.Append(", ").Append(Numero(ponto.X)).Append(", ").Append(Numero(ponto.Z));
                texto.Append(", ").Append(Numero(ponto.Y.GetValueOrDefault()));
                texto.AppendLine("),");
            }

            texto.AppendLine("        };");
            texto.AppendLine("    }");
            texto.AppendLine("}");
            File.WriteAllText(caminho, texto.ToString(), new UTF8Encoding(false));
        }

        internal static void SalvarPrefabPublicado()
        {
            var mapa = GameObject.Find(ConstrutorHub.NomeRaiz);
            if (mapa == null)
            {
                throw new InvalidOperationException("A cena de autoria precisa ter a raiz 'Hub'.");
            }

            foreach (var terrain in mapa.GetComponentsInChildren<Terrain>())
            {
                if (terrain.terrainData == null || !AssetDatabase.Contains(terrain.terrainData))
                    throw new InvalidOperationException(
                        "Terrain temporário: abra AutoriaDoMapa fora do Play antes de publicar."
                    );
                var colisor = terrain.GetComponent<TerrainCollider>();
                if (colisor == null || colisor.terrainData != terrain.terrainData)
                    throw new InvalidOperationException(
                        "Colisor desconectado do bloco " + terrain.name
                    );
            }

            ConstrutorHub.RemoverVegetacaoProcedural(mapa.transform);
            EditorSceneManager.MarkSceneDirty(mapa.scene);
            PrefabUtility.SaveAsPrefabAsset(mapa, CaminhoDoPrefabPublicado);
            AssetDatabase.SaveAssets();
        }

        private static void GarantirPastaDaCena()
        {
            var pasta = Path.GetDirectoryName(CaminhoDaCena);
            if (!AssetDatabase.IsValidFolder(pasta))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Features/Mundo", "Scenes");
            }
        }

        private static void EscreverVetor(
            StringBuilder texto,
            string tipo,
            string nome,
            Array valores
        )
        {
            texto.Append("        public static readonly ").Append(tipo).Append("[] ").Append(nome);
            texto.AppendLine(" =");
            texto.AppendLine("        {");
            for (var indice = 0; indice < valores.Length; indice++)
            {
                if (indice % 24 == 0)
                {
                    texto.Append("            ");
                }

                texto.Append(
                    Convert.ToString(valores.GetValue(indice), CultureInfo.InvariantCulture)
                );
                texto.Append(indice + 1 == valores.Length ? "" : ", ");
                if (indice % 24 == 23 || indice + 1 == valores.Length)
                {
                    texto.AppendLine();
                }
            }

            texto.AppendLine("        };");
        }

        private static string Numero(float valor)
        {
            return valor.ToString("0.###", CultureInfo.InvariantCulture) + "f";
        }
    }
}
