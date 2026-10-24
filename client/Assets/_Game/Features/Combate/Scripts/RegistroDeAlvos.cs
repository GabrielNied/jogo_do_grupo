using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Acha o objeto de um alvo de combate pelo <c>AcertoDeAtaque.AlvoId</c> que o servidor manda,
    /// sem varrer a cena a cada golpe. O boneco de treino se registra sozinho (id
    /// <see cref="RegrasBonecoDeTreino.AlvoId"/>) ou é achado pelo nome. Jogadores e outros avatares
    /// são consultados via Core. Para inimigos (Mobs), procura o objeto <c>"Inimigo &lt;id&gt;"</c>
    /// e guarda o achado.
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

            if (alvoId == RegrasBonecoDeTreino.AlvoId)
            {
                var boneco = GameObject.Find(BonecoDeTreinoVisual.Nome);
                if (boneco != null)
                {
                    Alvos[alvoId] = boneco;
                }

                return boneco;
            }

            if (EntradaNoJogo.PersonagemId != 0 && alvoId == EntradaNoJogo.PersonagemId)
            {
                var jogador = EntradaNoJogo.Jogador;
                if (jogador != null)
                {
                    Alvos[alvoId] = jogador;
                }

                return jogador;
            }

            var outro = AnimacaoDeOutros.Buscar != null ? AnimacaoDeOutros.Buscar(alvoId) : null;
            if (outro != null)
            {
                Alvos[alvoId] = outro;
                return outro;
            }

            if (alvoId <= 0)
            {
                return null;
            }

            var inimigo =
                GameObject.Find(NomeDoInimigo + alvoId) ?? GameObject.Find("Inimigo_" + alvoId);
            if (inimigo != null)
            {
                Alvos[alvoId] = inimigo;
            }

            return inimigo;
        }

        /// <summary>
        /// Onde nasce o número de dano de um alvo: acima da cabeça (topo dos renderers mais a
        /// <see cref="FolgaDoNumero"/>), no centro horizontal dele.
        /// </summary>
        public static Vector3 AcimaDaCabeca(GameObject alvo)
        {
            if (alvo == null)
            {
                return Vector3.zero;
            }

            var pos = alvo.transform.position;
            var renderers = alvo.GetComponentsInChildren<Renderer>();
            var maxY = float.NegativeInfinity;
            var achouRenderer = false;

            for (var i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || !r.enabled)
                {
                    continue;
                }

                if (r is ParticleSystemRenderer || r is TrailRenderer)
                {
                    continue;
                }

                var b = r.bounds;
                if (b.size.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                if (b.max.y > maxY)
                {
                    maxY = b.max.y;
                    achouRenderer = true;
                }
            }

            var topoY = (achouRenderer && maxY > pos.y) ? maxY : pos.y + 1.8f;
            return new Vector3(pos.x, topoY + FolgaDoNumero, pos.z);
        }

        /// <summary>O ponto sobre o topo de <paramref name="limites"/>, no centro dele.</summary>
        public static Vector3 AcimaDe(Bounds limites) =>
            new Vector3(limites.center.x, limites.max.y + FolgaDoNumero, limites.center.z);
    }
}
