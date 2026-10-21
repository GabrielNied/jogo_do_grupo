using JogoDoGrupo.Shared.Features.Npcs;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Npcs
{
    /// <summary>
    /// Variação de aparência por semente (#1064): altura de todo NPC e, nos moradores, cabelo, barba,
    /// cor da túnica e tom do corpo. A mesma semente dá sempre o mesmo rosto, em qualquer client.
    /// O gerador de editor deixa no prefab os grupos <c>Cabelo_N</c> e <c>Barba_N</c> desligados.
    /// </summary>
    public static class AparenciaDeNpc
    {
        public const float AlturaMinima = 0.93f;
        public const float AlturaMaxima = 1.07f;
        public const int EstilosDeCabelo = 3;
        public const int EstilosDeBarba = 2;

        private static readonly Color[] CoresDeCabelo =
        {
            new Color(0.08f, 0.06f, 0.05f),
            new Color(0.25f, 0.15f, 0.08f),
            new Color(0.42f, 0.27f, 0.13f),
            new Color(0.8f, 0.65f, 0.3f),
            new Color(0.62f, 0.25f, 0.1f),
            new Color(0.7f, 0.7f, 0.7f),
        };

        private static readonly Color[] CoresDeTunica =
        {
            new Color(0.35f, 0.5f, 0.35f),
            new Color(0.3f, 0.4f, 0.6f),
            new Color(0.6f, 0.3f, 0.25f),
            new Color(0.7f, 0.55f, 0.25f),
            new Color(0.5f, 0.45f, 0.4f),
            new Color(0.5f, 0.3f, 0.5f),
            new Color(0.25f, 0.5f, 0.52f),
        };

        public static void Aplicar(GameObject corpo, PapelNpc papel, int semente)
        {
            var sorteio = new System.Random(unchecked(semente * 7919 + 13));
            var altura = AlturaMinima + (float)sorteio.NextDouble() * (AlturaMaxima - AlturaMinima);
            corpo.transform.localScale = Vector3.one * altura;

            if (papel != PapelNpc.Morador)
            {
                return;
            }

            var cabelo = sorteio.Next(0, EstilosDeCabelo + 1);
            var barba = sorteio.Next(0, EstilosDeBarba + 2);
            var corDoCabelo = CoresDeCabelo[sorteio.Next(CoresDeCabelo.Length)];
            var corDaTunica = CoresDeTunica[sorteio.Next(CoresDeTunica.Length)];
            var tom = 0.8f + (float)sorteio.NextDouble() * 0.25f;

            LigarGrupo(corpo.transform, NomesDeGrupo.Cabelo(cabelo), corDoCabelo);
            LigarGrupo(corpo.transform, NomesDeGrupo.Barba(barba), corDoCabelo);

            var tunica = AcharPorNome(corpo.transform, "Tunica");
            if (tunica != null)
            {
                Tingir(tunica.GetComponent<Renderer>(), corDaTunica);
            }

            foreach (var malha in corpo.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Tingir(malha, new Color(tom, tom * 0.97f, tom * 0.94f));
            }
        }

        private static void LigarGrupo(Transform raiz, string nome, Color cor)
        {
            if (nome == null)
            {
                return;
            }

            var grupo = AcharPorNome(raiz, nome, true);
            if (grupo == null)
            {
                return;
            }

            grupo.gameObject.SetActive(true);
            foreach (var renderer in grupo.GetComponentsInChildren<Renderer>(true))
            {
                Tingir(renderer, cor);
            }
        }

        private static void Tingir(Renderer renderer, Color cor)
        {
            if (renderer == null)
            {
                return;
            }

            var bloco = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(bloco);
            bloco.SetColor("_BaseColor", cor);
            renderer.SetPropertyBlock(bloco);
        }

        // Busca em profundidade, inclusive em objetos desligados.
        private static Transform AcharPorNome(
            Transform raiz,
            string nome,
            bool incluirInativos = false
        )
        {
            foreach (var filho in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (filho.name == nome)
                {
                    return filho;
                }
            }

            return null;
        }

        private static class NomesDeGrupo
        {
            // 0 = sem cabelo/barba; os estilos 1..N são os grupos do prefab.
            public static string Cabelo(int estilo) => estilo <= 0 ? null : "Cabelo_" + estilo;

            public static string Barba(int estilo) =>
                estilo <= 0 || estilo > EstilosDeBarba ? null : "Barba_" + estilo;
        }
    }
}
