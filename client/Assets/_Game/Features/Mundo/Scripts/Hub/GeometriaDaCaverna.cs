using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;
using UnityEngine.Rendering;

namespace JogoDoGrupo.Client.Features.Mundo.Hub
{
    /// <summary>
    /// Malha contínua da primeira caverna. Em vez de preencher a passagem com cubos de uma
    /// grade, varre seções em arco pela curva que o shared já usa para o movimento. Assim a boca
    /// fica aberta, a parede é vista pelo lado de dentro e o colisor é a própria superfície.
    /// </summary>
    public static class GeometriaDaCaverna
    {
        /// <summary>Distância entre seções do túnel (m): suave sem criar geometria excessiva.</summary>
        public const float PassoDasSecoes = 1.25f;

        private const int LadosDoArco = 10;

        /// <summary>Cria a malha da galeria, câmara final e beco lateral nas coordenadas do mundo.</summary>
        public static Mesh CriarMalha()
        {
            var construtor = new ConstrutorDeFaces();
            AdicionarPercursoPrincipal(construtor);
            AdicionarBeco(construtor);
            return construtor.CriarMalha("TunelDaCaverna");
        }

        private static void AdicionarPercursoPrincipal(ConstrutorDeFaces construtor)
        {
            var secoes = new List<Secao>();
            var comprimento = PrimeiraCaverna.ComprimentoDoPercurso;
            for (var d = 0f; d < comprimento; d += PassoDasSecoes)
            {
                secoes.Add(SecaoPrincipal(d));
            }

            secoes.Add(SecaoPrincipal(comprimento));
            AdicionarGaleria(construtor, secoes, false, true);
        }

        private static void AdicionarBeco(ConstrutorDeFaces construtor)
        {
            var secoes = new List<Secao>();
            var quantidade = Mathf.CeilToInt(24f / PassoDasSecoes);
            for (var i = 0; i <= quantidade; i++)
            {
                secoes.Add(SecaoDoBeco(i / (float)quantidade));
            }

            // A raiz do beco se mistura com o corredor; só sua ponta é fechada.
            AdicionarGaleria(construtor, secoes, false, true);
        }

        private static Secao SecaoPrincipal(float distancia)
        {
            PrimeiraCaverna.SecaoDoPercurso(
                distancia,
                out var x,
                out var z,
                out var meiaLargura,
                out var piso,
                out var altura,
                out var direcaoX,
                out var direcaoZ
            );
            return new Secao(x, piso, z, meiaLargura, altura, direcaoX, direcaoZ);
        }

        private static Secao SecaoDoBeco(float fracao)
        {
            PrimeiraCaverna.SecaoDoBeco(
                fracao,
                out var x,
                out var z,
                out var meiaLargura,
                out var piso,
                out var altura,
                out var direcaoX,
                out var direcaoZ
            );
            return new Secao(x, piso, z, meiaLargura, altura, direcaoX, direcaoZ);
        }

        private static void AdicionarGaleria(
            ConstrutorDeFaces construtor,
            List<Secao> secoes,
            bool tamparInicio,
            bool tamparFim
        )
        {
            for (var i = 0; i < secoes.Count - 1; i++)
            {
                AdicionarTrecho(construtor, secoes[i], secoes[i + 1]);
            }

            if (tamparInicio)
            {
                Tampar(construtor, secoes[0], -1f);
            }

            if (tamparFim)
            {
                Tampar(construtor, secoes[secoes.Count - 1], 1f);
            }
        }

        private static void AdicionarTrecho(
            ConstrutorDeFaces construtor,
            Secao anterior,
            Secao atual
        )
        {
            // Piso em duas metades: além de manter uma rampa plana no eixo, o vértice central é
            // útil para aferir que o piso renderizado coincide com o contrato do shared.
            construtor.Face(
                anterior.PontoDoChao(0f),
                atual.PontoDoChao(0f),
                atual.PontoDoChao(0.5f),
                anterior.PontoDoChao(0.5f),
                Vector3.up
            );
            construtor.Face(
                anterior.PontoDoChao(0.5f),
                atual.PontoDoChao(0.5f),
                atual.PontoDoChao(1f),
                anterior.PontoDoChao(1f),
                Vector3.up
            );

            for (var lado = 0; lado < LadosDoArco; lado++)
            {
                var t0 = lado / (float)LadosDoArco;
                var t1 = (lado + 1) / (float)LadosDoArco;
                var a = anterior.PontoDoArco(t0);
                var b = atual.PontoDoArco(t0);
                var c = atual.PontoDoArco(t1);
                var d = anterior.PontoDoArco(t1);
                var dentro = (
                    (anterior.Centro + atual.Centro) * 0.5f - ((a + b + c + d) * 0.25f)
                ).normalized;
                construtor.Face(a, b, c, d, dentro);
            }
        }

