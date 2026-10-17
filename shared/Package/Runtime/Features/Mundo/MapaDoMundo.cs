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

        /// <summary>Cidade do passo, no vale ao sul da serra nevada.</summary>
        public const float CidadeDaNeveX = -520f;
        public const float CidadeDaNeveZ = 320f;
        public const float CidadeDaNeveAltura = 28f;

        /// <summary>Oásis da terceira cidade, no deserto oeste.</summary>
        public const float CidadeDoDesertoX = -900f;
        public const float CidadeDoDesertoZ = -620f;
        public const float CidadeDoDesertoAltura = 8f;

        /// <summary>Raio dos pátios das duas cidades externas.</summary>
        public const float RaioCidadeSecundaria = 42f;

        /// <summary>Folga suavizada entre os pátios e o terreno natural.</summary>
        public const float BordaCidadeSecundaria = 30f;

        /// <summary>
        /// Até esta distância do centro (a maior entre |x| e |z|) a chegada à cidade ainda não
        /// entrou no relevo dos biomas distantes: dentro dos muros é plano; entre os muros e este
        /// raio há uma subida leve (a estrada do portão acompanha).
        /// </summary>
        public const float RaioPlano = 130f;

        /// <summary>Raio da primeira transição ao sair dos muros (40 m a 60 m: 20 m de faixa).</summary>
        public const float RaioTransicao1 = 60f;

        /// <summary>Raio da segunda transição intermediária mais suave (60 m a 120 m).</summary>
        public const float RaioTransicao2 = 120f;

        /// <summary>Subida máxima na faixa entre os muros e <see cref="RaioPlano"/>, em metros.</summary>
        public const float SubidaMaximaDaFaixaInicial = 5f;

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

        private static RegiaoDeBioma[] _regioesDePrevia;

        /// <summary>Mostra um desenho ainda não salvo somente no editor Unity.</summary>
        public static void DefinirRegioesDePrevia(RegiaoDeBioma[] regioes)
        {
            _regioesDePrevia = regioes == null ? null : (RegiaoDeBioma[])regioes.Clone();
        }

        private static RegiaoDeBioma[] RegioesAtivas
        {
            get => _regioesDePrevia ?? RegioesDoMundo.Todas;
        }

        /// <summary>Regiões salvas ou em prévia para gerar detalhes dentro de seus contornos.</summary>
        public static RegiaoDeBioma[] CopiarRegioesAtivas()
        {
            return (RegiaoDeBioma[])RegioesAtivas.Clone();
        }

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

        // As formas da planta sao fixas: preparar seno e cosseno uma vez evita
        // milhares de chamadas trigonometricas durante a geracao de cada mapa.
        private static readonly ManchaEliptica NeveNoroeste = new ManchaEliptica(
            -920f,
            1030f,
            900f,
            760f,
            -0.22f
        );
        private static readonly ManchaEliptica NeveNorte = new ManchaEliptica(
            -130f,
            1340f,
            730f,
            390f,
            0.22f
        );
        private static readonly ManchaEliptica NeveOriental = new ManchaEliptica(
            580f,
            1160f,
            430f,
            330f,
            -0.27f
        );
        private static readonly ManchaEliptica NeveDoPasso = new ManchaEliptica(
            -570f,
            450f,
            280f,
            480f,
            -0.30f
        );
        private static readonly ManchaEliptica DesertoSul = new ManchaEliptica(
            -1080f,
            -680f,
            920f,
            1030f,
            0.12f
        );
        private static readonly ManchaEliptica DesertoOeste = new ManchaEliptica(
            -1350f,
            270f,
            390f,
            720f,
            -0.23f
        );
        private static readonly ManchaEliptica PantanoLeste = new ManchaEliptica(
            1090f,
            250f,
            760f,
            1040f,
            0.13f
        );
        private static readonly ManchaEliptica PantanoSul = new ManchaEliptica(
            730f,
            -480f,
            490f,
            650f,
            -0.28f
        );
        private static readonly ManchaEliptica PantanoNorte = new ManchaEliptica(
            990f,
            1130f,
            470f,
            470f,
            0.15f
        );
        private static readonly ManchaEliptica LavaSul = new ManchaEliptica(
            790f,
            -1180f,
            1300f,
            760f,
            -0.16f
        );
        private static readonly ManchaEliptica LavaLeste = new ManchaEliptica(
            1300f,
            -580f,
            370f,
            560f,
            0.20f
        );
        private static readonly ManchaEliptica ClareiraSul = new ManchaEliptica(
            160f,
            -180f,
            260f,
            220f,
            0.28f
        );
        private static readonly ManchaEliptica ClareiraCentro = new ManchaEliptica(
            -110f,
            60f,
            180f,
            130f,
            -0.22f
        );
        private static readonly ManchaEliptica ClareiraDoPortao = new ManchaEliptica(
            0f,
            95f,
            90f,
            115f,
            0.14f
        );
        private static readonly ManchaEliptica ColinasOeste = new ManchaEliptica(
            -450f,
            180f,
            300f,
            680f,
            -0.23f
        );
        private static readonly ManchaEliptica ColinasSul = new ManchaEliptica(
            -530f,
            -440f,
            390f,
            330f,
            0.30f
        );
        private static readonly ManchaEliptica ColinasDaCaverna = new ManchaEliptica(
            -270f,
            -100f,
            260f,
            200f,
            0.18f
        );

        private static readonly (float Z, float X)[] TrajetoRioPrincipal =
        {
            (-650f, 1030f),
            (-400f, 800f),
            (-200f, 590f),
            (0f, 430f),
            (200f, 340f),
            (400f, 300f),
            (600f, 210f),
            (800f, 70f),
            (1000f, -80f),
            (1200f, -240f),
            (1500f, -310f),
        };

        private static readonly (float Z, float X)[] TrajetoAfluenteDaNeve =
        {
            (400f, 300f),
            (600f, -110f),
            (800f, -430f),
            (1000f, -600f),
            (1200f, -760f),
        };

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

        /// <summary>Área reservada para as duas cidades externas e sua borda vegetada.</summary>
        public static bool PertoDeCidadeSecundaria(float x, float z, float folga = 0f)
        {
            var raio = RaioCidadeSecundaria + BordaCidadeSecundaria + folga;
            var dxNeve = x - CidadeDaNeveX;
            var dzNeve = z - CidadeDaNeveZ;
            var dxDeserto = x - CidadeDoDesertoX;
            var dzDeserto = z - CidadeDoDesertoZ;
            return dxNeve * dxNeve + dzNeve * dzNeve <= raio * raio
                || dxDeserto * dxDeserto + dzDeserto * dzDeserto <= raio * raio;
        }

        /// <summary>
        /// Bioma em (<paramref name="x"/>, <paramref name="z"/>): a cidade dentro dos muros e,
        /// fora deles, o bioma de maior peso.
        /// </summary>
        public static Bioma BiomaEm(float x, float z)
        {
            if (_regioesDePrevia != null && CoberturaDaAutoriaEm(x, z) > 0f)
            {
                Calcular(x, z, false, out var pesosPrevia);
                return pesosPrevia.Dominante;
            }

            if (DadosDoMapaFeitoAMao.Disponivel)
            {
                DadosDoMapaFeitoAMao.TentarAmostrar(
                    IndiceDaGrade(x),
                    IndiceDaGrade(z),
                    out _,
                    out var biomaExportado,
                    out _
                );
                return biomaExportado;
            }

            if (Math.Abs(x) <= MetadeDaCidade && Math.Abs(z) <= MetadeDaCidade)
            {
                return Bioma.Cidade;
            }

            return PesosEm(x, z).Dominante;
        }

        /// <summary>Quanto de cada bioma existe em (<paramref name="x"/>, <paramref name="z"/>).</summary>
        public static PesosDosBiomas PesosEm(float x, float z)
        {
            if (_regioesDePrevia != null && CoberturaDaAutoriaEm(x, z) > 0f)
            {
                Calcular(x, z, false, out var pesosPrevia);
                return pesosPrevia;
            }

            if (DadosDoMapaFeitoAMao.Disponivel)
            {
                return PesosDoBioma(BiomaEm(x, z));
            }

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
            if (_regioesDePrevia != null && CoberturaDaAutoriaEm(x, z) > 0f)
            {
                return Calcular(x, z, true, out pesos);
            }

            if (DadosDoMapaFeitoAMao.TentarAmostrar(i, j, out var altura, out var bioma, out _))
            {
                pesos = PesosDoBioma(bioma);
                return altura;
            }

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
            float altura;
            if (fx + fz <= 1f)
            {
                var origem = AlturaNaGrade(i, j);
                altura = origem + (leste - origem) * fx + (norte - origem) * fz;
            }
            else
            {
                var oposto = AlturaNaGrade(i + 1, j + 1);
                altura = oposto + (norte - oposto) * (1f - fx) + (leste - oposto) * (1f - fz);
            }

            return DadosDoMapaFeitoAMao.Disponivel
                ? ModificadoresDoMapa.AplicarDinamicos(x, z, altura)
                : altura;
        }

        // Pesos dos biomas e, se pedida, a altura do chão sem interpolar. As fronteiras são
        // medidas num ponto desviado por ruído, para não ficarem retas.
        private static float Calcular(
            float x,
            float z,
            bool comAltura,
            out PesosDosBiomas pesos,
            bool aplicarEstrada = true
        )
        {
            pesos = PesosDoNovoMundo(x, z);
            var intensidadeDoRelevo = 1f;
            var regioes = RegioesAtivas;
            for (var indice = 0; indice < regioes.Length; indice++)
            {
                var regiao = regioes[indice];
                var cobertura = CoberturaPermitida(regiao, x, z);
                if (cobertura <= 0f)
                {
                    continue;
                }

                pesos = regiao.Aplicar(pesos, cobertura);
                intensidadeDoRelevo +=
                    (regiao.IntensidadeDoRelevo - intensidadeDoRelevo) * cobertura;
            }

            if (!comAltura)
            {
                return 0f;
            }

            var alcance = Math.Max(Math.Abs(x), Math.Abs(z));
            if (alcance <= MetadeDaCidade)
            {
                return 0f;
            }

            // Transição 1 (grudada no muro: 40 m a 60 m) e Transição 2 (intermediária: 60 m a 120 m).
            var alturaFaixa = AlturaDasTransicoes(x, z, alcance);

            // A partir de 85 m o relevo do bioma começa a se misturar, atingindo 100% em 130 m.
            var mascara = Suave(85f, 130f, alcance);

            // Relevo vivo imediato: as montanhas e platôs começam a subir logo após as transições da cidade (a partir de 100 m), atingindo porte pleno em 480 m.
            var longe = Suave(100f, 480f, alcance);

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

            altura *= intensidadeDoRelevo;

            // A divisoria vulcanica conserva um bordo seco antes do delta pantanoso.
            altura += 8f * pesos.Lava * pesos.Pantano;
            if (ProximoDeRio(x, z, out var dRio) && pesos.Lava < 0.4f)
            {
                var calha = 1f - Suave(16f, 60f, dRio);
                var fundoRio = z < 300f ? -0.92f : NivelDoRioAlto(z) - 0.42f;
                altura = (1f - calha) * altura + calha * fundoRio;
            }

            // Vau raso no unico cruzamento da trilha oriental com o rio principal.
            // O chao oficial sobe junto no shared; o servidor aceita a mesma travessia.
            if (x > 300f && x < 480f && z > 60f && z < 160f)
            {
                var eixo = CaminhosDoMundo.DistanciaAteCaminho(x, z);
                var vau =
                    Suave(300f, 325f, x)
                    * (1f - Suave(455f, 480f, x))
                    * (1f - Suave(8f, 20f, eixo));
                altura = Math.Max(altura, altura * (1f - vau) + (NivelDaAgua - 0.15f) * vau);
            }

            // Vale de acesso ao Passo: a serra se aproxima da vila sem criar um poco
            // circular ou uma parede de subida na chegada pelas trilhas.
            var dxPasso = x - CidadeDaNeveX;
            var dzPasso = z - CidadeDaNeveZ;
            var distanciaPasso = (float)Math.Sqrt(dxPasso * dxPasso + dzPasso * dzPasso);
            var entradaDoVale = Suave(65f, 380f, distanciaPasso);
            altura = CidadeDaNeveAltura * (1f - entradaDoVale) + altura * entradaDoVale;

            // O relevo base une a transição das bordas da cidade com o bioma natural.
            var alturaBase = (1f - mascara) * alturaFaixa + mascara * altura;
            var comCaverna = alturaBase + PrimeiraCaverna.AlturaDoMorro(x, z);

            // Modificadores de relevo (monumentos do mundo e terraplanagens em tempo de execução)
            var alturaFinal = ModificadoresDoMapa.Aplicar(x, z, comCaverna);
            return aplicarEstrada
                ? EstradaDoPasso.Tracado.AplicarEstrada(x, z, alturaFinal, AlturaBaseDaAutoriaEm)
                : alturaFinal;
        }

        /// <summary>Altura anterior ao tracado autoral, para a previa da ferramenta no editor.</summary>
        public static float AlturaBaseDaAutoriaEm(float x, float z)
        {
            if (DadosDoMapaFeitoAMao.Disponivel)
            {
                return AlturaEm(x, z);
            }

            return Calcular(x, z, true, out _, false);
        }

        /// <summary>Áreas especiais do ponto mais próximo da grade publicada.</summary>
        public static AreaEspecialDoMapa AreasEspeciaisEm(float x, float z)
        {
            if (DadosDoMapaFeitoAMao.Disponivel)
            {
                DadosDoMapaFeitoAMao.TentarAmostrar(
                    IndiceDaGrade(x),
                    IndiceDaGrade(z),
                    out _,
                    out _,
                    out var areas
                );
                return areas;
            }

            var bioma = BiomaEm(x, z);
            var areasPadrao =
                bioma == Bioma.Cidade ? AreaEspecialDoMapa.Cidade : AreaEspecialDoMapa.Nenhuma;
            if (bioma == Bioma.Pantano && AlturaEm(x, z) < NivelDaAgua)
            {
                areasPadrao |= AreaEspecialDoMapa.Agua | AreaEspecialDoMapa.Pantano;
            }

            if (bioma == Bioma.Lava && AlturaEm(x, z) < NivelDaLava)
            {
                areasPadrao |= AreaEspecialDoMapa.Lava;
            }

            return areasPadrao;
        }

        private static int IndiceDaGrade(float coordenada)
        {
            return Limitar(
                (int)Math.Round((coordenada + Metade) / PassoDaGrade),
                0,
                CelulasPorLado
            );
        }

        private static PesosDosBiomas PesosDoBioma(Bioma bioma)
        {
            if (bioma == Bioma.Cidade)
            {
                return new PesosDosBiomas(1f, 0f, 0f, 0f, 0f, 0f, 0f);
            }

            return new PesosDosBiomas(
                bioma == Bioma.Campo ? 1f : 0f,
                bioma == Bioma.Mata ? 1f : 0f,
                bioma == Bioma.Colinas ? 1f : 0f,
                bioma == Bioma.Deserto ? 1f : 0f,
                bioma == Bioma.Neve ? 1f : 0f,
                bioma == Bioma.Lava ? 1f : 0f,
                bioma == Bioma.Pantano ? 1f : 0f
            );
        }

        /// <summary>Densidade autoral relativa da vegetação no ponto (1 é a distribuição padrão).</summary>
        public static float DensidadeDaVegetacaoEm(float x, float z)
        {
            var densidade = 1f;
            var regioes = RegioesAtivas;
            for (var indice = 0; indice < regioes.Length; indice++)
            {
                var regiao = regioes[indice];
                var cobertura = CoberturaPermitida(regiao, x, z);
                densidade += (regiao.DensidadeDaVegetacao - densidade) * cobertura;
            }

            return densidade;
        }

        /// <summary>Quanto as árvores entram no chão no ponto, misturado pelas regiões autorais.</summary>
        public static float EnterroDaVegetacaoEm(float x, float z)
        {
            var enterro = 0.15f;
            var regioes = RegioesAtivas;
            for (var indice = 0; indice < regioes.Length; indice++)
            {
                var regiao = regioes[indice];
                var cobertura = CoberturaPermitida(regiao, x, z);
                enterro += (regiao.EnterroDaVegetacao - enterro) * cobertura;
            }

            return enterro;
        }

        /// <summary>Parte já redesenhada por alguma região autoral, inclusive na transição.</summary>
        public static float CoberturaDaAutoriaEm(float x, float z)
        {
            var cobertura = 0f;
            var regioes = RegioesAtivas;
            for (var indice = 0; indice < regioes.Length; indice++)
            {
                cobertura = Math.Max(cobertura, CoberturaPermitida(regioes[indice], x, z));
            }

            return cobertura;
        }

        private static float CoberturaPermitida(RegiaoDeBioma regiao, float x, float z)
        {
            var cobertura = regiao.CoberturaEm(x, z);
            if (cobertura <= 0f)
            {
                return 0f;
            }

            // As praças mantêm altura e solo; a autoria entra sem criar um anel brusco.
            var alcance = Math.Max(Math.Abs(x), Math.Abs(z));
            var distanciaPasso = (float)
                Math.Sqrt(
                    (x - CidadeDaNeveX) * (x - CidadeDaNeveX)
                        + (z - CidadeDaNeveZ) * (z - CidadeDaNeveZ)
                );
            var distanciaOasis = (float)
                Math.Sqrt(
                    (x - CidadeDoDesertoX) * (x - CidadeDoDesertoX)
                        + (z - CidadeDoDesertoZ) * (z - CidadeDoDesertoZ)
                );
            var raioVila = RaioCidadeSecundaria + BordaCidadeSecundaria;
            return cobertura
                * Suave(RaioPlano, RaioPlano + 30f, alcance)
                * Suave(raioVila, raioVila + 30f, distanciaPasso)
                * Suave(raioVila, raioVila + 30f, distanciaOasis);
        }

        // Planta autoral nova: a mata liga os tres assentamentos; neve, deserto, pantano e
        // caldeira formam massas irregulares, com prolongamentos e vales, sem setores radiais.
        private static PesosDosBiomas PesosDoNovoMundo(float x, float z)
        {
            var xDesviado = x + 220f * (Fbm(x / 440f, z / 440f, SementeDesvioX, 3) - 0.5f);
            var zDesviado = z + 220f * (Fbm(x / 440f, z / 440f, SementeDesvioZ, 3) - 0.5f);

            var neve = Math.Max(
                NeveNoroeste.PesoEm(xDesviado, zDesviado),
                NeveNorte.PesoEm(xDesviado, zDesviado)
            );
            neve = Math.Max(neve, 0.84f * NeveOriental.PesoEm(xDesviado, zDesviado));
            // O povoado fica no pe nevado da serra, nao numa clareira marrom isolada.
            neve = Math.Max(neve, 0.92f * NeveDoPasso.PesoEm(xDesviado, zDesviado));
            var deserto = Math.Max(
                DesertoSul.PesoEm(xDesviado, zDesviado),
                DesertoOeste.PesoEm(xDesviado, zDesviado)
            );
            var pantano = Math.Max(
                PantanoLeste.PesoEm(xDesviado, zDesviado),
                PantanoSul.PesoEm(xDesviado, zDesviado)
            );
            pantano = Math.Max(pantano, PantanoNorte.PesoEm(xDesviado, zDesviado));
            if (z > -650f && z < 320f)
            {
                var deltaDoRio =
                    (1f - Suave(55f, 290f, Math.Abs(x - CentroDoRioGlacial(z))))
                    * Suave(-650f, -500f, z)
                    * (1f - Suave(100f, 320f, z));
                pantano = Math.Max(pantano, 0.53f * deltaDoRio);
            }
            var lava = Math.Max(
                LavaSul.PesoEm(xDesviado, zDesviado),
                LavaLeste.PesoEm(xDesviado, zDesviado)
            );

            var mataLivre = (1f - neve) * (1f - deserto) * (1f - pantano) * (1f - lava);
            var clareira = Math.Max(
                Math.Max(
                    0.85f * ClareiraSul.PesoEm(xDesviado, zDesviado),
                    0.57f * ClareiraCentro.PesoEm(xDesviado, zDesviado)
                ),
                0.84f * ClareiraDoPortao.PesoEm(x, z)
            );
            var colinas =
                0.78f
                * Math.Max(
                    Math.Max(
                        ColinasOeste.PesoEm(xDesviado, zDesviado),
                        ColinasSul.PesoEm(xDesviado, zDesviado)
                    ),
                    ColinasDaCaverna.PesoEm(xDesviado, zDesviado)
                );
            var pesoColinas = mataLivre * colinas;
            var pesoCampo = (mataLivre - pesoColinas) * clareira;
            var pesoMata = mataLivre - pesoColinas - pesoCampo;
            var soma = pesoMata + pesoCampo + pesoColinas + neve + deserto + pantano + lava;
            return new PesosDosBiomas(
                pesoCampo / soma,
                pesoMata / soma,
                pesoColinas / soma,
                deserto / soma,
                neve / soma,
                lava / soma,
                pantano / soma
            );
        }

        private readonly struct ManchaEliptica
        {
            private readonly float _centroX;
            private readonly float _centroZ;
            private readonly float _raioX;
            private readonly float _raioZ;
            private readonly float _cosseno;
            private readonly float _seno;

            public ManchaEliptica(
                float centroX,
                float centroZ,
                float raioX,
                float raioZ,
                float giro
            )
            {
                _centroX = centroX;
                _centroZ = centroZ;
                _raioX = raioX;
                _raioZ = raioZ;
                _cosseno = (float)Math.Cos(giro);
                _seno = (float)Math.Sin(giro);
            }

            public float PesoEm(float x, float z)
            {
                var dx = x - _centroX;
                var dz = z - _centroZ;
                var u = (dx * _cosseno + dz * _seno) / _raioX;
                var v = (dz * _cosseno - dx * _seno) / _raioZ;
                var distancia = (float)Math.Sqrt(u * u + v * v);
                return 1f - Suave(0.65f, 1.02f, distancia);
            }
        }

        /// <summary>
        /// Indica se as coordenadas estão dentro da calha ou margem de um rio navegável/visível.
        /// Devolve a distância perpendicular à calha mais próxima em <paramref name="distanciaRio"/>.
        /// </summary>
        public static bool ProximoDeRio(float x, float z, out float distanciaRio)
        {
            // A cidade e sua transição imediata (60 m) não são cortadas por rios
            if (Math.Abs(x) <= 60f && Math.Abs(z) <= 60f)
            {
                distanciaRio = float.MaxValue;
                return false;
            }

            // Protege a área da Primeira Caverna (-220, -47) para não furar o teto do túnel
            var dxCav = x - (-220f);
            var dzCav = z - (-47f);
            if (dxCav * dxCav + dzCav * dzCav <= 55f * 55f)
            {
                distanciaRio = float.MaxValue;
                return false;
            }

            // Rio principal: nasce na serra, contorna a cidade florestal pelo leste e termina no delta.
            var xRio1 = CentroDoRioGlacial(z);
            var d1 = (z >= -650f && z <= 1500f) ? Math.Abs(x - xRio1) : float.MaxValue;

            // Afluente da neve: chega ao rio principal ao norte da cidade, sem um canal paralelo.
            var xRio2 = CentroDoRioDasCascatas(z);
            var d2 = (z >= 400f && z <= 1200f) ? Math.Abs(x - xRio2) : float.MaxValue;

            distanciaRio = Math.Min(d1, d2);
            return distanciaRio <= 60f;
        }

        /// <summary>Nivel da superficie dos rios norte, com descida continua ate as lagoas.</summary>
        public static float NivelDoRioAlto(float z)
        {
            return NivelDaAgua + 0.015f * (z - 300f);
        }

        /// <summary>Meia largura visivel dos rios altos, com variacao suave ao longo do curso.</summary>
        public static float MeiaLarguraDoRioAlto(float z)
        {
            return 10f + 1.4f * (float)Math.Sin(z / 39f) + 1.3f * (float)Math.Sin(z / 91f);
        }

        /// <summary>Se o ponto esta sob a superficie de um dos rios altos.</summary>
        public static bool DentroDoRioAlto(float x, float z)
        {
            return z >= 300f
                && ProximoDeRio(x, z, out var distanciaRio)
                && distanciaRio <= MeiaLarguraDoRioAlto(z);
        }

        /// <summary>Coordenada X do leito do rio glacial na altura Z indicada.</summary>
        public static float CentroDoRioGlacial(float z)
        {
            return CentroInterpolado(z, TrajetoRioPrincipal);
        }

        /// <summary>Coordenada X do leito do rio das cascatas na altura Z indicada.</summary>
        public static float CentroDoRioDasCascatas(float z)
        {
            return CentroInterpolado(z, TrajetoAfluenteDaNeve);
        }

        private static float CentroInterpolado(float z, (float Z, float X)[] pontos)
        {
            if (z <= pontos[0].Z)
            {
                return pontos[0].X;
            }

            var ultimo = pontos.Length - 1;
            if (z >= pontos[ultimo].Z)
            {
                return pontos[ultimo].X;
            }

            for (var i = 0; i < ultimo; i++)
            {
                var inicio = pontos[i];
                var fim = pontos[i + 1];
                if (z > fim.Z)
                {
                    continue;
                }

                var anterior = pontos[Math.Max(0, i - 1)];
                var proximo = pontos[Math.Min(ultimo, i + 2)];
                var intervalo = fim.Z - inicio.Z;
                var t = (z - inicio.Z) / intervalo;
                var tangenteInicial = intervalo * (fim.X - anterior.X) / (fim.Z - anterior.Z);
                var tangenteFinal = intervalo * (proximo.X - inicio.X) / (proximo.Z - inicio.Z);
                var t2 = t * t;
                var t3 = t2 * t;
                return (2f * t3 - 3f * t2 + 1f) * inicio.X
                    + (t3 - 2f * t2 + t) * tangenteInicial
                    + (-2f * t3 + 3f * t2) * fim.X
                    + (t3 - t2) * tangenteFinal;
            }

            return pontos[ultimo].X;
        }

        /// <summary>
        /// Duas transições do relevo ao redor da cidade:
        /// 1) Grudada no muro (40 a 60 m): acomodação imediata com subida na estrada norte.
        /// 2) Intermediária suave (60 a 120 m): fusão suave antes de entrar no relevo pleno dos biomas.
        /// </summary>
        private static float AlturaDasTransicoes(float x, float z, float alcance)
        {
            var corredor = 1f - Suave(6f, 10f, Math.Abs(x));
            var norte = Suave(MetadeDaCidade - 4f, RaioTransicao2, z);
            var pesoNorte = 0.2f + 0.8f * corredor * Math.Max(norte, 0.12f);

            if (alcance <= RaioTransicao1)
            {
                // Transição 1 (40 m a 60 m - primeiros 20 m fora dos muros)
                var t1 = Suave(MetadeDaCidade, RaioTransicao1, alcance);
                return SubidaMaximaDaFaixaInicial * 0.5f * t1 * pesoNorte;
            }

            // Transição 2 (60 m a 120 m)
            var baseT1 = SubidaMaximaDaFaixaInicial * 0.5f * pesoNorte;
            var t2 = Suave(RaioTransicao1, RaioTransicao2, alcance);
            return baseT1 + (SubidaMaximaDaFaixaInicial * 0.5f * t2 * pesoNorte);
        }

        // Campo: ondulações campestres vivas de 2 a 6,5 m.
        private static float AlturaDoCampo(float x, float z)
        {
            return 2f + 4.5f * Fbm(x / 80f, z / 80f, SementeCampo, 3);
        }

        // Mata: platôs florestais escalonados em degraus e cascatas de 3 a 18 m.
        private static float AlturaDaMata(float x, float z)
        {
            var patamares = Degrau(Fbm(x / 120f, z / 120f, SementeMata, 4) * 12f, 4f);
            var ondulacao = 3f * Fbm(x / 50f, z / 50f, SementeMata + 13u, 2);
            return 3f + patamares + ondulacao;
        }

        // Colinas: morros baixos perto da cidade, com dorsais rochosas rumo ao deserto.
        private static float AlturaDasColinas(float x, float z)
        {
            var morros = 13f * Potencia(Fbm(x / 110f, z / 110f, SementeColinas, 4), 1.35);
            var distancia = Math.Max(Math.Abs(x), Math.Abs(z));
            var dorsais =
                12f
                * Crista(x / 120f, z / 160f, SementeColinas + 17u, 3)
                * Suave(220f, 390f, distancia);
            var rochaExposta =
                2.8f
                * (Crista(x / 28f, z / 34f, SementeColinas + 31u, 2) - 0.45f)
                * Suave(260f, 460f, distancia);
            return morros + dorsais + rochaExposta;
        }

        // Deserto: dunas e degraus de cânion de 3 a 20 m.
        private const float AlturaDaGrandeDunaOeste = 60f;
        private const float AlturaDaGrandeDunaSul = 42f;

        private static float AlturaDoDeserto(float x, float z)
        {
            var dunas = Fbm(x / 140f, z / 90f, SementeDunas, 3);
            var regiao = 0.35f + 0.65f * Fbm(x / 350f, z / 350f, SementeAreia, 2);
            var dunaOeste = DunaSuave(x, z, -1350f, -570f, 500f, 580f);
            var dunaSul = DunaSuave(x, z, -720f, -1130f, 410f, 370f);
            return 3f
                + 14f * dunas * regiao
                + AlturaDaGrandeDunaOeste * dunaOeste
                + AlturaDaGrandeDunaSul * dunaSul;
        }

        private static float DunaSuave(
            float x,
            float z,
            float centroX,
            float centroZ,
            float raioX,
            float raioZ
        )
        {
            var dx = (x - centroX) / raioX;
            var dz = (z - centroZ) / raioZ;
            var distancia = (float)Math.Sqrt(dx * dx + dz * dz);
            return 1f - Suave(0f, 1f, distancia);
        }

        // Neve: montanhas imponentes com cumes arredondados e orgânicos (sem picos pontudos em V).
        private static float AlturaDaNeve(float x, float z, float longe)
        {
            var cumes = 145f * Potencia(Fbm(x / 380f, z / 380f, SementePicosDeNeve, 4), 1.9);
            var encosta = 25f * Fbm(x / 110f, z / 110f, SementeEncostaDeNeve, 3);
            var chao = 4f * Fbm(x / 60f, z / 60f, SementeChaoDeNeve, 2);
            var erosao = 4.5f * (Crista(x / 45f, z / 55f, SementeEncostaDeNeve + 17u, 3) - 0.35f);
            var rochaExposta =
                3.5f * (Crista(x / 30f, z / 38f, SementeEncostaDeNeve + 29u, 2) - 0.45f);
            return 5f + longe * (cumes + encosta + erosao + rochaExposta) + chao;
        }

        // Lava: montanhas esculturais de até 95 m com cumes suaves, cortadas por trechos de rio de lava.
        private static float AlturaDaLava(float x, float z, float longe)
        {
            var picos = 85f * Potencia(Fbm(x / 350f, z / 350f, SementePicosDeLava, 4), 1.8);
            var encosta = 16f * Fbm(x / 90f, z / 90f, SementeEncostaDeLava, 3);
            var chao = 4f * Fbm(x / 50f, z / 50f, SementeChaoDeLava, 2);
            var erosao = 5f * (Crista(x / 40f, z / 50f, SementeEncostaDeLava + 17u, 3) - 0.35f);
            var rochaExposta =
                3.5f * (Crista(x / 28f, z / 36f, SementeEncostaDeLava + 29u, 2) - 0.45f);
            var altura = 6f + longe * (picos + encosta + erosao + rochaExposta) + chao;

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

        // Função de platô/degrau suave para modelar patamares e terraços escalonados
        private static float Degrau(float valor, float passo)
        {
            var b = (float)Math.Floor(valor / passo);
            var f = (valor / passo) - b;
            var s = f * f * (3f - 2f * f);
            return (b + s) * passo;
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
