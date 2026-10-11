using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>
    /// Silhuetas que o kit da cidade não tem: cacto, árvore seca, pinheiro, rocha vulcânica,
    /// árvore de pântano e arbusto. Nascem em código, na altura do chão, sem colisor (não
    /// atravessam o terreno nem travam o passo).
    /// </summary>
    public static class VegetacaoProcedural
    {
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

        public static GameObject Criar(string nome, Vector3 posicao, float guinada, Transform pai)
        {
            var raiz = new GameObject(nome);
            raiz.transform.SetParent(pai, false);
            raiz.transform.SetLocalPositionAndRotation(posicao, Quaternion.Euler(0f, guinada, 0f));

            switch (nome)
            {
                case "Cacto":
                    Tronco(
                        raiz.transform,
                        new Vector3(0f, 1.1f, 0f),
                        new Vector3(0.35f, 1.1f, 0.35f),
                        Verde(0.35f, 0.55f, 0.28f)
                    );
                    Tronco(
                        raiz.transform,
                        new Vector3(0.35f, 1.3f, 0f),
                        new Vector3(0.18f, 0.45f, 0.18f),
                        Verde(0.4f, 0.6f, 0.3f)
                    );
                    break;
                case "Arvore_Seca":
                    Tronco(
                        raiz.transform,
                        new Vector3(0f, 1.2f, 0f),
                        new Vector3(0.12f, 1.2f, 0.12f),
                        Marrom
                    );
                    Tronco(
                        raiz.transform,
                        new Vector3(0.35f, 2f, 0f),
                        new Vector3(0.5f, 0.06f, 0.06f),
                        Marrom
                    );
                    break;
                case "Pinheiro":
                    Tronco(
                        raiz.transform,
                        new Vector3(0f, 0.6f, 0f),
                        new Vector3(0.15f, 0.6f, 0.15f),
                        Marrom
                    );
                    Tronco(
                        raiz.transform,
                        new Vector3(0f, 1.5f, 0f),
                        new Vector3(0.9f, 0.45f, 0.9f),
                        Verde(0.2f, 0.4f, 0.28f)
                    );
                    Tronco(
                        raiz.transform,
                        new Vector3(0f, 2.2f, 0f),
                        new Vector3(0.55f, 0.4f, 0.55f),
                        Verde(0.25f, 0.48f, 0.32f)
                    );
                    break;
                case "Rocha_Vulcanica":
                    Tronco(
                        raiz.transform,
                        new Vector3(0f, 0.4f, 0f),
                        new Vector3(1.1f, 0.5f, 0.8f),
                        new Color(0.18f, 0.12f, 0.1f)
                    );
                    Tronco(
                        raiz.transform,
                        new Vector3(0.35f, 0.7f, 0.1f),
                        new Vector3(0.5f, 0.35f, 0.45f),
                        new Color(0.28f, 0.14f, 0.1f)
                    );
                    break;
                case "Arvore_Pantano":
                    Tronco(
                        raiz.transform,
                        new Vector3(0.15f, 1f, 0f),
                        new Vector3(0.16f, 1.1f, 0.16f),
                        new Color(0.28f, 0.22f, 0.14f)
                    );
                    Tronco(
                        raiz.transform,
                        new Vector3(0.2f, 2f, 0.1f),
                        new Vector3(0.7f, 0.45f, 0.7f),
                        Verde(0.22f, 0.32f, 0.2f)
                    );
                    break;
                default:
                    Tronco(
                        raiz.transform,
                        new Vector3(0f, 0.35f, 0f),
                        new Vector3(0.8f, 0.4f, 0.8f),
                        Verde(0.3f, 0.5f, 0.22f)
                    );
                    break;
            }

            return raiz;
        }

        private static readonly Color Marrom = new Color(0.4f, 0.28f, 0.16f);

        private static Color Verde(float r, float g, float b) => new Color(r, g, b);

        private static void Tronco(Transform pai, Vector3 local, Vector3 escala, Color cor)
        {
            var bloco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bloco.name = "Bloco";
            bloco.transform.SetParent(pai, false);
            bloco.transform.localPosition = local;
            bloco.transform.localScale = escala;
            Object.DestroyImmediate(bloco.GetComponent<Collider>());

            var renderizador = bloco.GetComponent<Renderer>();
            var material = MaterialBase();
            if (material != null)
            {
                var copia = new Material(material) { name = "Vegetacao" };
                if (copia.HasProperty("_BaseColor"))
                {
                    copia.SetColor("_BaseColor", cor);
                }
                else if (copia.HasProperty("_Color"))
                {
                    copia.SetColor("_Color", cor);
                }

                renderizador.sharedMaterial = copia;
            }
        }

        private static Material _base;

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
