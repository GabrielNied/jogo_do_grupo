using JogoDoGrupo.Shared.Features.Profissoes;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Profissoes
{
    /// <summary>
    /// Os marcadores procedurais dos pontos de coleta e da forja, até haver modelo (mesmo
    /// caminho dos NPCs de economia). Só desenho: quem decide a coleta é o servidor.
    /// </summary>
    internal static class MarcadorDeProfissoes
    {
        public static GameObject CriarPonto(PontoDeColeta ponto, Vector3 posicao)
        {
            var raiz = new GameObject("Coleta." + ponto.Nome + "." + ponto.Id);
            raiz.transform.position = posicao;

            switch (ponto.MaterialId)
            {
                case ConfigProfissoes.MaterialMadeira:
                    Parte(
                        raiz,
                        PrimitiveType.Cylinder,
                        new Vector3(0.5f, 1.2f, 0.5f),
                        new Vector3(0f, 1.2f, 0f),
                        new Color(0.40f, 0.26f, 0.14f)
                    );
                    Parte(
                        raiz,
                        PrimitiveType.Sphere,
                        new Vector3(2.2f, 2.0f, 2.2f),
                        new Vector3(0f, 3.0f, 0f),
                        new Color(0.20f, 0.50f, 0.22f)
                    );
                    break;
                case ConfigProfissoes.MaterialErvaMedicinal:
                    Parte(
                        raiz,
                        PrimitiveType.Sphere,
                        new Vector3(1.2f, 0.8f, 1.2f),
                        new Vector3(0f, 0.4f, 0f),
                        new Color(0.25f, 0.65f, 0.35f)
                    );
                    Parte(
                        raiz,
                        PrimitiveType.Sphere,
                        new Vector3(0.4f, 0.4f, 0.4f),
                        new Vector3(0.2f, 0.8f, 0.1f),
                        new Color(0.85f, 0.45f, 0.80f)
                    );
                    break;
                case ConfigProfissoes.MaterialGraos:
                    Parte(
                        raiz,
                        PrimitiveType.Cylinder,
                        new Vector3(0.3f, 0.9f, 0.3f),
                        new Vector3(-0.2f, 0.45f, 0f),
                        new Color(0.88f, 0.75f, 0.30f)
                    );
                    Parte(
                        raiz,
                        PrimitiveType.Cylinder,
                        new Vector3(0.3f, 0.9f, 0.3f),
                        new Vector3(0.2f, 0.45f, 0f),
                        new Color(0.92f, 0.80f, 0.35f)
                    );
                    break;
                case ConfigProfissoes.MaterialCristalArcano:
                    Parte(
                        raiz,
                        PrimitiveType.Cube,
                        new Vector3(0.6f, 1.4f, 0.6f),
                        new Vector3(0f, 0.7f, 0f),
                        new Color(0.25f, 0.75f, 0.95f)
                    );
                    Parte(
                        raiz,
                        PrimitiveType.Cube,
                        new Vector3(0.4f, 0.9f, 0.4f),
                        new Vector3(0.3f, 0.45f, 0.2f),
                        new Color(0.70f, 0.40f, 0.95f)
                    );
                    break;
                case ConfigProfissoes.MaterialMinerioDeFerro:
                    Parte(
                        raiz,
                        PrimitiveType.Sphere,
                        new Vector3(1.6f, 1.2f, 1.4f),
                        new Vector3(0f, 0.5f, 0f),
                        new Color(0.42f, 0.42f, 0.46f)
                    );
                    Parte(
                        raiz,
                        PrimitiveType.Sphere,
                        new Vector3(0.6f, 0.5f, 0.6f),
                        new Vector3(0.5f, 1.0f, 0.2f),
                        new Color(0.80f, 0.50f, 0.25f)
                    );
                    break;
                case ConfigProfissoes.MaterialCarneDeCaca:
                    Parte(
                        raiz,
                        PrimitiveType.Cube,
                        new Vector3(1.0f, 0.3f, 0.7f),
                        new Vector3(0f, 0.15f, 0f),
                        new Color(0.60f, 0.25f, 0.20f)
                    );
                    Parte(
                        raiz,
                        PrimitiveType.Cylinder,
                        new Vector3(0.2f, 0.6f, 0.2f),
                        new Vector3(0.2f, 0.4f, 0.1f),
                        new Color(0.85f, 0.85f, 0.80f)
                    );
                    break;
                default:
                    Parte(
                        raiz,
                        PrimitiveType.Cube,
                        new Vector3(1.4f, 0.4f, 0.8f),
                        new Vector3(0f, 0.2f, 0f),
                        new Color(0.55f, 0.36f, 0.24f)
                    );
                    break;
            }

            return raiz;
        }

        public static GameObject CriarForja(Vector3 posicao)
        {
            var raiz = new GameObject("Profissoes.Forja");
            raiz.transform.position = posicao;
            var ferro = new Color(0.25f, 0.26f, 0.30f);
            Parte(
                raiz,
                PrimitiveType.Cube,
                new Vector3(1.2f, 0.9f, 0.7f),
                new Vector3(0f, 0.45f, 0f),
                ferro
            );
            Parte(
                raiz,
                PrimitiveType.Cube,
                new Vector3(1.6f, 0.25f, 0.8f),
                new Vector3(0f, 1.0f, 0f),
                new Color(0.55f, 0.57f, 0.62f)
            );
            Parte(
                raiz,
                PrimitiveType.Sphere,
                new Vector3(0.45f, 0.3f, 0.45f),
                new Vector3(0.9f, 0.2f, 0f),
                new Color(0.95f, 0.45f, 0.15f)
            );
            return raiz;
        }

        public static GameObject CriarCurtume(Vector3 posicao)
        {
            var raiz = new GameObject("Profissoes.Curtume");
            raiz.transform.position = posicao;
            Parte(
                raiz,
                PrimitiveType.Cube,
                new Vector3(1.4f, 0.8f, 0.8f),
                new Vector3(0f, 0.4f, 0f),
                new Color(0.48f, 0.32f, 0.18f)
            );
            Parte(
                raiz,
                PrimitiveType.Cube,
                new Vector3(1.6f, 0.15f, 1.0f),
                new Vector3(0f, 0.85f, 0f),
                new Color(0.62f, 0.45f, 0.28f)
            );
            return raiz;
        }

        public static GameObject CriarCaldeirao(Vector3 posicao)
        {
            var raiz = new GameObject("Profissoes.Caldeirao");
            raiz.transform.position = posicao;
            Parte(
                raiz,
                PrimitiveType.Cylinder,
                new Vector3(1.0f, 0.6f, 1.0f),
                new Vector3(0f, 0.35f, 0f),
                new Color(0.20f, 0.22f, 0.25f)
            );
            Parte(
                raiz,
                PrimitiveType.Sphere,
                new Vector3(0.8f, 0.3f, 0.8f),
                new Vector3(0f, 0.65f, 0f),
                new Color(0.25f, 0.85f, 0.70f)
            );
            return raiz;
        }

        public static GameObject CriarCozinha(Vector3 posicao)
        {
            var raiz = new GameObject("Profissoes.Cozinha");
            raiz.transform.position = posicao;
            Parte(
                raiz,
                PrimitiveType.Cube,
                new Vector3(1.4f, 0.8f, 0.9f),
                new Vector3(0f, 0.4f, 0f),
                new Color(0.65f, 0.60f, 0.55f)
            );
            Parte(
                raiz,
                PrimitiveType.Cylinder,
                new Vector3(0.6f, 0.3f, 0.6f),
                new Vector3(0.2f, 0.95f, 0f),
                new Color(0.85f, 0.45f, 0.20f)
            );
            return raiz;
        }

        public static bool EstaPerto(Vector3 jogador, Vector3 marcador)
        {
            var dx = jogador.x - marcador.x;
            var dz = jogador.z - marcador.z;
            var alcance = ConfigProfissoes.AlcanceDaDicaMetros;
            return dx * dx + dz * dz <= alcance * alcance;
        }

        private static void Parte(
            GameObject raiz,
            PrimitiveType tipo,
            Vector3 escala,
            Vector3 posicaoLocal,
            Color cor
        )
        {
            var parte = GameObject.CreatePrimitive(tipo);
            parte.transform.SetParent(raiz.transform, false);
            parte.transform.localScale = escala;
            parte.transform.localPosition = posicaoLocal;
            // Sem colisor: o personagem não trava em marcador (a coleta é por distância).
            Object.Destroy(parte.GetComponent<Collider>());
            var renderer = parte.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = cor;
        }
    }
}
