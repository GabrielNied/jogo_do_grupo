using System;

namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// A primeira caverna: um túnel escavado num morro das colinas, contínuo com o terreno (sem
    /// teleporte, sem troca de espaço; não é a dungeon da issue #66). A pessoa entra andando pela
    /// boca, desce por um túnel sinuoso, passa por duas câmaras (uma média e uma grande, no
    /// fim), por um corredor com desnível, e há um beco lateral sem saída.
    /// </summary>
    /// <remarks>
    /// O terreno é um mapa de alturas e não guarda nada sobreposto, então o túnel é geometria
    /// própria: aqui ficam o piso, o teto e o contorno (funções puras, sem Unity), que o client
    /// usa para montar a malha e o mapa, e o servidor para julgar o movimento. O relevo do morro
    /// (<see cref="AlturaDoMorro"/>) é somado ao chão do <see cref="MapaDoMundo"/>.
    /// </remarks>
    public static class PrimeiraCaverna
    {
        /// <summary>Boca no mundo, em X.</summary>
        public const float EntradaX = -220f;

        /// <summary>Boca no mundo, em Z. O túnel segue para o norte (Z crescente).</summary>
        public const float EntradaZ = -80f;

        /// <summary>Centro do morro, em X.</summary>
        public const float MorroCentroX = -220f;

        /// <summary>Centro do morro, em Z.</summary>
        public const float MorroCentroZ = -47f;

        /// <summary>Raio do morro (m): fora dele o relevo é o das colinas.</summary>
        public const float MorroRaio = 42f;

        /// <summary>Altura do morro no centro (m), somada ao relevo das colinas.</summary>
        public const float MorroAltura = 16f;

        /// <summary>Quanto o personagem pode ficar abaixo do piso do túnel (m).</summary>
        public const float FolgaAbaixoDoPiso = 1f;

        /// <summary>
        /// Terreno acima do teto, em metros, até onde a malha do terreno é furada (o furo vale
        /// onde o terreno fica abaixo do teto mais esta margem).
        /// </summary>
        public const float MargemDoFuro = 0.3f;

        /// <summary>Largura mínima de passagem em qualquer ponto do túnel (m).</summary>
        public const float LarguraMinimaDePassagem = 3f;

        /// <summary>Altura mínima entre piso e teto em qualquer ponto do túnel (m).</summary>
        public const float AlturaMinimaDePassagem = 3.2f;

        // Pontos por trecho entre dois nós do percurso (curva suave, passo de uns 1 m).
        private const int PassosPorTrecho = 12;

        private const float TamanhoDoTrechoMinimo = 1e-6f;

        // Cada nó: centro (X, Z), meia largura, quanto o piso desceu desde a boca e altura do
        // teto sobre o piso, tudo em metros. O percurso é uma curva suave que passa pelos nós.
        // Curvas (nós 2 a 5), câmara média (6), corredor (8 e 9), rampa (9 a 10), câmara grande (12).
        private static readonly float[][] NosDoPercurso =
        {
            new[] { -220f, -80f, 2.0f, 0.0f, 3.8f },
            new[] { -220f, -72f, 2.0f, 0.6f, 3.8f },
            new[] { -215f, -63f, 2.2f, 1.7f, 3.8f },
            new[] { -221f, -54f, 2.2f, 3.0f, 3.8f },
            new[] { -230f, -46f, 2.4f, 4.4f, 3.8f },
            new[] { -237f, -37f, 3.0f, 5.4f, 4.0f },
            new[] { -234f, -28f, 6.5f, 5.8f, 6.0f },
            new[] { -228f, -20f, 3.0f, 6.0f, 4.0f },
            new[] { -216f, -17f, 2.2f, 6.0f, 3.8f },
            new[] { -206f, -18f, 2.2f, 6.0f, 3.8f },
            new[] { -197f, -22f, 2.2f, 8.2f, 3.8f },
            new[] { -190f, -30f, 3.5f, 9.0f, 4.5f },
            new[] { -188f, -41f, 9.5f, 10.0f, 7.0f },
        };

        // Beco sem saída, saindo do corredor (nó 8 do percurso, mesmo piso).
        private static readonly float[][] NosDoBeco =
        {
            new[] { -216f, -17f, 1.6f, 6.0f, 3.4f },
            new[] { -215f, -6f, 1.6f, 6.0f, 3.4f },
            new[] { -218f, 3f, 1.5f, 6.0f, 3.4f },
        };

        private static readonly float _entradaY = MapaDoMundo.AlturaEm(EntradaX, EntradaZ);
        private static readonly Curva _percurso = new Curva(NosDoPercurso);
        private static readonly Curva _beco = new Curva(NosDoBeco);

        /// <summary>Altura do chão do mundo na boca.</summary>
        public static float EntradaY => _entradaY;

        /// <summary>
        /// Quanto o morro soma ao relevo em (<paramref name="x"/>, <paramref name="z"/>), em
        /// metros: um domo suave, que acaba no raio do morro.
        /// </summary>
        public static float AlturaDoMorro(float x, float z)
        {
            var dx = x - MorroCentroX;
            var dz = z - MorroCentroZ;
            var t = (float)Math.Sqrt((dx * dx) + (dz * dz)) / MorroRaio;
            if (t >= 1f)
            {
                return 0f;
            }

            return MorroAltura * (1f - Suavizar(t));
        }

        /// <summary>
        /// Altura do piso do túnel em (<paramref name="x"/>, <paramref name="z"/>) para quem está
        /// na altura <paramref name="y"/> (pés), ou <c>null</c> se o ponto não está dentro do
        /// túnel: fora do contorno, abaixo do piso (mais que <see cref="FolgaAbaixoDoPiso"/>) ou
        /// acima do teto (em cima do morro, ou dentro da rocha).
        /// </summary>
        public static float? PisoEm(float x, float z, float y)
        {
            if (!Amostrar(x, z, out var amostra))
            {
                return null;
            }

            return y >= amostra.Piso - FolgaAbaixoDoPiso && y <= amostra.Piso + amostra.Altura
                ? amostra.Piso
                : (float?)null;
        }

        /// <summary>Piso do túnel em (x, z), em qualquer altura, ou <c>null</c> fora do contorno.</summary>
        public static float? PisoNoPlano(float x, float z) =>
            Amostrar(x, z, out var amostra) ? amostra.Piso : (float?)null;

        /// <summary>Teto do túnel em (x, z), ou <c>null</c> fora do contorno.</summary>
        public static float? TetoEm(float x, float z) =>
            Amostrar(x, z, out var amostra) ? amostra.Piso + amostra.Altura : (float?)null;

        /// <summary>Se o ponto (pés em <paramref name="y"/>) está dentro do túnel.</summary>
        public static bool DentroDoTunel(float x, float z, float y) => PisoEm(x, z, y).HasValue;

        /// <summary>
        /// Piso do trecho mais próximo de (x, z), mesmo fora do contorno (ao longo do percurso):
        /// serve à malha, que precisa do piso nos cantos de células na borda.
        /// </summary>
        public static float PisoProximo(float x, float z)
        {
            var melhor = float.MaxValue;
            var piso = _entradaY;
            ProximoDe(_percurso, x, z, ref melhor, ref piso);
            ProximoDe(_beco, x, z, ref melhor, ref piso);
            return piso;
        }

        /// <summary>
        /// Se o terreno deve ter um furo em (x, z): dentro do contorno do túnel e com o terreno
        /// abaixo do teto (mais <see cref="MargemDoFuro"/>), ou seja, onde o chão do mundo
        /// atravessaria o espaço do túnel. Mais adentro, com o morro por cima, não há furo.
        /// </summary>
        public static bool FuraOTerreno(float x, float z) =>
            Amostrar(x, z, out var amostra)
            && MapaDoMundo.AlturaEm(x, z) < amostra.Piso + amostra.Altura + MargemDoFuro;

        /// <summary>Comprimento do percurso principal, da boca à câmara final (m).</summary>
        public static float ComprimentoDoPercurso => _percurso.Comprimento;

        /// <summary>Ponto do percurso principal a <paramref name="distancia"/> metros da boca.</summary>
        public static void PontoDoPercurso(float distancia, out float x, out float z) =>
            _percurso.PontoA(distancia, out x, out z);

        /// <summary>Centro da câmara grande, no fim do percurso, em X.</summary>
        public static float CamaraFinalX => NosDoPercurso[NosDoPercurso.Length - 1][0];

        /// <summary>Centro da câmara grande, no fim do percurso, em Z.</summary>
        public static float CamaraFinalZ => NosDoPercurso[NosDoPercurso.Length - 1][1];

        /// <summary>Ponta do beco sem saída, em X.</summary>
        public static float PontaDoBecoX => NosDoBeco[NosDoBeco.Length - 1][0];

        /// <summary>Ponta do beco sem saída, em Z.</summary>
        public static float PontaDoBecoZ => NosDoBeco[NosDoBeco.Length - 1][1];

        /// <summary>Retângulo (com folga) que contém o túnel inteiro, para varrer a malha.</summary>
        public static void Limites(out float minX, out float minZ, out float maxX, out float maxZ)
        {
            minX = float.MaxValue;
            minZ = float.MaxValue;
            maxX = float.MinValue;
            maxZ = float.MinValue;
            Alargar(_percurso, ref minX, ref minZ, ref maxX, ref maxZ);
            Alargar(_beco, ref minX, ref minZ, ref maxX, ref maxZ);
        }

        /// <summary>Distância horizontal entre dois pontos.</summary>
        public static float Distancia(float x, float z, float outroX, float outroZ)
        {
            var dx = x - outroX;
            var dz = z - outroZ;
            return (float)Math.Sqrt((dx * dx) + (dz * dz));
        }

        private static void Alargar(
            Curva curva,
            ref float minX,
            ref float minZ,
            ref float maxX,
            ref float maxZ
        )
        {
            for (var i = 0; i < curva.X.Length; i++)
            {
                var folga = curva.Meia[i] + 1f;
                minX = Math.Min(minX, curva.X[i] - folga);
                maxX = Math.Max(maxX, curva.X[i] + folga);
                minZ = Math.Min(minZ, curva.Z[i] - folga);
                maxZ = Math.Max(maxZ, curva.Z[i] + folga);
            }
        }

        private readonly struct Amostra
        {
            public Amostra(float piso, float altura)
            {
                Piso = piso;
                Altura = altura;
            }

            public float Piso { get; }
            public float Altura { get; }
        }

        // Acha o trecho que contém (x, z): a menor distância ao eixo em relação à meia largura.
        private static bool Amostrar(float x, float z, out Amostra amostra)
        {
            var melhor = float.MaxValue;
            amostra = default;
            Dentro(_percurso, x, z, ref melhor, ref amostra);
            Dentro(_beco, x, z, ref melhor, ref amostra);
            return melhor <= 1f;
        }

        private static void Dentro(
            Curva curva,
            float x,
            float z,
            ref float melhor,
            ref Amostra amostra
        )
        {
            if (
                !curva.Projetar(
                    x,
                    z,
                    true,
                    out var distancia,
                    out var meia,
                    out var piso,
                    out var altura
                )
            )
            {
                return;
            }

            var relativa = distancia / meia;
            if (relativa <= 1f && relativa < melhor)
            {
                melhor = relativa;
                amostra = new Amostra(_entradaY - piso, altura);
            }
        }

        private static void ProximoDe(
            Curva curva,
            float x,
            float z,
            ref float melhor,
            ref float piso
        )
        {
            if (curva.Projetar(x, z, false, out var distancia, out _, out var descida, out _))
            {
                if (distancia < melhor)
                {
                    melhor = distancia;
                    piso = _entradaY - descida;
                }
            }
        }

        private static float Suavizar(float t) => t * t * (3f - (2f * t));

        // Curva suave (Catmull-Rom) pelos nós, em passos curtos, com meia largura, descida do
        // piso e altura do teto interpolados entre os nós (suavizados, sem ressalto).
        private sealed class Curva
        {
            public readonly float[] X;
            public readonly float[] Z;
            public readonly float[] Meia;
            public readonly float[] Descida;
            public readonly float[] Altura;
            private readonly float[] _acumulado;

            public Curva(float[][] nos)
            {
                var total = ((nos.Length - 1) * PassosPorTrecho) + 1;
                X = new float[total];
                Z = new float[total];
                Meia = new float[total];
                Descida = new float[total];
                Altura = new float[total];
                _acumulado = new float[total];

                var k = 0;
                for (var i = 0; i < nos.Length - 1; i++)
                {
                    var p0 = nos[Math.Max(i - 1, 0)];
                    var p1 = nos[i];
                    var p2 = nos[i + 1];
                    var p3 = nos[Math.Min(i + 2, nos.Length - 1)];
                    for (var p = 0; p < PassosPorTrecho; p++)
                    {
                        var u = p / (float)PassosPorTrecho;
                        X[k] = CatmullRom(p0[0], p1[0], p2[0], p3[0], u);
                        Z[k] = CatmullRom(p0[1], p1[1], p2[1], p3[1], u);
                        var s = Suavizar(u);
                        Meia[k] = p1[2] + ((p2[2] - p1[2]) * s);
                        Descida[k] = p1[3] + ((p2[3] - p1[3]) * s);
                        Altura[k] = p1[4] + ((p2[4] - p1[4]) * s);
                        k++;
                    }
                }

                var ultimo = nos[nos.Length - 1];
                X[k] = ultimo[0];
                Z[k] = ultimo[1];
                Meia[k] = ultimo[2];
                Descida[k] = ultimo[3];
                Altura[k] = ultimo[4];

                for (var i = 1; i < total; i++)
                {
                    _acumulado[i] = _acumulado[i - 1] + Distancia(X[i - 1], Z[i - 1], X[i], Z[i]);
                }
            }

            public float Comprimento => _acumulado[_acumulado.Length - 1];

            public void PontoA(float distancia, out float x, out float z)
            {
                var d = Math.Max(0f, Math.Min(distancia, Comprimento));
                var i = 1;
                while (i < _acumulado.Length - 1 && _acumulado[i] < d)
                {
                    i++;
                }

                var trecho = _acumulado[i] - _acumulado[i - 1];
                var t = trecho < TamanhoDoTrechoMinimo ? 0f : (d - _acumulado[i - 1]) / trecho;
                x = X[i - 1] + ((X[i] - X[i - 1]) * t);
                z = Z[i - 1] + ((Z[i] - Z[i - 1]) * t);
            }

            // Ponto do eixo mais próximo de (x, z): distância, meia largura, descida e altura
            // ali. Com <paramref name="semPontas"/>, o que passa das pontas do trecho (as
            // extremidades são retas, não arredondadas) não conta.
            public bool Projetar(
                float x,
                float z,
                bool semPontas,
                out float distancia,
                out float meia,
                out float descida,
                out float altura
            )
            {
                distancia = float.MaxValue;
                meia = 0f;
                descida = 0f;
                altura = 0f;
                var achou = false;
                var ultimo = X.Length - 2;
                for (var i = 0; i <= ultimo; i++)
                {
                    var sx = X[i + 1] - X[i];
                    var sz = Z[i + 1] - Z[i];
                    var tamanho2 = (sx * sx) + (sz * sz);
                    if (tamanho2 < TamanhoDoTrechoMinimo)
                    {
                        continue;
                    }

                    var t = (((x - X[i]) * sx) + ((z - Z[i]) * sz)) / tamanho2;
                    var alemDaPonta = (i == 0 && t < 0f) || (i == ultimo && t > 1f);
                    t = Math.Max(0f, Math.Min(1f, t));
                    var px = X[i] + (sx * t);
                    var pz = Z[i] + (sz * t);
                    var d = Distancia(x, z, px, pz);
                    if (d >= distancia)
                    {
                        continue;
                    }

                    achou = !(semPontas && alemDaPonta);
                    distancia = d;
                    meia = Meia[i] + ((Meia[i + 1] - Meia[i]) * t);
                    descida = Descida[i] + ((Descida[i + 1] - Descida[i]) * t);
                    altura = Altura[i] + ((Altura[i + 1] - Altura[i]) * t);
                }

                return achou;
            }

            private static float CatmullRom(float p0, float p1, float p2, float p3, float u)
            {
                var u2 = u * u;
                var u3 = u2 * u;
                return 0.5f
                    * (
                        (2f * p1)
                        + ((p2 - p0) * u)
                        + (((2f * p0) - (5f * p1) + (4f * p2) - p3) * u2)
                        + ((-p0 + (3f * p1) - (3f * p2) + p3) * u3)
                    );
            }
        }
    }
}
