using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Mobs;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>Projeta inimigos na tela e acha quem está sob a mira (centro da viewport).</summary>
    internal static class MiraDeCombate
    {
        /// <summary>Altura acima do chão para mirar no tronco (mesma ordem de grandeza das etiquetas).</summary>
        private const float AlturaNaTela = 1.4f;

        public static long? InimigoSobMira(
            Camera camera,
            IReadOnlyCollection<InimigoNoClient> inimigos,
            Vector3 posicaoDoJogador
        )
        {
            if (camera == null || inimigos == null)
            {
                return null;
            }

            long? escolhido = null;
            var menorDistanciaAoCentro = float.MaxValue;
            var raio = RegrasAlvoDoHud.RaioDaMiraViewport;
            var raioQuadrado = raio * raio;

            foreach (var inimigo in inimigos)
            {
                if (inimigo == null || inimigo.Vida <= 0)
                {
                    continue;
                }

                var horizontal = DistanciaHorizontal(posicaoDoJogador, inimigo.X, inimigo.Z);
                if (horizontal > RegrasAlvoDoHud.DistanciaMaximaMetros)
                {
                    continue;
                }

                var mundo = new Vector3(
                    inimigo.X,
                    inimigo.Y + AlturaNaTela * inimigo.Escala,
                    inimigo.Z
                );
                var viewport = camera.WorldToViewportPoint(mundo);
                if (viewport.z <= 0f)
                {
                    continue;
                }

                var dx = viewport.x - 0.5f;
                var dy = viewport.y - 0.5f;
                var distanciaAoCentro = dx * dx + dy * dy;
                if (distanciaAoCentro > raioQuadrado || distanciaAoCentro >= menorDistanciaAoCentro)
                {
                    continue;
                }

                menorDistanciaAoCentro = distanciaAoCentro;
                escolhido = inimigo.InimigoId;
            }

            return escolhido;
        }

        private static float DistanciaHorizontal(Vector3 jogador, float x, float z)
        {
            var dx = x - jogador.x;
            var dz = z - jogador.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
