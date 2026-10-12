using System;

namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// O mapa do mundo: que bioma e que altura do chão existem em cada ponto. Regras puras, sem
    /// rede e sem engine, iguais no client (que desenha o terreno) e no servidor (que confere o
    /// movimento). Tudo vem de sementes fixas: o mesmo ponto dá sempre a mesma resposta.
    /// Medidas e disposição em docs/features/mundo.md, "Biomas e relevo".
    /// </summary>
    public static class MapaDoMundo
    {
        /// <summary>Lado do mundo, em metros. É um quadrado centrado na origem.</summary>
        public const float Lado = 3000f;

        /// <summary>Metade do lado: o mundo vai de -Metade a +Metade em X e em Z.</summary>
        public const float Metade = Lado / 2f;

        /// <summary>Metade do lado da cidade murada, em metros (a cidade tem 80 m).</summary>
        public const float MetadeDaCidade = 40f;

        /// <summary>
        /// Até esta distância do centro (a maior entre |x| e |z|) a chegada à cidade ainda não
        /// entrou no relevo dos biomas distantes: dentro dos muros é plano; entre os muros e este
        /// raio há uma subida leve (a estrada do portão acompanha).
        /// </summary>
        public const float RaioPlano = 130f;

        /// <summary>Subida máxima na faixa entre os muros e <see cref="RaioPlano"/>, em metros.</summary>
        public const float SubidaMaximaDaFaixaInicial = 5f;

        /// <summary>
        /// Distância do centro (a maior entre |x| e |z|) em que campo, mata e colinas dão lugar
        /// aos biomas de fora. A fronteira real ondula em torno deste valor.
        /// </summary>
        public const float MetadeDoMiolo = 500f;

        /// <summary>Distância entre dois pontos vizinhos da grade de alturas, em metros.</summary>
        public const float PassoDaGrade = 10f;

        /// <summary>Número de células da grade por lado (300).</summary>
        public const int CelulasPorLado = (int)(Lado / PassoDaGrade);

        /// <summary>Número de pontos da grade por lado (301).</summary>
        public const int PontosPorLado = CelulasPorLado + 1;

        /// <summary>Altura da superfície da água do pântano, em metros.</summary>
        public const float NivelDaAgua = -0.5f;

        /// <summary>Altura da superfície da lava, em metros.</summary>
        public const float NivelDaLava = -1f;

        /// <summary>Nenhum ponto do chão fica abaixo desta altura, em metros.</summary>
        public const float AlturaMinima = -2f;

        /// <summary>Nenhum ponto do chão fica acima desta altura, em metros.</summary>
        public const float AlturaMaxima = 220f;

        private const uint SementeDesvioX = 11u;
        private const uint SementeDesvioZ = 23u;
        private const uint SementeCampo = 31u;
        private const uint SementeMata = 37u;
        private const uint SementeColinas = 41u;
        private const uint SementeDunas = 43u;
        private const uint SementeAreia = 47u;
        private const uint SementePicosDeNeve = 53u;
        private const uint SementeEncostaDeNeve = 59u;
        private const uint SementeChaoDeNeve = 61u;
        private const uint SementePicosDeLava = 67u;
        private const uint SementeEncostaDeLava = 71u;
        private const uint SementeChaoDeLava = 73u;
        private const uint SementeRios = 79u;
        private const uint SementePantano = 83u;
        private const uint SementeTrechos = 89u;

        /// <summary>Coordenada (X ou Z) do ponto <paramref name="indice"/> da grade, em metros.</summary>
        public static float CoordenadaDaGrade(int indice)
        {
            return -Metade + PassoDaGrade * indice;
        }

        /// <summary>Se o ponto fica dentro do quadrado do mundo.</summary>
        public static bool DentroDoMundo(float x, float z)
        {
            return Math.Abs(x) <= Metade && Math.Abs(z) <= Metade;
        }

        /// <summary>
        /// Bioma em (<paramref name="x"/>, <paramref name="z"/>): a cidade dentro dos muros e,
        /// fora deles, o bioma de maior peso.
        /// </summary>
        public static Bioma BiomaEm(float x, float z)
        {
            if (Math.Abs(x) <= MetadeDaCidade && Math.Abs(z) <= MetadeDaCidade)
            {
                return Bioma.Cidade;
            }

            return PesosEm(x, z).Dominante;
        }

        /// <summary>Quanto de cada bioma existe em (<paramref name="x"/>, <paramref name="z"/>).</summary>
        public static PesosDosBiomas PesosEm(float x, float z)
        {
            Calcular(x, z, false, out var pesos);
            return pesos;
        }

        /// <summary>
        /// Altura do chão no ponto (<paramref name="i"/>, <paramref name="j"/>) da grade
        /// (<paramref name="i"/> ao longo de X, <paramref name="j"/> ao longo de Z), em metros.
        /// Índices fora da grade são trazidos para a borda.
        /// </summary>
        public static float AlturaNaGrade(int i, int j)
        {
            return AmostrarNaGrade(i, j, out _);
        }

        /// <summary>
        /// Altura do chão e pesos dos biomas no ponto (<paramref name="i"/>,
        /// <paramref name="j"/>) da grade. O client usa para montar a malha e as cores.
        /// </summary>
        public static float AmostrarNaGrade(int i, int j, out PesosDosBiomas pesos)
        {
            var x = CoordenadaDaGrade(Limitar(i, 0, CelulasPorLado));
            var z = CoordenadaDaGrade(Limitar(j, 0, CelulasPorLado));
            return Calcular(x, z, true, out pesos);
        }

        /// <summary>
        /// Altura do chão em (<paramref name="x"/>, <paramref name="z"/>), em metros. É a altura
        /// da malha que o client desenha: cada célula da grade são dois triângulos, divididos pela
        /// diagonal que liga o canto (x + passo, z) ao canto (x, z + passo). Fora do mundo vale a
        /// altura da borda.
        /// </summary>
        public static float AlturaEm(float x, float z)
        {
            var gx = (x + Metade) / PassoDaGrade;
            var gz = (z + Metade) / PassoDaGrade;
            if (!(gx > 0f))
            {
                gx = 0f;
            }

            if (!(gz > 0f))
            {
                gz = 0f;
            }

            if (gx > CelulasPorLado)
            {
                gx = CelulasPorLado;
            }

            if (gz > CelulasPorLado)
            {
                gz = CelulasPorLado;
            }

            var i = Math.Min((int)gx, CelulasPorLado - 1);
            var j = Math.Min((int)gz, CelulasPorLado - 1);
            var fx = gx - i;
            var fz = gz - j;

            var leste = AlturaNaGrade(i + 1, j);
            var norte = AlturaNaGrade(i, j + 1);
            if (fx + fz <= 1f)
            {
                var origem = AlturaNaGrade(i, j);
                return origem + (leste - origem) * fx + (norte - origem) * fz;
            }

            var oposto = AlturaNaGrade(i + 1, j + 1);
            return oposto + (norte - oposto) * (1f - fx) + (leste - oposto) * (1f - fz);
        }

        // Pesos dos biomas e, se pedida, a altura do chão sem interpolar. As fronteiras são
        // medidas num ponto desviado por ruído, para não ficarem retas.
        private static float Calcular(float x, float z, bool comAltura, out PesosDosBiomas pesos)
        {
            var desvioX = (Fbm(x / 260f, z / 260f, SementeDesvioX, 3) - 0.5f) * 520f;
            var desvioZ = (Fbm(x / 260f, z / 260f, SementeDesvioZ, 3) - 0.5f) * 520f;

            // Biomas de fora: quanto do ponto já saiu do miolo de 1 km.
            var foraX = x + desvioX;
            var foraZ = z + desvioZ;
            var fora = Suave(
                MetadeDoMiolo - 40f,
                MetadeDoMiolo + 40f,
                Math.Max(Math.Abs(foraX), Math.Abs(foraZ))
            );
            var dentro = 1f - fora;

            // Miolo, como já era: campo ao norte, mata a leste e colinas a oeste. O desvio é
            // menor, para a divisão continuar perto de onde as peças antigas foram espalhadas.
            var mioloX = x + desvioX * 0.25f;
            var mioloZ = z + desvioZ * 0.25f;
            var campo = Suave(10f, 70f, mioloZ);
            var resto = 1f - campo;
            var mata = resto * Suave(-30f, 30f, mioloX);
            var colinas = resto - mata;

            // Fora: as duas diagonais dividem o mundo em norte (neve), leste (pântano), sul
            // (lava) e oeste (deserto).
            var a = Suave(-90f, 90f, foraZ - foraX);
            var b = Suave(-90f, 90f, foraZ + foraX);
            var neve = a * b;
            var pantano = (1f - a) * b;
            var lava = (1f - a) * (1f - b);
            var deserto = a * (1f - b);

            pesos = new PesosDosBiomas(
                campo * dentro,
                mata * dentro,
                colinas * dentro,
                deserto * fora,
                neve * fora,
                lava * fora,
                pantano * fora
            );

            if (!comAltura)
            {
                return 0f;
            }

            var alcance = Math.Max(Math.Abs(x), Math.Abs(z));
            if (alcance <= MetadeDaCidade)
            {
                return 0f;
            }

            if (alcance <= RaioPlano)
            {
                return AlturaDaFaixaInicial(x, z, alcance);
            }

            var mascara = Suave(RaioPlano, 230f, alcance);
            if (mascara <= 0f)
            {
                return 0f;
            }

            // As montanhas crescem aos poucos a partir da fronteira com o miolo.
            var longe = Suave(520f, 1050f, alcance);

            var altura = 0f;
            if (pesos.Campo > 0f)
            {
                altura += pesos.Campo * AlturaDoCampo(x, z);
            }

            if (pesos.Mata > 0f)
            {
                altura += pesos.Mata * AlturaDaMata(x, z);
            }

            if (pesos.Colinas > 0f)
            {
                altura += pesos.Colinas * AlturaDasColinas(x, z);
            }

            if (pesos.Deserto > 0f)
            {
                altura += pesos.Deserto * AlturaDoDeserto(x, z);
            }

            if (pesos.Neve > 0f)
            {
                altura += pesos.Neve * AlturaDaNeve(x, z, longe);
            }

            if (pesos.Lava > 0f)
            {
                altura += pesos.Lava * AlturaDaLava(x, z, longe);
            }

            if (pesos.Pantano > 0f)
            {
                altura += pesos.Pantano * AlturaDoPantano(x, z);
            }

            // Um morro baixo acompanha cada fronteira: separa os biomas e impede que a água do
            // pântano encontre a lava.
            var puro = deserto * deserto + neve * neve + lava * lava + pantano * pantano;
            var fronteira = 20f * fora * dentro + 18f * fora * (1f - puro);

            // O morro da primeira caverna fica nas colinas do oeste e soma seu relevo ao fim.
            return (mascara * (altura + fronteira)) + PrimeiraCaverna.AlturaDoMorro(x, z);
        }

        /// <summary>
        /// Relevo entre os muros e <see cref="RaioPlano"/>: sobe aos poucos, mais forte na estrada
        /// do portão (norte) e mais suave nas laterais.
        /// </summary>
        private static float AlturaDaFaixaInicial(float x, float z, float alcance)
        {
            var t = Suave(MetadeDaCidade, RaioPlano, alcance);
            var corredor = 1f - Suave(6f, 10f, Math.Abs(x));
            var norte = Suave(MetadeDaCidade - 4f, RaioPlano, z);
            var peso = 0.2f + 0.8f * corredor * Math.Max(norte, 0.12f);
            return SubidaMaximaDaFaixaInicial * t * peso;
        }

        // Campo: quase plano, até 1,5 m.
        private static float AlturaDoCampo(float x, float z)
        {
            return 1.5f * Fbm(x / 90f, z / 90f, SementeCampo, 3);
        }

        // Mata: ondulada, até 3,5 m.
        private static float AlturaDaMata(float x, float z)
        {
            return 3.5f * Fbm(x / 70f, z / 70f, SementeMata, 3);
        }

        // Colinas: morros de até 14 m.
        private static float AlturaDasColinas(float x, float z)
        {
            return 14f * Potencia(Fbm(x / 110f, z / 110f, SementeColinas, 4), 1.3);
        }

        // Deserto: dunas alongadas de 3 a 17 m, mais altas em algumas regiões.
        private static float AlturaDoDeserto(float x, float z)
        {
            var dunas = Crista(x / 150f, z / 90f, SementeDunas, 2);
            var regiao = 0.35f + 0.65f * Fbm(x / 350f, z / 350f, SementeAreia, 2);
            return 3f + 14f * dunas * regiao;
        }

        // Neve: cristas de até uns 155 m, com vales entre elas por onde se passa.
        private static float AlturaDaNeve(float x, float z, float longe)
        {
            var picos = 150f * Potencia(Crista(x / 420f, z / 420f, SementePicosDeNeve, 4), 1.6);
            var encosta = 25f * Fbm(x / 90f, z / 90f, SementeEncostaDeNeve, 3);
            var chao = 5f * Fbm(x / 60f, z / 60f, SementeChaoDeNeve, 2);
            return 8f + longe * (picos + encosta) + chao;
        }

        // Lava: montanhas de até uns 100 m, cortadas por trechos de rio de lava.
        private static float AlturaDaLava(float x, float z, float longe)
        {
            var picos = 90f * Potencia(Crista(x / 360f, z / 360f, SementePicosDeLava, 4), 1.5);
            var encosta = 14f * Fbm(x / 80f, z / 80f, SementeEncostaDeLava, 3);
            var chao = 4f * Fbm(x / 50f, z / 50f, SementeChaoDeLava, 2);
            var altura = 6f + longe * (picos + encosta) + chao;

            // O rio segue a linha em que um ruído largo cruza a metade. Um segundo ruído corta o
            // rio em trechos, para sobrar terra por onde atravessar.
            var linha = Math.Abs(2f * Fbm(x / 260f, z / 260f, SementeRios, 2) - 1f);
            var rio = 1f - Suave(0.03f, 0.11f, linha);
            rio *= Suave(0.38f, 0.5f, Fbm(x / 190f, z / 190f, SementeTrechos, 2));
            return altura + (-1.6f - altura) * rio;
        }

        // Pântano: ilhas baixas (até 1,6 m) e poças rasas (até 0,9 m abaixo de zero).
        private static float AlturaDoPantano(float x, float z)
        {
            return -0.9f + 2.5f * Suave(0.38f, 0.62f, Fbm(x / 55f, z / 55f, SementePantano, 3));
        }

        private static int Limitar(int valor, int minimo, int maximo)
        {
            return valor < minimo ? minimo : (valor > maximo ? maximo : valor);
        }

        private static float Potencia(float valor, double expoente)
        {
            return (float)Math.Pow(valor, expoente);
        }

        // Curva suave de 0 a 1 entre "de" e "ate".
        private static float Suave(float de, float ate, float valor)
        {
            var t = (valor - de) / (ate - de);
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            return t * t * (3f - 2f * t);
        }

        // Soma de oitavas de ruído, de 0 a 1.
        private static float Fbm(float x, float z, uint semente, int oitavas)
        {
            var soma = 0f;
            var total = 0f;
            var amplitude = 0.5f;
            for (var k = 0; k < oitavas; k++)
            {
                soma += Ruido(x, z, semente + (uint)k * 101u) * amplitude;
                total += amplitude;
                amplitude *= 0.5f;
                x *= 2f;
                z *= 2f;
            }

            return soma / total;
        }

        // Como o Fbm, mas cada oitava vira uma crista (o meio do ruído é o topo), de 0 a 1.
        private static float Crista(float x, float z, uint semente, int oitavas)
        {
            var soma = 0f;
            var total = 0f;
            var amplitude = 0.5f;
            for (var k = 0; k < oitavas; k++)
            {
                var crista = 1f - Math.Abs(2f * Ruido(x, z, semente + (uint)k * 101u) - 1f);
                soma += crista * crista * amplitude;
                total += amplitude;
                amplitude *= 0.5f;
                x *= 2f;
                z *= 2f;
            }

            return soma / total;
        }

        // Ruído de valor: sorteios nos pontos inteiros, interpolados com curva suave.
        private static float Ruido(float x, float z, uint semente)
        {
            var baseX = (float)Math.Floor(x);
            var baseZ = (float)Math.Floor(z);
            var ix = (int)baseX;
            var iz = (int)baseZ;
            var fx = x - baseX;
            var fz = z - baseZ;
            var sx = fx * fx * (3f - 2f * fx);
            var sz = fz * fz * (3f - 2f * fz);

            var a = Sorteio(ix, iz, semente);
            var b = Sorteio(ix + 1, iz, semente);
            var c = Sorteio(ix, iz + 1, semente);
            var d = Sorteio(ix + 1, iz + 1, semente);
            var ab = a + (b - a) * sx;
            var cd = c + (d - c) * sx;
            return ab + (cd - ab) * sz;
        }

        // Número de 0 a 1 que só depende das coordenadas e da semente. Contas com inteiros, para
        // o resultado ser o mesmo em qualquer plataforma.
        private static float Sorteio(int x, int z, uint semente)
        {
            unchecked
            {
                var h = (uint)x * 374761393u + (uint)z * 668265263u + semente * 2246822519u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFFu) / 16777216f;
            }
        }
    }
}
