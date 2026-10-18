using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    public static class ValidacaoDoEspacamentoVegetacao
    {
        public static void Executar(ConjuntoDeVegetacao origem, Vector3 centro)
        {
            foreach (
                var modo in new[]
                {
                    DetailScatterMode.InstanceCountMode,
                    DetailScatterMode.CoverageMode,
                }
            )
            {
                var receita = Object.Instantiate(origem);
                var dados = new TerrainData
                {
                    heightmapResolution = 33,
                    size = new Vector3(32f, 10f, 32f),
                };
                dados.SetDetailScatterMode(modo);
                dados.SetDetailResolution(32, 8);
                var objeto = Terrain.CreateTerrainGameObject(dados);
                var terreno = objeto.GetComponent<Terrain>();
                dados.treePrototypes = new[]
                {
                    new TreePrototype { prefab = origem.Arvores[0].Modelo },
                };
                dados.treeInstances = new[]
                {
                    new TreeInstance
                    {
                        prototypeIndex = 0,
                        position = new Vector3(0.5f, 0f, 0.5f),
                        widthScale = 1f,
                        heightScale = 1f,
                        color = Color.white,
                        lightmapColor = Color.white,
                    },
                };
                try
                {
                    receita.ArvoresPorCemMetros = 0f;
                    receita.CoberturaDePlantas = 0.4f;
                    receita.CoberturaDeGrama = 1f;
                    receita.DistanciaEntrePlantas = 0f;
                    receita.DistanciaEntreGramas = 0f;
                    PinturaDeVegetacao.Aplicar(terreno, receita, centro, false, false);
                    var plantas = Total(dados, 0);
                    var grama = Total(dados, 1);
                    Verificar(
                        dados.GetDetailLayer(16, 16, 1, 1, 1)[0, 0] == dados.maxDetailScatterPerRes,
                        "Cobertura 100% não atingiu o limite correto de " + modo
                    );
                    Limpar(dados);
                    receita.DistanciaEntreArvores = 20f;
                    PinturaDeVegetacao.Aplicar(terreno, receita, centro, false, false);
                    Verificar(
                        Total(dados, 0) == plantas && Total(dados, 1) == grama,
                        "Espaçamento das árvores alterou a vegetação baixa."
                    );
                    Limpar(dados);
                    receita.DistanciaEntrePlantas = 3f;
                    PinturaDeVegetacao.Aplicar(terreno, receita, centro, false, false);
                    var plantasEspacadas = Total(dados, 0);
                    Verificar(
                        plantasEspacadas < plantas && Total(dados, 1) == grama,
                        "Espaçamento de plantas não é independente da grama."
                    );
                    Limpar(dados);
                    receita.DistanciaEntreGramas = 4f;
                    PinturaDeVegetacao.Aplicar(terreno, receita, centro, false, false);
                    Verificar(
                        Total(dados, 1) < grama && Total(dados, 0) == plantasEspacadas,
                        "Espaçamento de grama não é independente das plantas."
                    );
                    Verificar(
                        receita.DistanciaEntreArvores == 20f,
                        "Um grupo sobrescreveu o espaçamento de outro."
                    );
                    Debug.Log(
                        "[Mundo] Espaçamentos independentes e cobertura plena validados em "
                            + modo
                            + "."
                    );
                }
                finally
                {
                    Object.DestroyImmediate(objeto);
                    Object.DestroyImmediate(dados);
                    Object.DestroyImmediate(receita);
                }
            }
        }

        private static int Total(TerrainData dados, int indice)
        {
            var total = 0;
            foreach (
                var valor in dados.GetDetailLayer(
                    0,
                    0,
                    dados.detailResolution,
                    dados.detailResolution,
                    indice
                )
            )
                total += valor;
            return total;
        }

        private static void Limpar(TerrainData dados)
        {
            for (var i = 0; i < dados.detailPrototypes.Length; i++)
                dados.SetDetailLayer(
                    0,
                    0,
                    i,
                    new int[dados.detailResolution, dados.detailResolution]
                );
        }

        private static void Verificar(bool condicao, string mensagem)
        {
            if (!condicao)
                throw new InvalidOperationException(mensagem);
        }
    }
}
