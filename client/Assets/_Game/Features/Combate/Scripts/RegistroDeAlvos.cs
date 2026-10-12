using System.Collections.Generic;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Acha o objeto de um alvo de combate pelo <c>AcertoDeAtaque.AlvoId</c> que o servidor manda,
    /// sem varrer a cena a cada golpe. O boneco de treino se registra sozinho (id
    /// <see cref="JogoDoGrupo.Shared.Features.Combate.RegrasBonecoDeTreino.AlvoId"/>). Os inimigos
    /// (Mobs) ainda não se registram: para eles o registro procura uma vez o objeto
    /// <c>"Inimigo &lt;id&gt;"</c> que a funcionalidade Mobs cria, e guarda o achado.
    /// </summary>
    internal static class RegistroDeAlvos
    {
        public const string NomeDoInimigo = "Inimigo ";

        /// <summary>Folga, em metros, entre o topo do alvo e a origem do número de dano.</summary>
        public const float FolgaDoNumero = 0.35f;

        private static readonly Dictionary<long, GameObject> Alvos =
            new Dictionary<long, GameObject>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Alvos.Clear();

        public static void Registrar(long alvoId, GameObject alvo)
        {
            if (alvo != null)
            {
                Alvos[alvoId] = alvo;
            }
        }

        public static void Remover(long alvoId, GameObject alvo)
        {
            if (Alvos.TryGetValue(alvoId, out var atual) && atual == alvo)
            {
                Alvos.Remove(alvoId);
            }
        }

        /// <summary>O objeto do alvo, ou <c>null</c> se ele não está na cena.</summary>
        public static GameObject Achar(long alvoId)
        {
            if (Alvos.TryGetValue(alvoId, out var guardado))
            {
                if (guardado != null)
                {
                    return guardado;
                }

                Alvos.Remove(alvoId);
            }

            if (alvoId <= 0)
            {
                return null;
            }

            var inimigo = GameObject.Find(NomeDoInimigo + alvoId);
            if (inimigo != null)
            {
                Alvos[alvoId] = inimigo;
            }

            return inimigo;
        }

        /// <summary>
        /// Onde nasce o número de dano de um alvo: acima da cabeça (topo dos renderers mais a
        /// <see cref="FolgaDoNumero"/>), no centro dele.
        /// </summary>
        public static Vector3 AcimaDaCabeca(GameObject alvo)
        {
            var renderers = alvo.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return alvo.transform.position + Vector3.up * (1.8f + FolgaDoNumero);
            }

            var limites = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                limites.Encapsulate(renderers[i].bounds);
            }

            return AcimaDe(limites);
        }

        /// <summary>O ponto sobre o topo de <paramref name="limites"/>, no centro dele.</summary>
        public static Vector3 AcimaDe(Bounds limites) =>
            new Vector3(limites.center.x, limites.max.y + FolgaDoNumero, limites.center.z);
    }
}
