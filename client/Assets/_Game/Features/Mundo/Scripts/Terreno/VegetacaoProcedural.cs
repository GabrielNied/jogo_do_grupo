using System.Collections.Generic;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>
    /// Silhuetas que o kit da cidade não tem: cacto, árvore seca, pinheiro, rocha vulcânica,
    /// árvore de pântano e arbusto. Cada tipo é um <see cref="ModeloDePeca"/> montado uma vez: uma
    /// malha só (os blocos do tipo fundidos, uma submalha por cor) e materiais compartilhados.
    /// Cacto, árvores e rocha têm um colisor de cápsula no tronco, do chão ao topo; o arbusto
    /// não tem (dá para atravessar).
    /// </summary>
    public static class VegetacaoProcedural
    {
        private struct Bloco
        {
            public Vector3 Centro;
            public Vector3 Tamanho;
            public Color Cor;

            public Bloco(Vector3 centro, Vector3 tamanho, Color cor)
            {
                Centro = centro;
                Tamanho = tamanho;
                Cor = cor;
            }
        }

        private static readonly Color Marrom = new Color(0.4f, 0.28f, 0.16f);

        private static readonly Dictionary<string, ModeloDePeca> _modelos =
            new Dictionary<string, ModeloDePeca>();

        private static readonly Dictionary<Color, Material> _materiais =
            new Dictionary<Color, Material>();

        private static Material _base;

        public static bool ConsegueCriar(string nome)
        {
            switch (nome)
            {
                case "Cacto":
                case "Arvore_Seca":
                case "Pinheiro":
                case "Rocha_Vulcanica":
                case "Arvore_Pantano":
                case "Arbusto":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Esquece modelos e materiais (testes, ou troca de cena).</summary>
        public static void Limpar()
        {
            _modelos.Clear();
            _materiais.Clear();
            _base = null;
        }

        /// <summary>O modelo do tipo <paramref name="nome"/>, montado na primeira vez e dividido depois.</summary>
        public static ModeloDePeca Modelo(string nome)
        {
            if (_modelos.TryGetValue(nome, out var pronto) && pronto != null)
            {
                return pronto;
            }

            var blocos = Blocos(nome);
            var modelo = Montar(nome, blocos, Colisor(nome));
            _modelos[nome] = modelo;
            return modelo;
        }

        private static List<Bloco> Blocos(string nome)
        {
            var lista = new List<Bloco>();
            switch (nome)
            {
                case "Cacto":
                    lista.Add(
                        new Bloco(
                            new Vector3(0f, 1.1f, 0f),
                            new Vector3(0.35f, 1.1f, 0.35f),
                            Verde(0.35f, 0.55f, 0.28f)
                        )
                    );
                    lista.Add(
                        new Bloco(
                            new Vector3(0.35f, 1.3f, 0f),
                            new Vector3(0.18f, 0.45f, 0.18f),
                            Verde(0.4f, 0.6f, 0.3f)
                        )
                    );
                    break;
                case "Arvore_Seca":
                    lista.Add(
                        new Bloco(
                            new Vector3(0f, 1.2f, 0f),
                            new Vector3(0.12f, 1.2f, 0.12f),
                            Marrom
                        )
                    );
                    lista.Add(
                        new Bloco(
                            new Vector3(0.35f, 2f, 0f),
                            new Vector3(0.5f, 0.06f, 0.06f),
                            Marrom
                        )
                    );
                    break;
                case "Pinheiro":
                    lista.Add(
                        new Bloco(
                            new Vector3(0f, 0.6f, 0f),
                            new Vector3(0.15f, 0.6f, 0.15f),
                            Marrom
                        )
                    );
                    lista.Add(
                        new Bloco(
                            new Vector3(0f, 1.5f, 0f),
                            new Vector3(0.9f, 0.45f, 0.9f),
                            Verde(0.2f, 0.4f, 0.28f)
                        )
                    );
                    lista.Add(
                        new Bloco(
                            new Vector3(0f, 2.2f, 0f),
                            new Vector3(0.55f, 0.4f, 0.55f),
                            Verde(0.25f, 0.48f, 0.32f)
                        )
                    );
                    break;
                case "Rocha_Vulcanica":
                    lista.Add(
                        new Bloco(
                            new Vector3(0f, 0.4f, 0f),
                            new Vector3(1.1f, 0.5f, 0.8f),
                            new Color(0.18f, 0.12f, 0.1f)
                        )
                    );
                    lista.Add(
                        new Bloco(
                            new Vector3(0.35f, 0.7f, 0.1f),
                            new Vector3(0.5f, 0.35f, 0.45f),
                            new Color(0.28f, 0.14f, 0.1f)
                        )
                    );
                    break;
                case "Arvore_Pantano":
                    lista.Add(
                        new Bloco(
                            new Vector3(0.15f, 1f, 0f),
                            new Vector3(0.16f, 1.1f, 0.16f),
                            new Color(0.28f, 0.22f, 0.14f)
                        )
                    );
                    lista.Add(
                        new Bloco(
                            new Vector3(0.2f, 2f, 0.1f),
                            new Vector3(0.7f, 0.45f, 0.7f),
                            Verde(0.22f, 0.32f, 0.2f)
                        )
                    );
                    break;
                default:
                    lista.Add(
                        new Bloco(
                            new Vector3(0f, 0.35f, 0f),
                            new Vector3(0.8f, 0.4f, 0.8f),
                            Verde(0.3f, 0.5f, 0.22f)
                        )
                    );
                    break;
            }

            return lista;
        }

        // Cápsula em pé do chão até o topo do que é sólido. Folgada de propósito: o tronco é fino,
        // mas o jogador tem de bater nele sem entrar pela copa.
        private static ColisorDaPeca? Colisor(string nome)
        {
            switch (nome)
            {
                case "Cacto":
                    return new ColisorDaPeca(new Vector3(0.05f, 0.825f, 0f), 0.3f, 1.65f);
                case "Arvore_Seca":
                    return new ColisorDaPeca(new Vector3(0f, 1.2f, 0f), 0.25f, 2.4f);
                case "Pinheiro":
                    return new ColisorDaPeca(new Vector3(0f, 1.2f, 0f), 0.3f, 2.4f);
                case "Arvore_Pantano":
                    return new ColisorDaPeca(new Vector3(0.15f, 1.225f, 0.05f), 0.3f, 2.45f);
                case "Rocha_Vulcanica":
                    return new ColisorDaPeca(new Vector3(0.1f, 0.475f, 0f), 0.6f, 0.95f);
                default:
                    return null;
            }
        }

        private static Color Verde(float r, float g, float b) => new Color(r, g, b);

        // Funde os blocos numa malha só, com uma submalha por cor.
        private static ModeloDePeca Montar(string nome, List<Bloco> blocos, ColisorDaPeca? colisor)
        {
            var cores = new List<Color>();
            foreach (var bloco in blocos)
            {
                if (!cores.Contains(bloco.Cor))
                {
                    cores.Add(bloco.Cor);
                }
            }

            var vertices = new List<Vector3>();
            var normais = new List<Vector3>();
            var triangulos = new List<int>[cores.Count];
            for (var c = 0; c < cores.Count; c++)
            {
                triangulos[c] = new List<int>();
            }

            foreach (var bloco in blocos)
            {
                AdicionarCubo(bloco, vertices, normais, triangulos[cores.IndexOf(bloco.Cor)]);
            }

            // As posições dos blocos foram escritas com a base um pouco acima do chão; desce tudo
            // até o ponto mais baixo ficar em Y = 0 (a base encosta no terreno).
            var folga = 0f;
            if (vertices.Count > 0)
            {
                folga = float.MaxValue;
                foreach (var vertice in vertices)
                {
                    folga = Mathf.Min(folga, vertice.y);
                }

                for (var i = 0; i < vertices.Count; i++)
                {
                    var v = vertices[i];
                    v.y -= folga;
                    vertices[i] = v;
                }
            }

            colisor = BaixarColisor(colisor, folga);

            var malha = new Mesh { name = $"Vegetacao_{nome}" };
            malha.SetVertices(vertices);
            malha.SetNormals(normais);
            malha.subMeshCount = cores.Count;
            for (var c = 0; c < cores.Count; c++)
            {
                malha.SetTriangles(triangulos[c], c);
            }

            malha.RecalculateBounds();

            var partes = new ParteDeModelo[cores.Count];
            for (var c = 0; c < cores.Count; c++)
            {
                partes[c] = new ParteDeModelo(
                    malha,
                    c,
                    MaterialDaCor(cores[c]),
                    Matrix4x4.identity
                );
            }

            return new ModeloDePeca(nome, partes, colisor, ConfigVegetacao.EhPequena(nome));
        }

        // O colisor vai do chão ao topo; com o modelo `folga` metros mais baixo, o topo desce igual.
        private static ColisorDaPeca? BaixarColisor(ColisorDaPeca? colisor, float folga)
        {
            if (!colisor.HasValue || folga <= 0f)
            {
                return colisor;
            }

            var c = colisor.Value;
            if (c.EhCaixa)
            {
                return c;
            }

            var altura = Mathf.Max(c.Altura - folga, c.Raio * 2f);
            var centro = new Vector3(c.Centro.x, altura * 0.5f, c.Centro.z);
            return new ColisorDaPeca(centro, c.Raio, altura);
        }

        private static readonly Vector3[] Faces =
        {
            Vector3.up,
            Vector3.down,
            Vector3.right,
            Vector3.left,
            Vector3.forward,
            Vector3.back,
        };

        private static void AdicionarCubo(
            Bloco bloco,
            List<Vector3> vertices,
            List<Vector3> normais,
            List<int> triangulos
        )
        {
            foreach (var normal in Faces)
            {
                var u = normal.y != 0f ? Vector3.right : Vector3.up;
                var v = Vector3.Cross(normal, u);
                var meio = bloco.Tamanho * 0.5f;
                var centroDaFace = bloco.Centro + Vector3.Scale(normal, meio);
                var du = Vector3.Scale(u, meio);
                var dv = Vector3.Scale(v, meio);

                var primeiro = vertices.Count;
                vertices.Add(centroDaFace - du - dv);
                vertices.Add(centroDaFace + du - dv);
                vertices.Add(centroDaFace + du + dv);
                vertices.Add(centroDaFace - du + dv);
                for (var k = 0; k < 4; k++)
                {
                    normais.Add(normal);
                }

                // v = normal x u, logo u x v = normal: (0,1,2) e (0,2,3) olham para fora no Unity.
                triangulos.AddRange(
                    new[]
                    {
                        primeiro,
                        primeiro + 1,
                        primeiro + 2,
                        primeiro,
                        primeiro + 2,
                        primeiro + 3,
                    }
                );
            }
        }

        // Um material por cor, dividido por todos os tipos. Cópia do material do kit (para pegar o
        // shader do URP) com a cor trocada.
        private static Material MaterialDaCor(Color cor)
        {
            if (_materiais.TryGetValue(cor, out var pronto) && pronto != null)
            {
                return pronto;
            }

            var origem = MaterialBase();
            Material material;
            if (origem != null)
            {
                material = new Material(origem);
            }
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                material = shader != null ? new Material(shader) : null;
            }

            if (material != null)
            {
                material.name = $"Vegetacao_{ColorUtility.ToHtmlStringRGB(cor)}";
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", null);
                }

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", cor);
                }
                else if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", cor);
                }

                material.enableInstancing = true;
            }

            _materiais[cor] = material;
            return material;
        }

        private static Material MaterialBase()
        {
            if (_base != null)
            {
                return _base;
            }

            var modelo = Resources.Load<GameObject>("Mundo/Pedra_Pequena");
            var renderizador =
                modelo != null ? modelo.GetComponentInChildren<Renderer>(true) : null;
            _base = renderizador != null ? renderizador.sharedMaterial : null;
            return _base;
        }
    }
}
