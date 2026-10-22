using System;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>Dados persistentes dos blocos, na ordem z * BlocosPorLado + x.</summary>
    public sealed class GradeDeTerrenosDoMundo : ScriptableObject
    {
        public const string CaminhoNosResources = "Mundo/GradeDeTerrenos";
        public const int LadoDoBloco = 500;
        public const int BlocosPorLado = 6;
        public const int Quantidade = BlocosPorLado * BlocosPorLado;
        public const int ResolucaoDoRelevo = 1025;
        public const int ResolucaoDaPintura = 1024;
        public const int ResolucaoDosDetalhes = 1024;
        public const int DetalhesPorPatch = 16;
        public const int GrupoDeConexao = 1132;

        public TerrainData[] Blocos = new TerrainData[Quantidade];

        public static Vector3 Origem(int x, int z) =>
            new Vector3(
                -MapaDoMundo.Metade + x * LadoDoBloco,
                MapaDoMundo.AlturaMinima,
                -MapaDoMundo.Metade + z * LadoDoBloco
            );

        /// <summary>Inclui o limite externo positivo; divisas internas pertencem ao próximo bloco.</summary>
        public static int Indice(float x, float z)
        {
            if (
                x < -MapaDoMundo.Metade
                || x > MapaDoMundo.Metade
                || z < -MapaDoMundo.Metade
                || z > MapaDoMundo.Metade
                || float.IsNaN(x)
                || float.IsNaN(z)
            )
                throw new ArgumentOutOfRangeException(nameof(x), "Ponto fora do mundo.");
            var coluna = Mathf.Min(
                BlocosPorLado - 1,
                Mathf.FloorToInt((x + MapaDoMundo.Metade) / LadoDoBloco)
            );
            var linha = Mathf.Min(
                BlocosPorLado - 1,
                Mathf.FloorToInt((z + MapaDoMundo.Metade) / LadoDoBloco)
            );
            return linha * BlocosPorLado + coluna;
        }

        public static void Conectar(Terrain[] terrenos)
        {
            foreach (var terreno in terrenos)
            {
                if (terreno == null || terreno.terrainData == null)
                    continue;
                terreno.allowAutoConnect = true;
                terreno.groupingID = GrupoDeConexao;
                var origem = terreno.transform.position;
                var tamanho = terreno.terrainData.size;
                terreno.SetNeighbors(
                    Vizinho(origem + Vector3.left * tamanho.x),
                    Vizinho(origem + Vector3.forward * tamanho.z),
                    Vizinho(origem + Vector3.right * tamanho.x),
                    Vizinho(origem + Vector3.back * tamanho.z)
                );
            }

            Terrain Vizinho(Vector3 posicao)
            {
                foreach (var candidato in terrenos)
                    if (
                        candidato != null
                        && (candidato.transform.position - posicao).sqrMagnitude < 0.0001f
                    )
                        return candidato;
                return null;
            }
        }
    }
}
