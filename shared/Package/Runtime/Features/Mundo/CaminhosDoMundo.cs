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
            Array.ConvertAll(EstradaDoPasso.Tracado.Pontos, p => new Ponto(p.X, p.Z)),
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
            return DistanciaAteCaminho(x, z, 0);
        }

        private static float DistanciaAteCaminho(float x, float z, int primeiraRota)
        {
            var melhorQuadrado = float.MaxValue;
            for (var rota = primeiraRota; rota < Caminhos.Length; rota++)
            {
                var caminho = Caminhos[rota];
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
            var t = (DistanciaAteCaminho(x, z, 1) - 3f) / 9f;
            t = Math.Max(0f, Math.Min(1f, t));
            var pesoAnterior = 1f - t * t * (3f - 2f * t);
            var tracado = EstradaDoPasso.Tracado;
            var distancia = tracado.Projetar(x, z, out _, out _, out _, out _);
            var metade = tracado.Largura * 0.5f;
            var borda = Math.Max(0f, Math.Min(1f, (distancia - metade) / tracado.Borda));
            var pesoAutoral = 1f - borda * borda * (3f - 2f * borda);
            return Math.Max(pesoAnterior, pesoAutoral);
        }

        /// <summary>Reserva a passagem visivel da estrada antes de distribuir arvores e pecas.</summary>
        public static bool LivreDeVegetacao(float x, float z, float folga = 0f)
        {
            var raio = Math.Max(9f, EstradaDoPasso.Tracado.Largura * 0.5f + 2f) + folga;
            return DistanciaAteCaminho(x, z, 1) < 9f + folga
                || EstradaDoPasso.Tracado.Projetar(x, z, out _, out _, out _, out _) < raio;
        }
    }
}
