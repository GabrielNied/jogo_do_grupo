using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Planta fixa dos corredores de exploracao do mundo novo.</summary>
    public static class CaminhosDoMundo
    {
        public readonly struct Ponto
        {
            public Ponto(float x, float z)
            {
                X = x;
                Z = z;
            }

            public float X { get; }
            public float Z { get; }
        }

        private static readonly Ponto[][] Caminhos =
        {
            // Cidade florestal -> cidade do passo, pelo vale das colinas.
            new[]
            {
                new Ponto(0f, 42f),
                new Ponto(-75f, 105f),
                new Ponto(-205f, 145f),
                new Ponto(-345f, 205f),
                new Ponto(-520f, 320f),
            },
            // Cidade florestal -> oasis, passando pela ruina e pelas colinas secas.
            new[]
            {
                new Ponto(0f, 42f),
                new Ponto(-65f, 85f),
                new Ponto(-165f, -70f),
                new Ponto(-295f, -125f),
                new Ponto(-505f, -280f),
                new Ponto(-715f, -465f),
                new Ponto(-900f, -620f),
            },
            // Terceira ligacao: longo caminho do ombro oeste, com volta pelo deserto.
            new[]
            {
                new Ponto(-520f, 320f),
                new Ponto(-710f, 245f),
                new Ponto(-900f, 50f),
                new Ponto(-1120f, -175f),
                new Ponto(-1110f, -390f),
                new Ponto(-900f, -620f),
            },
            // Trilha leste ate as ilhas do pantano, com travessia do rio principal.
            new[]
            {
                new Ponto(0f, 42f),
                new Ponto(90f, 160f),
                new Ponto(250f, 165f),
                new Ponto(380f, 110f),
                new Ponto(565f, 70f),
                new Ponto(730f, 145f),
            },
            // Ramal seguro ate o mirante na borda da caldeira.
            new[]
            {
                new Ponto(0f, 42f),
                new Ponto(125f, -115f),
                new Ponto(300f, -300f),
                new Ponto(470f, -475f),
                new Ponto(565f, -650f),
            },
            // Rota de subida ao interior da serra, depois da cidade do passo.
            new[] { new Ponto(-520f, 320f), new Ponto(-655f, 495f), new Ponto(-765f, 665f) },
        };

        public static IReadOnlyList<Ponto[]> Rotas => Caminhos;

        /// <summary>Distancia horizontal ao eixo do caminho mais proximo, em metros.</summary>
        public static float DistanciaAteCaminho(float x, float z)
        {
            var melhorQuadrado = float.MaxValue;
            foreach (var caminho in Caminhos)
            {
                for (var i = 0; i + 1 < caminho.Length; i++)
                {
                    var a = caminho[i];
                    var b = caminho[i + 1];
                    var dx = b.X - a.X;
                    var dz = b.Z - a.Z;
                    var tamanhoQuadrado = dx * dx + dz * dz;
                    var t = Math.Max(
                        0f,
                        Math.Min(1f, ((x - a.X) * dx + (z - a.Z) * dz) / tamanhoQuadrado)
                    );
                    var lateralX = x - (a.X + t * dx);
                    var lateralZ = z - (a.Z + t * dz);
                    melhorQuadrado = Math.Min(
                        melhorQuadrado,
                        lateralX * lateralX + lateralZ * lateralZ
                    );
                }
            }

            return (float)Math.Sqrt(melhorQuadrado);
        }

        public static float PesoEm(float x, float z)
        {
            var t = (DistanciaAteCaminho(x, z) - 3f) / 9f;
            t = Math.Max(0f, Math.Min(1f, t));
            return 1f - t * t * (3f - 2f * t);
        }
    }
}
