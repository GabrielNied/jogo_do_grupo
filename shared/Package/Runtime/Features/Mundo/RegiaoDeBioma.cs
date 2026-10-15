using System;

namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Contorno autoral de um bioma. A ordem das regiões decide qual pintura fica por cima.</summary>
    public sealed class RegiaoDeBioma
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

        private readonly Ponto[] _pontos;
        private readonly float _minX;
        private readonly float _maxX;
        private readonly float _minZ;
        private readonly float _maxZ;

        public RegiaoDeBioma(
            string nome,
            Bioma bioma,
            Ponto[] pontos,
            float larguraDaBorda,
            float intensidadeDoRelevo,
            float densidadeDaVegetacao
        )
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                throw new ArgumentException("A região precisa de um nome.", nameof(nome));
            }

            if (bioma < Bioma.Campo || bioma > Bioma.Pantano)
            {
                throw new ArgumentOutOfRangeException(nameof(bioma));
            }

            if (pontos == null || pontos.Length < 3 || pontos.Length > 64)
            {
                throw new ArgumentException("Use entre 3 e 64 pontos no contorno.", nameof(pontos));
            }

            if (!Valido(larguraDaBorda, 0f, 200f))
            {
                throw new ArgumentOutOfRangeException(nameof(larguraDaBorda));
            }

            if (!Valido(intensidadeDoRelevo, 0f, 2f))
            {
                throw new ArgumentOutOfRangeException(nameof(intensidadeDoRelevo));
            }

            if (!Valido(densidadeDaVegetacao, 0f, 2f))
            {
                throw new ArgumentOutOfRangeException(nameof(densidadeDaVegetacao));
            }

            _pontos = (Ponto[])pontos.Clone();
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minZ = float.MaxValue;
            var maxZ = float.MinValue;
            double areaDupla = 0;
            for (var i = 0; i < _pontos.Length; i++)
            {
                var ponto = _pontos[i];
                if (
                    !Valido(ponto.X, -MapaDoMundo.Metade, MapaDoMundo.Metade)
                    || !Valido(ponto.Z, -MapaDoMundo.Metade, MapaDoMundo.Metade)
                )
                {
                    throw new ArgumentOutOfRangeException(nameof(pontos));
                }

                minX = Math.Min(minX, ponto.X);
                maxX = Math.Max(maxX, ponto.X);
                minZ = Math.Min(minZ, ponto.Z);
                maxZ = Math.Max(maxZ, ponto.Z);
                var proximo = _pontos[(i + 1) % _pontos.Length];
                areaDupla += (double)ponto.X * proximo.Z - (double)proximo.X * ponto.Z;
            }

            if (Math.Abs(areaDupla) < 1d)
            {
                throw new ArgumentException("O contorno precisa cercar uma área.", nameof(pontos));
            }

            Nome = nome;
            Bioma = bioma;
            LarguraDaBorda = larguraDaBorda;
            IntensidadeDoRelevo = intensidadeDoRelevo;
            DensidadeDaVegetacao = densidadeDaVegetacao;
            _minX = minX - larguraDaBorda / 2f;
            _maxX = maxX + larguraDaBorda / 2f;
            _minZ = minZ - larguraDaBorda / 2f;
            _maxZ = maxZ + larguraDaBorda / 2f;
        }

        public string Nome { get; }
        public Bioma Bioma { get; }
        public float LarguraDaBorda { get; }
        public float IntensidadeDoRelevo { get; }
        public float DensidadeDaVegetacao { get; }
        public Ponto[] Pontos => (Ponto[])_pontos.Clone();

        /// <summary>1 no interior, 0 fora e transição suave na largura escolhida.</summary>
        public float CoberturaEm(float x, float z)
        {
            if (x < _minX || x > _maxX || z < _minZ || z > _maxZ)
            {
                return 0f;
            }

            var dentro = false;
            var menorDistanciaQuadrada = float.MaxValue;
            for (var i = 0; i < _pontos.Length; i++)
            {
                var a = _pontos[i];
                var b = _pontos[(i + 1) % _pontos.Length];
                if ((a.Z > z) != (b.Z > z) && x < (b.X - a.X) * (z - a.Z) / (b.Z - a.Z) + a.X)
                {
                    dentro = !dentro;
                }

                var dx = b.X - a.X;
                var dz = b.Z - a.Z;
                var comprimentoQuadrado = dx * dx + dz * dz;
                var t =
                    comprimentoQuadrado > 0f
                        ? Limitar(((x - a.X) * dx + (z - a.Z) * dz) / comprimentoQuadrado)
                        : 0f;
                var px = x - a.X - t * dx;
                var pz = z - a.Z - t * dz;
                menorDistanciaQuadrada = Math.Min(menorDistanciaQuadrada, px * px + pz * pz);
            }

            if (LarguraDaBorda == 0f)
            {
                return dentro ? 1f : 0f;
            }

            var distancia = (float)Math.Sqrt(menorDistanciaQuadrada) * (dentro ? 1f : -1f);
            var valor = Limitar(0.5f + distancia / LarguraDaBorda);
            return valor * valor * (3f - 2f * valor);
        }

        public PesosDosBiomas Aplicar(PesosDosBiomas atual, float cobertura)
        {
            var restante = 1f - cobertura;
            return new PesosDosBiomas(
                atual.Campo * restante + (Bioma == Bioma.Campo ? cobertura : 0f),
                atual.Mata * restante + (Bioma == Bioma.Mata ? cobertura : 0f),
                atual.Colinas * restante + (Bioma == Bioma.Colinas ? cobertura : 0f),
                atual.Deserto * restante + (Bioma == Bioma.Deserto ? cobertura : 0f),
                atual.Neve * restante + (Bioma == Bioma.Neve ? cobertura : 0f),
                atual.Lava * restante + (Bioma == Bioma.Lava ? cobertura : 0f),
                atual.Pantano * restante + (Bioma == Bioma.Pantano ? cobertura : 0f)
            );
        }

        private static float Limitar(float valor) => Math.Max(0f, Math.Min(1f, valor));

        private static bool Valido(float valor, float minimo, float maximo)
        {
            return !float.IsNaN(valor) && valor >= minimo && valor <= maximo;
        }
    }
}
