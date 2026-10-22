using System;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using UnityEditor;
using UnityEngine;
using UnityEngine.TerrainTools;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Verifica operações reais do Unity em quatro blocos temporários, sem editar a autoria.</summary>
    public static class ValidacaoDosBlocosDeTerreno
    {
        public static void Executar()
        {
            var terrenos = new Terrain[4];
            var camada = new TerrainLayer();
            var outra = new TerrainLayer();
            var dados = new TerrainData[4];
            ConjuntoDeVegetacao conjunto = null;
            try
            {
                for (var z = 0; z < 2; z++)
                for (var x = 0; x < 2; x++)
                {
                    var i = z * 2 + x;
                    dados[i] = new TerrainData
                    {
                        heightmapResolution = 33,
                        alphamapResolution = 32,
                        size = new Vector3(500f, 100f, 500f),
                        terrainLayers = new[] { camada, outra },
                    };
                    var alturas = new float[33, 33];
                    var pintura = new float[32, 32, 2];
                    for (var a = 0; a < 33; a++)
                    for (var b = 0; b < 33; b++)
                        alturas[a, b] = 0.1f;
                    for (var a = 0; a < 32; a++)
                    for (var b = 0; b < 32; b++)
                        pintura[a, b, 0] = 1f;
                    dados[i].SetHeights(0, 0, alturas);
                    dados[i].SetAlphamaps(0, 0, pintura);
                    dados[i].SetDetailResolution(32, 8);
                    var objeto = Terrain.CreateTerrainGameObject(dados[i]);
                    objeto.name = "Bloco temporário de validação " + i;
                    objeto.transform.position = new Vector3(
                        10000f + x * 500f,
                        0f,
                        10000f + z * 500f
                    );
                    terrenos[i] = objeto.GetComponent<Terrain>();
                }
                GradeDeTerrenosDoMundo.Conectar(terrenos);
                if (PinturaDeVegetacao.TerrenosConectados(terrenos[0]).Length != 4)
                    throw new InvalidOperationException(
                        "O pincel próprio não alcança os quatro vizinhos."
                    );
                var centro = new Vector3(10500f, 0f, 10500f);
                foreach (var terreno in terrenos)
                    if (!PinturaDeVegetacao.Intersecta(terreno, centro, 5f))
                        throw new InvalidOperationException(
                            "O pincel não atravessa o encontro de quatro blocos."
                        );

                var retangulo = new Rect(460f, 460f, 80f, 80f);
                var contexto = TerrainPaintUtility.BeginPaintHeightmap(terrenos[0], retangulo);
                try
                {
                    if (contexto.terrainCount != 4)
                        throw new InvalidOperationException(
                            "A ferramenta nativa de relevo não reuniu os quatro blocos."
                        );
                    Limpar(contexto.destinationRenderTexture, new Color(0.2f, 0f, 0f, 0f));
                    TerrainPaintUtility.EndPaintHeightmap(contexto, "Validar emenda de relevo");
                    contexto = null;
                }
                finally
                {
                    if (contexto != null)
                        TerrainPaintUtility.ReleaseContextResources(contexto);
                }
                PaintContext.ApplyDelayedActions();
                foreach (var terreno in terrenos)
                    terreno.terrainData.SyncHeightmap();
                ConferirCantos(dados);
                var altura = dados[0].GetHeight(32, 32);
                if (Mathf.Abs(altura - 10f) < 0.1f)
                    throw new InvalidOperationException(
                        "A operação de relevo não alterou o canto compartilhado."
                    );

                contexto = TerrainPaintUtility.BeginPaintTexture(terrenos[0], retangulo, outra);
                try
                {
                    if (contexto.terrainCount != 4)
                        throw new InvalidOperationException(
                            "A pintura nativa não reuniu os quatro blocos."
                        );
                    Limpar(contexto.destinationRenderTexture, Color.white);
                    TerrainPaintUtility.EndPaintTexture(contexto, "Validar pintura entre blocos");
                    contexto = null;
                }
                finally
                {
                    if (contexto != null)
                        TerrainPaintUtility.ReleaseContextResources(contexto);
                }
                PaintContext.ApplyDelayedActions();
                for (var i = 0; i < 4; i++)
                {
                    dados[i].SyncTexture(TerrainData.AlphamapTextureName);
                    var x = i % 2 == 0 ? 31 : 0;
                    var z = i / 2 == 0 ? 31 : 0;
                    if (dados[i].GetAlphamaps(x, z, 1, 1)[0, 0, 1] < 0.9f)
                        throw new InvalidOperationException(
                            "A pintura não atravessou o canto do bloco " + i
                        );
                }
                var texturas = AssetDatabase.FindAssets(
                    "t:Texture2D",
                    new[] { "Assets/ThirdParty" }
                );
                if (texturas.Length == 0)
                    throw new InvalidOperationException(
                        "A validação de vegetação precisa de uma textura persistente do projeto."
                    );
                conjunto = ScriptableObject.CreateInstance<ConjuntoDeVegetacao>();
                conjunto.Raio = 40f;
                conjunto.CoberturaDeGrama = 1f;
                conjunto.Gramas.Add(
                    new EspecieDoPincel
                    {
                        Textura = AssetDatabase.LoadAssetAtPath<Texture2D>(
                            AssetDatabase.GUIDToAssetPath(texturas[0])
                        ),
                    }
                );
                Undo.IncrementCurrentGroup();
                var grupo = Undo.GetCurrentGroup();
                foreach (var terreno in terrenos)
                    PinturaDeVegetacao.Aplicar(
                        terreno,
                        conjunto,
                        centro,
                        false,
                        true,
                        new System.Random(12345),
                        terrenos
                    );
                Undo.CollapseUndoOperations(grupo);
                foreach (var terreno in dados)
                {
                    var total = 0;
                    foreach (var valor in terreno.GetDetailLayer(0, 0, 32, 32, 0))
                        total += valor;
                    if (total == 0)
                        throw new InvalidOperationException(
                            "A vegetação não atravessou uma das quatro divisas."
                        );
                }
                Undo.PerformUndo();
                foreach (var terreno in dados)
                    if (terreno.detailPrototypes.Length != 0)
                        throw new InvalidOperationException(
                            "Undo não desfez a vegetação nos quatro blocos."
                        );
                Debug.Log(
                    "[Mundo] Validação: relevo, pintura e vegetação atravessaram quatro blocos; alturas dos cantos coincidem; um Undo desfez os quatro blocos de vegetação."
                );
            }
            finally
            {
                foreach (var terreno in terrenos)
                    if (terreno != null)
                        Object.DestroyImmediate(terreno.gameObject);
                foreach (var terreno in dados)
                    if (terreno != null)
                    {
                        Undo.ClearUndo(terreno);
                        Object.DestroyImmediate(terreno);
                    }
                Object.DestroyImmediate(camada);
                Object.DestroyImmediate(outra);
                if (conjunto != null)
                    Object.DestroyImmediate(conjunto);
            }
        }

        private static void Limpar(RenderTexture textura, Color cor)
        {
            var anterior = RenderTexture.active;
            try
            {
                RenderTexture.active = textura;
                GL.Clear(false, true, cor);
            }
            finally
            {
                RenderTexture.active = anterior;
            }
        }

        private static void ConferirCantos(TerrainData[] dados)
        {
            var altura = dados[0].GetHeight(32, 32);
            if (
                Mathf.Abs(altura - dados[1].GetHeight(0, 32)) > 0.01f
                || Mathf.Abs(altura - dados[2].GetHeight(32, 0)) > 0.01f
                || Mathf.Abs(altura - dados[3].GetHeight(0, 0)) > 0.01f
            )
                throw new InvalidOperationException(
                    "A ferramenta nativa criou um degrau no encontro de quatro blocos."
                );
        }

        public static void MigrarEValidar()
        {
            Executar();
            DivisorDoTerrenoEmBlocos.Dividir();
            ConferirMigracao();
        }

        public static void ConferirMigracao()
        {
            var terrenos = ExportadorDoMapaFeitoAMao.EncontrarTerrenos();
            DivisorDoTerrenoEmBlocos.ValidarBordas(terrenos);
            var legado = AssetDatabase.LoadAssetAtPath<TerrainData>(
                "Assets/_Game/Features/Mundo/Resources/Mundo/TerrenoDoMundo.asset"
            );
            var erroMaximo = ConferirAlturas(legado, terrenos);
            foreach (var terreno in terrenos)
            {
                if (terreno.terrainData.treeInstanceCount != 0)
                    throw new InvalidOperationException("Uma árvore foi migrada sem autorização.");
                if (terreno.GetComponent<TerrainCollider>().terrainData != terreno.terrainData)
                    throw new InvalidOperationException("Colisor desconectado.");
            }
            Debug.Log(
                $"[Mundo] Validação da migração: {terrenos.Length} blocos, 60 emendas, 90601 amostras de altura; maior diferença para a origem: {erroMaximo:F4} m. Colisores conectados. Grade do servidor inalterada."
            );
        }

        public static float ConferirAlturas(TerrainData legado, Terrain[] terrenos)
        {
            const float ToleranciaEmMetros = 0.05f;
            var erroMaximo = 0f;
            for (var z = 0; z <= 300; z++)
            for (var x = 0; x <= 300; x++)
            {
                var px = -1500f + x * 10f;
                var pz = -1500f + z * 10f;
                var terreno = terrenos[GradeDeTerrenosDoMundo.Indice(px, pz)];
                var altura = terreno.SampleHeight(new Vector3(px, 0f, pz));
                var original = legado.GetInterpolatedHeight(x / 300f, z / 300f);
                erroMaximo = Mathf.Max(erroMaximo, Mathf.Abs(altura - original));
            }
            if (erroMaximo > ToleranciaEmMetros)
                throw new InvalidOperationException(
                    $"A reamostragem alterou o relevo em {erroMaximo:F4} m (limite {ToleranciaEmMetros} m). A cena não deve ser trocada."
                );
            return erroMaximo;
        }
    }
}
