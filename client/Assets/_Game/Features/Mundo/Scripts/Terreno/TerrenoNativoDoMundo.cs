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
            var grade = Resources.Load<GradeDeTerrenosDoMundo>(
                GradeDeTerrenosDoMundo.CaminhoNosResources
            );
            if (grade != null)
            {
                if (grade.Blocos.Length != GradeDeTerrenosDoMundo.Quantidade)
                    throw new System.InvalidOperationException("Grade de terrenos incompleta.");
                var terrenos = new Terrain[grade.Blocos.Length];
                for (var z = 0; z < GradeDeTerrenosDoMundo.BlocosPorLado; z++)
                for (var x = 0; x < GradeDeTerrenosDoMundo.BlocosPorLado; x++)
                {
                    var indice = z * GradeDeTerrenosDoMundo.BlocosPorLado + x;
                    if (grade.Blocos[indice] == null)
                        throw new System.InvalidOperationException(
                            "Bloco de terreno ausente: " + indice
                        );
                    var bloco = Terrain.CreateTerrainGameObject(grade.Blocos[indice]);
                    bloco.name = indice == 0 ? Nome : $"{Nome}_{x}_{z}";
                    bloco.transform.SetParent(pai, false);
                    bloco.transform.localPosition = GradeDeTerrenosDoMundo.Origem(x, z);
                    terrenos[indice] = bloco.GetComponent<Terrain>();
                    GarantirDados(terrenos[indice]);
                }
                GradeDeTerrenosDoMundo.Conectar(terrenos);
                return terrenos[0];
            }
            var origem = Resources.Load<TerrainData>(CaminhoNosResources);
            if (origem == null)
            {
                throw new System.InvalidOperationException(
                    "TerrainData do mundo não encontrado em Resources/Mundo/TerrenoDoMundo."
                );
            }

            var dados = Object.Instantiate(origem);
            var objeto = Terrain.CreateTerrainGameObject(dados);
            objeto.name = Nome;
            objeto.transform.SetParent(pai, false);
            objeto.transform.localPosition = new Vector3(
                -MapaDoMundo.Metade,
                MapaDoMundo.AlturaMinima,
                -MapaDoMundo.Metade
            );
            AbrirEntradaDaCaverna(dados, objeto.transform.position);

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
            AbrirEntradaDaCaverna(dados, terrain.transform.position);
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

        private static void AbrirEntradaDaCaverna(TerrainData dados, Vector3 origem)
        {
            var resolucao = dados.holesResolution;
            // Só a área da caverna pode ter furo: testar o mapa inteiro (um milhão de pontos)
            // travava a entrada no mundo por uns 15 s e a conexão caía por falta de resposta.
            PrimeiraCaverna.Limites(out var minX, out var minZ, out var maxX, out var maxZ);
            if (
                maxX + MargemDaCaverna < origem.x
                || minX - MargemDaCaverna > origem.x + dados.size.x
                || maxZ + MargemDaCaverna < origem.z
                || minZ - MargemDaCaverna > origem.z + dados.size.z
            )
                return;
            var x0 = IndiceDaGrade(minX - MargemDaCaverna - origem.x, dados.size.x, resolucao);
            var x1 = IndiceDaGrade(maxX + MargemDaCaverna - origem.x, dados.size.x, resolucao);
            var z0 = IndiceDaGrade(minZ - MargemDaCaverna - origem.z, dados.size.z, resolucao);
            var z1 = IndiceDaGrade(maxZ + MargemDaCaverna - origem.z, dados.size.z, resolucao);
            var superficie = dados.GetHoles(x0, z0, x1 - x0 + 1, z1 - z0 + 1);
            for (var z = z0; z <= z1; z++)
            {
                var mundoZ = origem.z + dados.size.z * (z + 0.5f) / resolucao;
                for (var x = x0; x <= x1; x++)
                {
                    superficie[z - z0, x - x0] &= !PrimeiraCaverna.FuraOTerreno(
                        origem.x + dados.size.x * (x + 0.5f) / resolucao,
                        mundoZ
                    );
                }
            }

            dados.SetHoles(x0, z0, superficie);
        }

        // Folga em volta dos limites da caverna (m), para o contorno do túnel caber inteiro.
        private const float MargemDaCaverna = 8f;

        private static int IndiceDaGrade(float coordenada, float lado, int resolucao)
        {
            var indice = Mathf.FloorToInt(coordenada / lado * resolucao);
            return Mathf.Clamp(indice, 0, resolucao - 1);
        }
    }
}
