using System;

namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Dados de um eixo desenhado no editor, independentes do Unity.</summary>
    public sealed class TracadoDoMundo
    {
        private readonly float[] _distancias;

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

        public TracadoDoMundo(Ponto[] pontos, float largura, float borda, float suavizacao)
        {
            if (pontos == null || pontos.Length < 2)
            {
                throw new ArgumentException(
                    "O tracado precisa de pelo menos dois pontos.",
                    nameof(pontos)
                );
            }

            Pontos = (Ponto[])pontos.Clone();
            Largura = Math.Max(1f, largura);
            Borda = Math.Max(0.5f, borda);
            Suavizacao = Math.Max(0f, suavizacao);
            _distancias = new float[Pontos.Length];
            for (var i = 1; i < Pontos.Length; i++)
            {
                var dx = Pontos[i].X - Pontos[i - 1].X;
                var dz = Pontos[i].Z - Pontos[i - 1].Z;
                _distancias[i] = _distancias[i - 1] + (float)Math.Sqrt(dx * dx + dz * dz);
            }
        }

        public Ponto[] Pontos { get; }
        public float Largura { get; }
        public float Borda { get; }
        public float Suavizacao { get; }

        /// <summary>Localiza o ponto do eixo mais proximo e a direcao do segmento.</summary>
        public float Projetar(
            float x,
            float z,
            out float eixoX,
            out float eixoZ,
            out float direcaoX,
            out float direcaoZ
        )
        {
            return ProjetarInterno(x, z, out eixoX, out eixoZ, out direcaoX, out direcaoZ, out _);
        }

        private float ProjetarInterno(
            float x,
            float z,
            out float eixoX,
            out float eixoZ,
            out float direcaoX,
            out float direcaoZ,
            out float distanciaNoEixo
        )
        {
            var menor = float.MaxValue;
            eixoX = eixoZ = 0f;
            direcaoX = 1f;
            direcaoZ = 0f;
            distanciaNoEixo = 0f;
            for (var i = 0; i + 1 < Pontos.Length; i++)
            {
                var a = Pontos[i];
                var b = Pontos[i + 1];
                var dx = b.X - a.X;
                var dz = b.Z - a.Z;
                var comprimentoQuadrado = dx * dx + dz * dz;
                if (comprimentoQuadrado < 0.0001f)
                {
                    continue;
                }

                var t = Math.Max(
                    0f,
                    Math.Min(1f, ((x - a.X) * dx + (z - a.Z) * dz) / comprimentoQuadrado)
                );
                var px = a.X + t * dx;
                var pz = a.Z + t * dz;
                var quadrado = (x - px) * (x - px) + (z - pz) * (z - pz);
                if (quadrado >= menor)
                {
                    continue;
                }

                menor = quadrado;
                eixoX = px;
                eixoZ = pz;
                var comprimento = (float)Math.Sqrt(comprimentoQuadrado);
                direcaoX = dx / comprimento;
                direcaoZ = dz / comprimento;
                distanciaNoEixo = _distancias[i] + t * comprimento;
            }

            return (float)Math.Sqrt(menor);
        }

        /// <summary>Suaviza cortes e aterros no corredor. A altura base vem da mesma regra do mapa.</summary>
        public float AplicarEstrada(
            float x,
            float z,
            float alturaOriginal,
            Func<float, float, float> alturaBase
        )
        {
            var distancia = ProjetarInterno(
                x,
                z,
                out var eixoX,
                out var eixoZ,
                out _,
                out _,
                out var distanciaNoEixo
            );
            var metade = Largura * 0.5f;
            if (distancia >= metade + Borda)
            {
                return alturaOriginal;
            }

            var alvo = alturaBase(eixoX, eixoZ);
            if (Suavizacao > 0f)
            {
                var antes = PontoNaDistancia(distanciaNoEixo - Suavizacao);
                var depois = PontoNaDistancia(distanciaNoEixo + Suavizacao);
                var anterior = alturaBase(antes.X, antes.Z);
                var posterior = alturaBase(depois.X, depois.Z);
                alvo = (anterior + 2f * alvo + posterior) * 0.25f;
            }

            var t = Math.Max(0f, Math.Min(1f, (distancia - metade) / Borda));
            var peso = 1f - t * t * (3f - 2f * t);
            return alturaOriginal + (alvo - alturaOriginal) * peso;
        }

        private Ponto PontoNaDistancia(float distancia)
        {
            if (distancia <= 0f)
            {
                return Pontos[0];
            }

            var ultimo = Pontos.Length - 1;
            if (distancia >= _distancias[ultimo])
            {
                return Pontos[ultimo];
            }

            for (var i = 0; i < ultimo; i++)
            {
                if (distancia > _distancias[i + 1])
                {
                    continue;
                }

                var intervalo = _distancias[i + 1] - _distancias[i];
                var t = (distancia - _distancias[i]) / intervalo;
                return new Ponto(
                    Pontos[i].X + (Pontos[i + 1].X - Pontos[i].X) * t,
                    Pontos[i].Z + (Pontos[i + 1].Z - Pontos[i].Z) * t
                );
            }

            return Pontos[ultimo];
        }
    }
}
