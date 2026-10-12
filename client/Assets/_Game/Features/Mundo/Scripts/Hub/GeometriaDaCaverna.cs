using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;
using UnityEngine.Rendering;

namespace JogoDoGrupo.Client.Features.Mundo.Hub
{
    /// <summary>
    /// Malha do túnel da primeira caverna (piso, paredes e teto de rocha), montada a partir do
    /// contorno, do piso e do teto de <see cref="PrimeiraCaverna"/>: o mesmo que o servidor usa
    /// para julgar o movimento. O contorno é varrido numa grade de <see cref="Celula"/> metros;
    /// as paredes seguem as bordas da grade (rocha em blocos, de estilo simples).
    /// </summary>
    public static class GeometriaDaCaverna
    {
        /// <summary>Lado de cada célula da grade que varre o contorno do túnel (m).</summary>
        public const float Celula = 0.5f;

        /// <summary>Quanto as paredes descem abaixo do piso, para não deixar fresta (m).</summary>
        public const float AfundamentoDaParede = 0.6f;

        /// <summary>Cria a malha do túnel, nas coordenadas do mundo.</summary>
        public static Mesh CriarMalha()
        {
            PrimeiraCaverna.Limites(out var minX, out var minZ, out var maxX, out var maxZ);
            var colunas = Mathf.CeilToInt((maxX - minX) / Celula);
            var linhas = Mathf.CeilToInt((maxZ - minZ) / Celula);
            var tetos = new float[colunas * linhas];
            var dentro = new bool[colunas * linhas];
            for (var j = 0; j < linhas; j++)
            {
                for (var i = 0; i < colunas; i++)
                {
                    var teto = PrimeiraCaverna.TetoEm(
                        minX + ((i + 0.5f) * Celula),
                        minZ + ((j + 0.5f) * Celula)
                    );
                    dentro[(j * colunas) + i] = teto.HasValue;
                    tetos[(j * colunas) + i] = teto ?? 0f;
                }
            }

            var construtor = new ConstrutorDeFaces();
            for (var j = 0; j < linhas; j++)
            {
                for (var i = 0; i < colunas; i++)
                {
                    if (!dentro[(j * colunas) + i])
                    {
                        continue;
                    }

                    var x0 = minX + (i * Celula);
                    var x1 = x0 + Celula;
                    var z0 = minZ + (j * Celula);
                    var z1 = z0 + Celula;
                    var teto = tetos[(j * colunas) + i];
                    var y00 = PrimeiraCaverna.PisoProximo(x0, z0);
                    var y01 = PrimeiraCaverna.PisoProximo(x0, z1);
                    var y10 = PrimeiraCaverna.PisoProximo(x1, z0);
                    var y11 = PrimeiraCaverna.PisoProximo(x1, z1);

                    // Piso (visto de cima) e teto (visto de baixo).
                    construtor.Face(
                        new Vector3(x0, y00, z0),
                        new Vector3(x0, y01, z1),
                        new Vector3(x1, y11, z1),
                        new Vector3(x1, y10, z0),
                        Vector3.up
                    );
                    construtor.Face(
                        new Vector3(x0, teto, z0),
                        new Vector3(x0, teto, z1),
                        new Vector3(x1, teto, z1),
                        new Vector3(x1, teto, z0),
                        Vector3.down
                    );

                    // Paredes onde a vizinha é rocha.
                    if (!Dentro(dentro, colunas, linhas, i - 1, j))
                    {
                        Parede(construtor, x0, y00, z0, x0, y01, z1, teto, Vector3.right);
                    }

                    if (!Dentro(dentro, colunas, linhas, i + 1, j))
                    {
                        Parede(construtor, x1, y10, z0, x1, y11, z1, teto, Vector3.left);
                    }

                    if (!Dentro(dentro, colunas, linhas, i, j - 1))
                    {
                        Parede(construtor, x0, y00, z0, x1, y10, z0, teto, Vector3.forward);
                    }

                    if (!Dentro(dentro, colunas, linhas, i, j + 1))
                    {
                        Parede(construtor, x0, y01, z1, x1, y11, z1, teto, Vector3.back);
                    }
                }
            }

            return construtor.CriarMalha("TunelDaCaverna");
        }

        private static bool Dentro(bool[] dentro, int colunas, int linhas, int i, int j) =>
            i >= 0 && j >= 0 && i < colunas && j < linhas && dentro[(j * colunas) + i];

        // Parede vertical sobre a aresta (ax, az)-(bx, bz), do piso (afundado) até o teto,
        // virada para <paramref name="paraDentro"/>.
        private static void Parede(
            ConstrutorDeFaces construtor,
            float ax,
            float yA,
            float az,
            float bx,
            float yB,
            float bz,
            float teto,
            Vector3 paraDentro
        )
        {
            construtor.Face(
                new Vector3(ax, yA - AfundamentoDaParede, az),
                new Vector3(ax, teto, az),
                new Vector3(bx, teto, bz),
                new Vector3(bx, yB - AfundamentoDaParede, bz),
                paraDentro
            );
        }

        // Junta faces planas (quatro vértices cada, com normal própria) numa malha só.
        private sealed class ConstrutorDeFaces
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector3> _normais = new List<Vector3>();
            private readonly List<Vector2> _uvs = new List<Vector2>();
            private readonly List<int> _triangulos = new List<int>();

            // Quatro cantos em volta da face; a ordem dos triângulos é a que olha para
            // <paramref name="paraOnde"/> (o lado da frente, que o Unity desenha).
            public void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 paraOnde)
            {
                var normal = Vector3.Cross(b - a, c - a);
                var inverter = Vector3.Dot(normal, paraOnde) < 0f;
                var primeiro = _vertices.Count;
                var cantos = new[] { a, b, c, d };
                for (var k = 0; k < 4; k++)
                {
                    _vertices.Add(cantos[k]);
                    _normais.Add(paraOnde);
                    _uvs.Add(new Vector2(cantos[k].x + cantos[k].z, cantos[k].y));
                }

                if (inverter)
                {
                    Triangulo(primeiro, 2, 1, 0);
                    Triangulo(primeiro, 3, 2, 0);
                }
                else
                {
                    Triangulo(primeiro, 0, 1, 2);
                    Triangulo(primeiro, 0, 2, 3);
                }
            }

            public Mesh CriarMalha(string nome)
            {
                var malha = new Mesh { name = nome, indexFormat = IndexFormat.UInt32 };
                malha.SetVertices(_vertices);
                malha.SetNormals(_normais);
                malha.SetUVs(0, _uvs);
                malha.SetTriangles(_triangulos, 0);
                malha.RecalculateBounds();
                return malha;
            }

            private void Triangulo(int primeiro, int a, int b, int c)
            {
                _triangulos.Add(primeiro + a);
                _triangulos.Add(primeiro + b);
                _triangulos.Add(primeiro + c);
            }
        }
    }
}
