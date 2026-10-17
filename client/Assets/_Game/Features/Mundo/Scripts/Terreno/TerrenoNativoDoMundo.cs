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

        private static void AbrirEntradaDaCaverna(TerrainData dados)
        {
            var resolucao = dados.holesResolution;
            var superficie = new bool[resolucao, resolucao];
            for (var z = 0; z < resolucao; z++)
            {
                var mundoZ = CoordenadaDoMundo(z, resolucao);
                for (var x = 0; x < resolucao; x++)
                {
                    superficie[z, x] = !PrimeiraCaverna.FuraOTerreno(
                        CoordenadaDoMundo(x, resolucao),
                        mundoZ
                    );
                }
            }

            dados.SetHoles(0, 0, superficie);
        }

        private static float CoordenadaDoMundo(int indice, int resolucao)
        {
            return -MapaDoMundo.Metade + MapaDoMundo.Lado * (indice + 0.5f) / resolucao;
        }
    }
}
