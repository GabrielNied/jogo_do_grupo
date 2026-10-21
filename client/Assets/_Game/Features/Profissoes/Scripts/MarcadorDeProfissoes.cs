using JogoDoGrupo.Shared.Features.Profissoes;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Profissoes
{
    /// <summary>
    /// O desenho dos pontos de coleta e das bancadas. Os pontos usam os modelos da Mata
    /// (Quaternius, CC0) quando existem, com um detalhe que diz o material (veio de minério,
    /// cristais); sem modelo, ficam as formas simples. Só desenho: quem decide a coleta é o
    /// servidor.
    /// </summary>
    internal static class MarcadorDeProfissoes
    {
        private const string PastaDaMata = "Mundo/MataEstilizada/";

        public static GameObject CriarPonto(PontoDeColeta ponto, Vector3 posicao)
        {
            var raiz = new GameObject("Coleta." + ponto.Nome + "." + ponto.Id);
            raiz.transform.position = posicao;
            if (CriarComModelo(raiz, ponto))
            {
                return raiz;
            }

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

        // Cada material com o modelo que lembra a coleta; o giro varia por ponto para os dois
        // pontos do mesmo material não ficarem iguais.
        private static bool CriarComModelo(GameObject raiz, PontoDeColeta ponto)
        {
            var giro = ponto.Id * 67f;
            switch (ponto.MaterialId)
            {
                case ConfigProfissoes.MaterialMadeira:
                    return Modelo(raiz, "CommonTree_3", 1f, giro) != null;
                case ConfigProfissoes.MaterialErvaMedicinal:
                    return Modelo(raiz, "Bush_Common_Flowers", 1.2f, giro) != null;
                case ConfigProfissoes.MaterialGraos:
                case ConfigProfissoes.MaterialCarneDeCaca:
                    return Modelo(raiz, "Bush_Common", 1.1f, giro) != null;
                case ConfigProfissoes.MaterialCouroCru:
                    if (Modelo(raiz, "Rock_Medium_3", 0.7f, giro) == null)
                    {
                        return false;
                    }

                    // A carcaça: o couro estendido sobre a pedra.
                    Parte(
                        raiz,
                        PrimitiveType.Cube,
                        new Vector3(1.1f, 0.12f, 0.7f),
                        new Vector3(0f, 0.7f, 0f),
                        new Color(0.55f, 0.38f, 0.24f)
                    );
                    return true;
                case ConfigProfissoes.MaterialMinerioDeFerro:
                    if (Modelo(raiz, "Rock_Medium_1", 1f, giro) == null)
                    {
                        return false;
                    }

                    // Veio de minério: pedras cor de ferrugem saindo da rocha.
                    Parte(
                        raiz,
                        PrimitiveType.Cube,
                        new Vector3(0.35f, 0.3f, 0.3f),
                        new Vector3(0.45f, 0.75f, 0.2f),
                        new Color(0.72f, 0.42f, 0.2f)
                    );
                    Parte(
                        raiz,
                        PrimitiveType.Cube,
                        new Vector3(0.25f, 0.22f, 0.25f),
                        new Vector3(-0.35f, 0.6f, 0.35f),
                        new Color(0.62f, 0.36f, 0.18f)
                    );
                    return true;
                case ConfigProfissoes.MaterialCristalArcano:
                    if (Modelo(raiz, "Rock_Medium_2", 0.9f, giro) == null)
                    {
                        return false;
                    }

                    Parte(
                        raiz,
                        PrimitiveType.Cube,
                        new Vector3(0.3f, 1.1f, 0.3f),
                        new Vector3(0.2f, 1.0f, 0f),
                        new Color(0.35f, 0.8f, 0.98f)
                    );
                    Parte(
                        raiz,
                        PrimitiveType.Cube,
                        new Vector3(0.22f, 0.7f, 0.22f),
                        new Vector3(-0.2f, 0.8f, 0.2f),
                        new Color(0.72f, 0.45f, 0.98f)
                    );
                    return true;
                default:
                    return false;
            }
        }

        private static GameObject Modelo(GameObject raiz, string nome, float escala, float giroY)
        {
            var prefab = Resources.Load<GameObject>(PastaDaMata + nome);
            if (prefab == null)
            {
                return null;
            }

            var modelo = Object.Instantiate(prefab, raiz.transform);
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.Euler(0f, giroY, 0f);
            modelo.transform.localScale = Vector3.one * escala;
            return modelo;
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
