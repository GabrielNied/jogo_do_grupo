using JogoDoGrupo.Shared.Features.Instancias;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JogoDoGrupo.Client.Features.Instancias
{
    /// <summary>
    /// A porta da Mina do Reflexo na colina oeste: um arco de pedra e um véu azul, de frente
    /// para a cidade, montados por código (sem modelo de dungeon ainda; a arte final é da #329).
    /// </summary>
    internal static class ConstrutorDaEntrada
    {
        public const string NomeRaiz = "EntradaMinaDoReflexo";

        private static readonly Color CorDaPedra = new Color(0.42f, 0.38f, 0.34f);
        private static readonly Color CorDoVeu = new Color(0.35f, 0.7f, 1f);

        public static Transform Montar(Scene cena)
        {
            var raiz = new GameObject(NomeRaiz);
            SceneManager.MoveGameObjectToScene(raiz, cena);

            raiz.transform.position = new Vector3(
                LayoutDaMinaDoReflexo.EntradaX,
                LayoutDaMinaDoReflexo.EntradaY,
                LayoutDaMinaDoReflexo.EntradaZ
            );
            // De frente para a cidade (o centro do mundo).
            var paraACidade = new Vector3(
                -LayoutDaMinaDoReflexo.EntradaX,
                0f,
                -LayoutDaMinaDoReflexo.EntradaZ
            );
            raiz.transform.rotation = Quaternion.LookRotation(paraACidade.normalized);

            var pedra = CriarMaterial(CorDaPedra);
            Bloco(
                raiz.transform,
                pedra,
                "PilarEsquerdo",
                new Vector3(-2.2f, 2f, 0f),
                new Vector3(1.2f, 4.4f, 1.2f),
                true
            );
            Bloco(
                raiz.transform,
                pedra,
                "PilarDireito",
                new Vector3(2.2f, 2f, 0f),
                new Vector3(1.2f, 4.4f, 1.2f),
                true
            );
            Bloco(
                raiz.transform,
                pedra,
                "Verga",
                new Vector3(0f, 4.6f, 0f),
                new Vector3(5.8f, 1f, 1.4f),
                true
            );
            Bloco(
                raiz.transform,
                pedra,
                "Soleira",
                new Vector3(0f, 0.1f, 0f),
                new Vector3(3.4f, 0.2f, 1.6f),
                false
            );

            // O véu não tem colisor: a entrada é pela interação, não por esbarrar nele.
            var veu = CriarMaterial(CorDoVeu);
            Bloco(
                raiz.transform,
                veu,
                "Veu",
                new Vector3(0f, 2.1f, 0f),
                new Vector3(3.2f, 4f, 0.1f),
                false
            );

            var luz = new GameObject("Luz");
            luz.transform.SetParent(raiz.transform, false);
            luz.transform.localPosition = new Vector3(0f, 2.5f, 1.2f);
            var componente = luz.AddComponent<Light>();
            componente.type = LightType.Point;
            componente.color = CorDoVeu;
            componente.intensity = 2.5f;
            componente.range = 9f;
            return raiz.transform;
        }

        private static void Bloco(
            Transform pai,
            Material material,
            string nome,
            Vector3 local,
            Vector3 escala,
            bool comColisor
        )
        {
            var bloco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bloco.name = nome;
            bloco.transform.SetParent(pai, false);
            bloco.transform.localPosition = local;
            bloco.transform.localScale = escala;
            bloco.GetComponent<Renderer>().sharedMaterial = material;
            if (!comColisor)
            {
                Object.Destroy(bloco.GetComponent<Collider>());
            }
        }

        private static Material CriarMaterial(Color cor)
        {
            var modelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var material = new Material(modelo.GetComponent<Renderer>().sharedMaterial)
            {
                color = cor,
            };
            Object.Destroy(modelo);
            return material;
        }
    }
}
