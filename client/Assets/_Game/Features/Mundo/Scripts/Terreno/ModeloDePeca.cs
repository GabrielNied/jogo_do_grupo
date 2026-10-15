using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>Um pedaço desenhável do modelo: malha, submalha, material e posição relativa à raiz.</summary>
    public readonly struct ParteDeModelo
    {
        public ParteDeModelo(Mesh malha, int submalha, Material material, Matrix4x4 local)
        {
            Malha = malha;
            Submalha = submalha;
            Material = material;
            Local = local;
        }

        public Mesh Malha { get; }

        public int Submalha { get; }

        public Material Material { get; }

        /// <summary>Do espaço da malha para o da raiz da peça.</summary>
        public Matrix4x4 Local { get; }
    }

    /// <summary>
    /// Colisor do tronco ou do corpo da peça, no espaço da raiz (antes da escala da peça): uma
    /// cápsula em pé (<see cref="Raio"/> e <see cref="Altura"/>) ou, com <see cref="EhCaixa"/>,
    /// uma caixa de tamanho <see cref="Tamanho"/>.
    /// </summary>
    public readonly struct ColisorDaPeca
    {
        public ColisorDaPeca(Vector3 centro, float raio, float altura)
        {
            Centro = centro;
            Raio = raio;
            Altura = altura;
            Tamanho = Vector3.zero;
            EhCaixa = false;
        }

        public ColisorDaPeca(Vector3 centro, Vector3 tamanho)
        {
            Centro = centro;
            Raio = 0f;
            Altura = 0f;
            Tamanho = tamanho;
            EhCaixa = true;
        }

        public Vector3 Centro { get; }

        public float Raio { get; }

        public float Altura { get; }

        public Vector3 Tamanho { get; }

        public bool EhCaixa { get; }

        /// <summary>Põe o colisor em <paramref name="objeto"/>.</summary>
        public void Aplicar(GameObject objeto)
        {
            if (EhCaixa)
            {
                var caixa = objeto.AddComponent<BoxCollider>();
                caixa.center = Centro;
                caixa.size = Tamanho;
                return;
            }

            var capsula = objeto.AddComponent<CapsuleCollider>();
            capsula.center = Centro;
            capsula.radius = Raio;
            capsula.height = Altura;
        }
    }

    /// <summary>
    /// O que há de comum a todas as peças de um tipo (Pinheiro, Arvore, Pedra_Grande...): as
    /// partes desenháveis, que dividem malha e material, e o colisor. Cada peça no mundo é só uma
    /// posição, guinada e escala em cima deste modelo.
    /// </summary>
    public sealed class ModeloDePeca
    {
        public ModeloDePeca(
            string nome,
            ParteDeModelo[] partes,
            ColisorDaPeca? colisor,
            bool pequena
        )
        {
            Nome = nome;
            Partes = partes;
            Colisor = colisor;
            Pequena = pequena;
        }

        public string Nome { get; }

        public ParteDeModelo[] Partes { get; }

        /// <summary>Colisor da peça, ou <c>null</c> se dá para atravessar (arbusto).</summary>
        public ColisorDaPeca? Colisor { get; }

        /// <summary>Peça que some mais perto da câmera que as grandes.</summary>
        public bool Pequena { get; }
    }
}
