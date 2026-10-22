using System;
using System.Collections.Generic;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor.Estradas
{
    /// <summary>Backup esparso apenas das células tocadas; nunca substitui TerrainData inteiro.</summary>
    public sealed class RegistroDaEstrada : ScriptableObject
    {
        public bool Aplicada;
        public bool EmAplicacao;
        public List<Vector3> Caminho = new List<Vector3>();
        public float Raio;
        public float Largura;
        public float Transicao;
        public float Margem;
        public float Variacao;
        public int Semente;
        public List<AlteracaoDeTerreno> Alteracoes = new List<AlteracaoDeTerreno>();
    }

    [Serializable]
    public sealed class AlteracaoDeTerreno
    {
        public TerrainData Dados;
        public Vector3 Origem;
        public Vector3 Tamanho;
        public int ResolucaoAltura;
        public int ResolucaoDetalhe;
        public float ErroPixelOriginal;
        public float ErroPixelAplicado;
        public int[] IndicesAltura;
        public float[] AlturasAntes;
        public float[] AlturasDepois;
        public List<AlteracaoDeDetalhe> Detalhes = new List<AlteracaoDeDetalhe>();
        public ArvoreDaEstrada[] ArvoresRemovidas;
        public GameObject[] PrototiposArvores;
        public Texture2D[] TexturasDetalhes;
        public GameObject[] PrototiposDetalhes;
    }

    [Serializable]
    public struct ArvoreDaEstrada
    {
        public Vector3 Posicao;
        public float Largura;
        public float Altura;
        public float Rotacao;
        public Color32 Cor;
        public Color32 Luz;
        public int Prototipo;

        public static ArvoreDaEstrada De(TreeInstance arvore)
        {
            return new ArvoreDaEstrada
            {
                Posicao = arvore.position,
                Largura = arvore.widthScale,
                Altura = arvore.heightScale,
                Rotacao = arvore.rotation,
                Cor = arvore.color,
                Luz = arvore.lightmapColor,
                Prototipo = arvore.prototypeIndex,
            };
        }

        public TreeInstance Instancia()
        {
            return new TreeInstance
            {
                position = Posicao,
                widthScale = Largura,
                heightScale = Altura,
                rotation = Rotacao,
                color = Cor,
                lightmapColor = Luz,
                prototypeIndex = Prototipo,
            };
        }
    }

    [Serializable]
    public sealed class AlteracaoDeDetalhe
    {
        public int Camada;
        public int[] Indices;
        public int[] Antes;
    }

    /// <summary>Índice espacial de segmentos para terrenos grandes e curvas longas.</summary>
    internal sealed class CorredorDaEstrada
    {
        private const float Celula = 16f;
        private readonly IReadOnlyList<Vector3> _pontos;
        private readonly Dictionary<Vector2Int, List<int>> _segmentos =
            new Dictionary<Vector2Int, List<int>>();
        public Bounds Limites { get; }

        public CorredorDaEstrada(IReadOnlyList<Vector3> pontos, float raio)
        {
            _pontos = pontos;
            var limites = new Bounds(pontos[0], Vector3.zero);
            for (var i = 1; i < pontos.Count; i++)
            {
                limites.Encapsulate(pontos[i]);
                var a = pontos[i - 1];
                var b = pontos[i];
                for (
                    var z = Mathf.FloorToInt((Mathf.Min(a.z, b.z) - raio) / Celula);
                    z <= Mathf.FloorToInt((Mathf.Max(a.z, b.z) + raio) / Celula);
                    z++
                )
                for (
                    var x = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - raio) / Celula);
                    x <= Mathf.FloorToInt((Mathf.Max(a.x, b.x) + raio) / Celula);
                    x++
                )
                {
                    var chave = new Vector2Int(x, z);
                    if (!_segmentos.TryGetValue(chave, out var lista))
                    {
                        lista = new List<int>();
                        _segmentos.Add(chave, lista);
                    }
                    lista.Add(i);
                }
            }
            limites.Expand(new Vector3(raio * 2f, 0f, raio * 2f));
            Limites = limites;
        }

        public float Distancia(Vector3 ponto)
        {
            var chave = new Vector2Int(
                Mathf.FloorToInt(ponto.x / Celula),
                Mathf.FloorToInt(ponto.z / Celula)
            );
            if (!_segmentos.TryGetValue(chave, out var lista))
                return float.PositiveInfinity;
            var p = new Vector2(ponto.x, ponto.z);
            var menor = float.PositiveInfinity;
            foreach (var i in lista)
            {
                var a = new Vector2(_pontos[i - 1].x, _pontos[i - 1].z);
                var b = new Vector2(_pontos[i].x, _pontos[i].z);
                var vetor = b - a;
                var t = Mathf.Clamp01(
                    Vector2.Dot(p - a, vetor) / Mathf.Max(vetor.sqrMagnitude, 0.000001f)
                );
                menor = Mathf.Min(menor, Vector2.Distance(p, a + vetor * t));
            }
            return menor;
        }
    }
}
