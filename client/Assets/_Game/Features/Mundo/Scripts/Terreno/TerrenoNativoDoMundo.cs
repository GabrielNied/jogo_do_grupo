using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>
    /// Instancia o relevo editável feito no Unity. A cópia em execução preserva o asset de autoria
    /// e abre, por cima dela, a boca definida para a primeira caverna.
    /// </summary>
    public static class TerrenoNativoDoMundo
    {
        public const string Nome = "ChaoNativo";

        private const string CaminhoNosResources = "Mundo/TerrenoDoMundo";

        public static Terrain Criar(Transform pai)
        {
            var origem = Resources.Load<TerrainData>(CaminhoNosResources);
            if (origem == null)
            {
                throw new System.InvalidOperationException(
                    "TerrainData do mundo não encontrado em Resources/Mundo/TerrenoDoMundo."
                );
            }

            var dados = Object.Instantiate(origem);
            AbrirEntradaDaCaverna(dados);
            var objeto = Terrain.CreateTerrainGameObject(dados);
            objeto.name = Nome;
            objeto.transform.SetParent(pai, false);
            objeto.transform.localPosition = new Vector3(
                -MapaDoMundo.Metade,
                MapaDoMundo.AlturaMinima,
                -MapaDoMundo.Metade
            );

            var terrain = objeto.GetComponent<Terrain>();
            terrain.drawTreesAndFoliage = true;
            terrain.detailObjectDensity = 1f;
            return terrain;
        }

        /// <summary>
        /// Garante que o Terrain tenha o TerrainData com relevo, biomas e a entrada da caverna
        /// aberta, atualizando também o TerrainCollider.
        /// </summary>
        public static void GarantirDados(Terrain terrain)
        {
            if (terrain == null)
            {
                return;
            }

            if (terrain.terrainData != null && terrain.terrainData.name.EndsWith("(Clone)"))
            {
                return;
            }

            var origem =
                terrain.terrainData != null
                    ? terrain.terrainData
                    : Resources.Load<TerrainData>(CaminhoNosResources);

            if (origem == null)
            {
                throw new System.InvalidOperationException(
                    "TerrainData do mundo não encontrado em Resources/Mundo/TerrenoDoMundo."
                );
            }

            var dados = Object.Instantiate(origem);
            AbrirEntradaDaCaverna(dados);
            terrain.terrainData = dados;
            var colisor = terrain.GetComponent<TerrainCollider>();
            if (colisor != null)
            {
                colisor.terrainData = dados;
            }

            terrain.drawTreesAndFoliage = true;
            terrain.detailObjectDensity = 1f;
            // O URP não tem o billboard de árvore do terreno: a 50 m as árvores viravam
            // silhuetas azul-escuras. Malha até onde a névoa já esconde.
            terrain.treeBillboardDistance = Mathf.Max(
                terrain.treeBillboardDistance,
                DistanciaDasArvoresEmMalha
            );
            terrain.treeMaximumFullLODCount = Mathf.Max(terrain.treeMaximumFullLODCount, 2000);
        }

        /// <summary>Até onde as árvores do terreno são desenhadas como malha (m).</summary>
        public const float DistanciaDasArvoresEmMalha = 320f;

        private static void AbrirEntradaDaCaverna(TerrainData dados)
        {
            var resolucao = dados.holesResolution;
            var superficie = new bool[resolucao, resolucao];
            for (var z = 0; z < resolucao; z++)
            {
                for (var x = 0; x < resolucao; x++)
                {
                    superficie[z, x] = true;
                }
            }

            // Só a área da caverna pode ter furo: testar o mapa inteiro (um milhão de pontos)
            // travava a entrada no mundo por uns 15 s e a conexão caía por falta de resposta.
            PrimeiraCaverna.Limites(out var minX, out var minZ, out var maxX, out var maxZ);
            var x0 = IndiceDaGrade(minX - MargemDaCaverna, resolucao);
            var x1 = IndiceDaGrade(maxX + MargemDaCaverna, resolucao);
            var z0 = IndiceDaGrade(minZ - MargemDaCaverna, resolucao);
            var z1 = IndiceDaGrade(maxZ + MargemDaCaverna, resolucao);
            for (var z = z0; z <= z1; z++)
            {
                var mundoZ = CoordenadaDoMundo(z, resolucao);
                for (var x = x0; x <= x1; x++)
                {
                    superficie[z, x] = !PrimeiraCaverna.FuraOTerreno(
                        CoordenadaDoMundo(x, resolucao),
                        mundoZ
                    );
                }
            }

            dados.SetHoles(0, 0, superficie);
        }

        // Folga em volta dos limites da caverna (m), para o contorno do túnel caber inteiro.
        private const float MargemDaCaverna = 8f;

        private static int IndiceDaGrade(float coordenada, int resolucao)
        {
            var indice = Mathf.FloorToInt(
                (coordenada + MapaDoMundo.Metade) / MapaDoMundo.Lado * resolucao
            );
            return Mathf.Clamp(indice, 0, resolucao - 1);
        }

        private static float CoordenadaDoMundo(int indice, int resolucao)
        {
            return -MapaDoMundo.Metade + MapaDoMundo.Lado * (indice + 0.5f) / resolucao;
        }
    }
}
