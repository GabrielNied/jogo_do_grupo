using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens.Visual
{
    /// <summary>
    /// Os modelos de arma, um por tipo: prefabs CC0 em <c>Resources/Itens/Modelos/Arma_&lt;Tipo&gt;</c>
    /// (Quaternius, gerados por <c>ModelosDeItensImportador</c> no editor, #1188). Cada um tem o
    /// cabo na origem e aponta para +Y, com tudo em metros: assim o encaixe da mão ou das costas só
    /// precisa pôr a origem no lugar certo. Sem o prefab (build sem o gerador rodado), o desenho de primitivas em código é a
    /// reserva.
    /// </summary>
    public static class ModelosDeArma
    {
        private static readonly Color Metal = new Color(0.78f, 0.8f, 0.85f);
        private static readonly Color Madeira = new Color(0.45f, 0.28f, 0.13f);
        private static readonly Color Couro = new Color(0.25f, 0.16f, 0.1f);
        private static readonly Color Ouro = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color Cristal = new Color(0.4f, 0.75f, 1f);

        /// <summary>Se o tipo tem modelo (as quatro armas principais).</summary>
        public static bool Tem(TipoEquipamento tipo) =>
            tipo == TipoEquipamento.Espada
            || tipo == TipoEquipamento.Martelo
            || tipo == TipoEquipamento.Arco
            || tipo == TipoEquipamento.Cajado;

        /// <summary>Comprimento do modelo do cabo à ponta, em metros (0 se o tipo não tem).</summary>
        public static float Comprimento(TipoEquipamento tipo)
        {
            switch (tipo)
            {
                case TipoEquipamento.Espada:
                    return 1.1f;
                case TipoEquipamento.Martelo:
                    return 0.9f;
                case TipoEquipamento.Arco:
                    return 1.1f;
                case TipoEquipamento.Cajado:
                    return 1.5f;
                default:
                    return 0f;
            }
        }

        /// <summary>Caminho em <c>Resources</c> do prefab do tipo.</summary>
        public static string CaminhoDoModelo(TipoEquipamento tipo) => "Itens/Modelos/Arma_" + tipo;

        /// <summary>Cria o modelo (sem colisor), ou <c>null</c> se o tipo não tem.</summary>
        public static GameObject Criar(TipoEquipamento tipo)
        {
            if (!Tem(tipo))
            {
                return null;
            }

            var prefab = Resources.Load<GameObject>(CaminhoDoModelo(tipo));
            if (prefab != null)
            {
                var modelo = Object.Instantiate(prefab);
                modelo.name = "Arma_" + tipo;
                return modelo;
            }

            return CriarDePrimitivas(tipo);
        }

        private static GameObject CriarDePrimitivas(TipoEquipamento tipo)
        {
            var raiz = new GameObject("Arma_" + tipo);
            var materiais = raiz.AddComponent<MateriaisDoBau>();
            switch (tipo)
            {
                case TipoEquipamento.Espada:
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cylinder,
                        "Cabo",
                        V(0f, 0.08f, 0f),
                        V(0.035f, 0.08f, 0.035f),
                        Couro
                    );
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cube,
                        "Guarda",
                        V(0f, 0.17f, 0f),
                        V(0.24f, 0.035f, 0.05f),
                        Ouro
                    );
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cube,
                        "Lamina",
                        V(0f, 0.62f, 0f),
                        V(0.07f, 0.9f, 0.018f),
                        Metal
                    );
                    break;
                case TipoEquipamento.Martelo:
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cylinder,
                        "Cabo",
                        V(0f, 0.35f, 0f),
                        V(0.04f, 0.4f, 0.04f),
                        Madeira
                    );
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cube,
                        "Cabeca",
                        V(0f, 0.78f, 0f),
                        V(0.32f, 0.18f, 0.18f),
                        Metal
                    );
                    break;
                case TipoEquipamento.Arco:
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cube,
                        "Centro",
                        V(0f, 0.45f, 0f),
                        V(0.04f, 0.25f, 0.04f),
                        Madeira
                    );
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cube,
                        "Haste superior",
                        V(0.05f, 0.82f, 0f),
                        V(0.03f, 0.5f, 0.03f),
                        Madeira,
                        15f
                    );
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cube,
                        "Haste inferior",
                        V(0.05f, 0.08f, 0f),
                        V(0.03f, 0.5f, 0.03f),
                        Madeira,
                        -15f
                    );
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cube,
                        "Corda",
                        V(-0.04f, 0.45f, 0f),
                        V(0.008f, 0.95f, 0.008f),
                        Metal
                    );
                    break;
                case TipoEquipamento.Cajado:
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cylinder,
                        "Haste",
                        V(0f, 0.5f, 0f),
                        V(0.035f, 0.85f, 0.035f),
                        Madeira
                    );
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Sphere,
                        "Gema",
                        V(0f, 1.4f, 0f),
                        V(0.16f, 0.16f, 0.16f),
                        Cristal
                    );
                    break;
            }

            return raiz;
        }

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        // Os primitivos do Unity têm 1 m de lado (cubo, esfera) e 2 m de altura (cilindro); a
        // escala aqui já é a final (o cilindro vem com a metade da altura em y).
        private static void Parte(
            GameObject pai,
            MateriaisDoBau materiais,
            PrimitiveType forma,
            string nome,
            Vector3 posicao,
            Vector3 escala,
            Color cor,
            float giroZ = 0f
        )
        {
            var parte = GameObject.CreatePrimitive(forma);
            parte.name = nome;
            var colisor = parte.GetComponent<Collider>();
            if (Application.isPlaying)
            {
                Object.Destroy(colisor);
            }
            else
            {
                Object.DestroyImmediate(colisor);
            }

            parte.transform.SetParent(pai.transform, false);
            parte.transform.localPosition = posicao;
            parte.transform.localRotation = Quaternion.Euler(0f, 0f, giroZ);
            parte.transform.localScale = escala;

            var renderizador = parte.GetComponent<Renderer>();
            var material = MateriaisSimples.Criar(cor);
            renderizador.sharedMaterial = material;
            materiais.Registrar(material);
        }
    }
}
