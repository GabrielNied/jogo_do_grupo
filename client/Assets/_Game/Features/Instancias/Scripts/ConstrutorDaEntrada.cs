using JogoDoGrupo.Shared.Features.Instancias;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JogoDoGrupo.Client.Features.Instancias
{
    /// <summary>
    /// A porta da Mina do Reflexo na Serra do Vidro Frio: um arco de pedra coberto de neve, um
    /// véu azul, cristais de gelo e orbes de luz, de frente para a cidade do passo, montados por
    /// código (sem modelo de dungeon).
    /// </summary>
    internal static class ConstrutorDaEntrada
    {
        public const string NomeRaiz = "EntradaMinaDoReflexo";

        private static readonly Color CorDaPedra = new Color(0.36f, 0.37f, 0.4f);
        private static readonly Color CorDaNeve = new Color(0.92f, 0.95f, 1f);
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
            // De frente para a cidade do passo, de onde o grupo chega.
            var paraACidade = new Vector3(
                LayoutDaMinaDoReflexo.OlharParaX - LayoutDaMinaDoReflexo.EntradaX,
                0f,
                LayoutDaMinaDoReflexo.OlharParaZ - LayoutDaMinaDoReflexo.EntradaZ
            );
            raiz.transform.rotation = Quaternion.LookRotation(paraACidade.normalized);

            var pedra = AmbientacaoDoReflexo.MaterialUrp(CorDaPedra);
            var neve = AmbientacaoDoReflexo.MaterialUrp(CorDaNeve);
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
            // Neve acumulada sobre a verga e as cabeças dos pilares.
            Bloco(
                raiz.transform,
                neve,
                "NeveDaVerga",
                new Vector3(0f, 5.2f, 0f),
                new Vector3(6.2f, 0.25f, 1.7f),
                false
            );
            Bloco(
                raiz.transform,
                neve,
                "NeveDoPilarEsquerdo",
                new Vector3(-2.2f, 4.3f, 0f),
                new Vector3(1.4f, 0.2f, 1.4f),
                false
            );
            Bloco(
                raiz.transform,
                neve,
                "NeveDoPilarDireito",
                new Vector3(2.2f, 4.3f, 0f),
                new Vector3(1.4f, 0.2f, 1.4f),
                false
            );

            // O véu não tem colisor: a entrada é pela interação, não por esbarrar nele.
            var veu = AmbientacaoDoReflexo.MaterialUrp(CorDoVeu);
            Bloco(
                raiz.transform,
                veu,
                "Veu",
                new Vector3(0f, 2.1f, 0f),
                new Vector3(3.2f, 4f, 0.1f),
                false
            );

            MontarCristais(raiz.transform);

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

        // Cristais de gelo nos dois lados da porta e um orbe de luz sobre cada pilar.
        private static void MontarCristais(Transform raiz)
        {
            var gelo = AmbientacaoDoReflexo.MaterialDeCristal(AmbientacaoDoReflexo.CorDoGelo, 1.2f);
            var orbe = AmbientacaoDoReflexo.MaterialDeCristal(AmbientacaoDoReflexo.CorDoOrbe, 3f);
            var lados = new[] { -1f, 1f };
            foreach (var lado in lados)
            {
                var cachos = new[]
                {
                    new Vector4(3.6f, 0.3f, 2.4f, 12f),
                    new Vector4(4.5f, -0.5f, 1.6f, -8f),
                    new Vector4(5.2f, 0.8f, 1.1f, 18f),
                };
                for (var i = 0; i < cachos.Length; i++)
                {
                    var c = cachos[i];
                    var local = new Vector3(lado * c.x, 0f, c.y);
                    var mundo = raiz.TransformPoint(local);
                    mundo.y = LayoutDaMinaDoReflexo.AlturaDoChao(mundo.x, mundo.z) - 0.1f;
                    AmbientacaoDoReflexo.Cristal(
                        raiz,
                        gelo,
                        mundo,
                        c.z,
                        c.z * 0.4f,
                        lado * c.w,
                        i * 47f
                    );
                }

                var acima = raiz.TransformPoint(new Vector3(lado * 2.2f, 5.9f, 0.4f));
                AmbientacaoDoReflexo.Orbe(
                    raiz,
                    orbe,
                    acima,
                    0.25f,
                    AmbientacaoDoReflexo.CorDoOrbe,
                    1.5f,
                    8f
                );
            }
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
    }
}
