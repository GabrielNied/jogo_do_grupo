using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// O que o Core monta quando nenhuma funcionalidade registrou o mundo ou o visual do jogador
    /// (ou quando o registrado falhou): um chão com alguns marcos, uma luz e uma cápsula no lugar
    /// do personagem. Serve para o jogo nunca travar na entrada.
    /// </summary>
    internal static class MundoPadrao
    {
        public const string NomeMundo = "MundoPadrao";
        public const string NomeCapsula = "VisualPadrao";

        /// <summary>Lado do chão, em metros.</summary>
        public const float LadoChao = 100f;

        /// <summary>Altura da cápsula, em metros (a mesma do jogador).</summary>
        public const float AlturaCapsula = 1.8f;

        private const float RaioCapsula = 0.35f;

        /// <summary>
        /// Cria o chão (Y = 0, centrado na origem), marcos para perceber o movimento e uma luz
        /// direcional, tudo sob um objeto <see cref="NomeMundo"/>, que é devolvido.
        /// </summary>
        public static GameObject CriarMundo()
        {
            var raiz = new GameObject(NomeMundo);

            // O plano do Unity tem 10 x 10 m na escala 1.
            var chao = GameObject.CreatePrimitive(PrimitiveType.Plane);
            chao.name = "Chao";
            chao.transform.SetParent(raiz.transform, false);
            chao.transform.localScale = new Vector3(LadoChao / 10f, 1f, LadoChao / 10f);

            // Alguns blocos espalhados, para dar referência de movimento e de colisão.
            for (var i = 0; i < 8; i++)
            {
                var angulo = i * 45f;
                var bloco = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bloco.name = $"Marco{i + 1}";
                bloco.transform.SetParent(raiz.transform, false);
                bloco.transform.localPosition =
                    Quaternion.Euler(0f, angulo, 0f) * new Vector3(0f, 1f, 12f);
                bloco.transform.localScale = new Vector3(1.5f, 2f, 1.5f);
            }

            var luz = new GameObject("Luz");
            luz.transform.SetParent(raiz.transform, false);
            luz.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
            var componente = luz.AddComponent<Light>();
            componente.type = LightType.Directional;
            componente.shadows = LightShadows.Soft;

            return raiz;
        }

        /// <summary>
        /// Cria uma cápsula do tamanho do jogador, com os pés no Y = 0 local e sem colisor
        /// (quem colide é o CharacterController do jogador).
        /// </summary>
        public static GameObject CriarCapsula()
        {
            // A cápsula do Unity tem 2 m de altura e 0,5 m de raio na escala 1.
            var capsula = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var colisor = capsula.GetComponent<Collider>();
            if (colisor != null)
            {
                Object.DestroyImmediate(colisor);
            }

            var raiz = new GameObject(NomeCapsula);
            capsula.name = "Capsula";
            capsula.transform.SetParent(raiz.transform, false);
            capsula.transform.localPosition = new Vector3(0f, AlturaCapsula / 2f, 0f);
            capsula.transform.localScale = new Vector3(
                RaioCapsula * 2f,
                AlturaCapsula / 2f,
                RaioCapsula * 2f
            );

            // Um "nariz" na frente (+Z), para ver para onde o jogador olha.
            var nariz = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nariz.name = "Frente";
            var colisorNariz = nariz.GetComponent<Collider>();
            if (colisorNariz != null)
            {
                Object.DestroyImmediate(colisorNariz);
            }

            nariz.transform.SetParent(raiz.transform, false);
            nariz.transform.localPosition = new Vector3(0f, AlturaCapsula * 0.85f, RaioCapsula);
            nariz.transform.localScale = new Vector3(0.2f, 0.1f, 0.2f);

            return raiz;
        }
    }
}