        private static void Tampar(ConstrutorDeFaces construtor, Secao secao, float sentido)
        {
            var centro = secao.Centro + (Vector3.up * (secao.Altura * 0.42f));
            var normalParaDentro = new Vector3(
                -secao.DirecaoX * sentido,
                0f,
                -secao.DirecaoZ * sentido
            );
            var anterior = secao.PontoDoChao(0f);
            for (var lado = 0; lado <= LadosDoArco; lado++)
            {
                var proximo =
                    lado == LadosDoArco
                        ? secao.PontoDoChao(1f)
                        : secao.PontoDoArco((lado + 1) / (float)LadosDoArco);
                construtor.Triangulo(centro, anterior, proximo, normalParaDentro);
                anterior = proximo;
            }
        }

        private readonly struct Secao
        {
            public Secao(
                float x,
                float piso,
                float z,
                float meiaLargura,
                float altura,
                float direcaoX,
                float direcaoZ
            )
            {
                Centro = new Vector3(x, piso, z);
                MeiaLargura = meiaLargura;
                Altura = altura;
                DirecaoX = direcaoX;
                DirecaoZ = direcaoZ;
            }

            public Vector3 Centro { get; }
            public float MeiaLargura { get; }
            public float Altura { get; }
            public float DirecaoX { get; }
            public float DirecaoZ { get; }

            public Vector3 PontoDoChao(float t)
            {
                var lateral = (t - 0.5f) * 2f * MeiaLargura;
                return Centro + new Vector3(-DirecaoZ * lateral, 0f, DirecaoX * lateral);
            }

            public Vector3 PontoDoArco(float t)
            {
                var angulo = Mathf.PI * t;
                var lateral = Mathf.Cos(angulo) * MeiaLargura;
                return Centro
                    + new Vector3(
                        -DirecaoZ * lateral,
                        Mathf.Sin(angulo) * Altura,
                        DirecaoX * lateral
                    );
            }
        }

        private sealed class ConstrutorDeFaces
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector3> _normais = new List<Vector3>();
            private readonly List<Vector2> _uvs = new List<Vector2>();
            private readonly List<int> _triangulos = new List<int>();

            public void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 paraDentro)
            {
                var normal = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(normal, paraDentro) < 0f)
                {
                    var troca = b;
                    b = d;
                    d = troca;
                    normal = -normal;
                }

                var primeiro = _vertices.Count;
                AdicionarVertice(a, normal);
                AdicionarVertice(b, normal);
                AdicionarVertice(c, normal);
                AdicionarVertice(d, normal);
                _triangulos.Add(primeiro);
                _triangulos.Add(primeiro + 1);
                _triangulos.Add(primeiro + 2);
                _triangulos.Add(primeiro);
                _triangulos.Add(primeiro + 2);
                _triangulos.Add(primeiro + 3);
            }

            public void Triangulo(Vector3 a, Vector3 b, Vector3 c, Vector3 paraDentro)
            {
                var normal = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(normal, paraDentro) < 0f)
                {
                    var troca = b;
                    b = c;
                    c = troca;
                    normal = -normal;
                }

                var primeiro = _vertices.Count;
                AdicionarVertice(a, normal);
                AdicionarVertice(b, normal);
                AdicionarVertice(c, normal);
                _triangulos.Add(primeiro);
                _triangulos.Add(primeiro + 1);
                _triangulos.Add(primeiro + 2);
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

            private void AdicionarVertice(Vector3 vertice, Vector3 normal)
            {
                _vertices.Add(vertice);
                _normais.Add(normal);
                _uvs.Add(new Vector2(vertice.x * 0.25f, (vertice.y + vertice.z) * 0.25f));
            }
        }
    }
}
