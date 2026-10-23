using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens.Visual
{
    /// <summary>
    /// Os modelos de armadura e acessórios (capacete e peito): prefabs CC0 em
    /// <c>Resources/Itens/Modelos/Armadura_&lt;Tipo&gt;</c> (gerados por <c>ModelosDeItensImportador</c>
    /// no editor, #1197), com a origem centralizada no encaixe correspondente (cabeça ou tronco) e
    /// a face para +Z. Sem o prefab (build sem o gerador rodado), o desenho de primitivas em código
    /// é a reserva.
    /// </summary>
    public static class ModelosDeArmadura
    {
        private static readonly Color Metal = new Color(0.75f, 0.78f, 0.82f);
        private static readonly Color Couro = new Color(0.35f, 0.22f, 0.12f);
        private static readonly Color Ouro = new Color(0.9f, 0.72f, 0.18f);
        private static readonly Color AcoEscuro = new Color(0.28f, 0.3f, 0.35f);

        /// <summary>Se o tipo tem modelo 3D implementado (Capacete e Peito).</summary>
        public static bool Tem(TipoEquipamento tipo) =>
            tipo == TipoEquipamento.Capacete || tipo == TipoEquipamento.Peito;

        /// <summary>Caminho em <c>Resources</c> do prefab do tipo.</summary>
        public static string CaminhoDoModelo(TipoEquipamento tipo) =>
            "Itens/Modelos/Armadura_" + tipo;

        /// <summary>Cria o modelo da peça no tamanho do avatar, ou <c>null</c> se não implementado.</summary>
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
                modelo.name = "Armadura_" + tipo;
                return modelo;
            }

            return CriarDePrimitivas(tipo);
        }

        private static GameObject CriarDePrimitivas(TipoEquipamento tipo)
        {
            var raiz = new GameObject("Armadura_" + tipo);
            var materiais = raiz.AddComponent<MateriaisDoBau>();

            switch (tipo)
            {
                case TipoEquipamento.Capacete:
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Sphere,
                        "Cupula",
                        V(0f, 0.04f, 0f),
                        V(0.30f, 0.28f, 0.32f),
                        Metal
                    );
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cube,
                        "Visor",
                        V(0f, 0.01f, 0.14f),
                        V(0.24f, 0.06f, 0.08f),
                        Ouro
                    );
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cube,
                        "Crista",
                        V(0f, 0.17f, -0.02f),
                        V(0.04f, 0.10f, 0.26f),
                        AcoEscuro
                    );
                    break;

                case TipoEquipamento.Peito:
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cube,
                        "Peitoral",
                        V(0f, 0f, 0.06f),
                        V(0.40f, 0.44f, 0.22f),
                        Metal
                    );
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cube,
                        "Dorsal",
                        V(0f, 0f, -0.06f),
                        V(0.38f, 0.42f, 0.14f),
                        Couro
                    );
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cube,
                        "Ombreira_E",
                        V(0.25f, 0.18f, 0f),
                        V(0.16f, 0.08f, 0.18f),
                        Ouro,
                        -15f
                    );
                    Parte(
                        raiz,
                        materiais,
                        PrimitiveType.Cube,
                        "Ombreira_D",
                        V(-0.25f, 0.18f, 0f),
                        V(0.16f, 0.08f, 0.18f),
                        Ouro,
                        15f
                    );
                    break;
            }

            return raiz;
        }

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

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
