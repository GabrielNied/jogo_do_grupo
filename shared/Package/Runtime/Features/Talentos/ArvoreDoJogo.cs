using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>
    /// A árvore de atributos do jogo (#627, números propostos pela simulação em
    /// <c>tools/JogoDoGrupo.Simulacoes</c> e aprovados no review do PR). Cada caminho tem três
    /// nós principais encadeados, dois secundários, um nó grande e um marco avançado; entre
    /// caminhos vizinhos há um híbrido. Todos os números são constantes nomeadas: mudar um
    /// é mudar só aqui, e quem muda a árvore sobe <see cref="Versao"/> (a migração zera e
    /// reembolsa, ver <c>talentos.md</c>).
    /// </summary>
    public static class ArvoreDoJogo
    {
        /// <summary>Versão da configuração. 1 era a árvore mínima de teste.</summary>
        public const int Versao = 2;

        // --- Nós principais (3 por caminho) ------------------------------------------------

        public const int RankMaximoPrincipal = 5;

        /// <summary>Custo do 1º ao 5º rank de um nó principal (9 pontos no total).</summary>
        public static readonly int[] CustoPrincipal = { 1, 1, 2, 2, 3 };

        /// <summary>Atributo que cada rank de um nó principal soma.</summary>
        public const int ValorPorRankPrincipal = 2;

        /// <summary>Rank que o nó anterior do caminho precisa ter para abrir o próximo.</summary>
        public const int RankParaAbrirProximo = 3;

        // --- Nós secundários (2 por caminho) -----------------------------------------------

        public const int RankMaximoSecundario = 3;

        /// <summary>Custo do 1º ao 3º rank de um nó secundário (7 pontos no total).</summary>
        public static readonly int[] CustoSecundario = { 2, 2, 3 };

        /// <summary>Rank do 1º e do 2º nó principal que abre o 1º e o 2º secundário.</summary>
        public const int RankParaAbrirSecundario = 2;

        // --- Nó grande (1 por caminho) -----------------------------------------------------

        public const int RankMaximoGrande = 3;

        /// <summary>Custo do 1º ao 3º rank do nó grande (15 pontos no total).</summary>
        public static readonly int[] CustoGrande = { 4, 5, 6 };

        /// <summary>Atributo do próprio caminho que cada rank do nó grande soma.</summary>
        public const int ValorPorRankGrande = 2;

        /// <summary>Rank do 3º nó principal que abre o nó grande.</summary>
        public const int RankParaAbrirGrande = 3;

        // --- Marco avançado (1 por caminho) ------------------------------------------------

        public const int CustoMarco = 8;

        /// <summary>Atributo do próprio caminho que o marco soma.</summary>
        public const int ValorMarcoProprio = 4;

        /// <summary>Atributo do caminho seguinte (a sinergia) que o marco soma.</summary>
        public const int ValorMarcoVizinho = 3;

        // --- Híbridos (1 entre cada par de caminhos vizinhos) ------------------------------

        public const int RankMaximoHibrido = 4;

        /// <summary>Custo do 1º ao 4º rank de um híbrido (10 pontos no total).</summary>
        public static readonly int[] CustoHibrido = { 2, 2, 3, 3 };

        /// <summary>Atributo que cada rank do híbrido soma a cada um dos dois caminhos.</summary>
        public const int ValorPorRankHibrido = 1;

        /// <summary>Rank do 1º nó principal dos dois caminhos que abre o híbrido.</summary>
        public const int RankParaAbrirHibrido = 3;

        // --- Efeitos de secundário ---------------------------------------------------------

        /// <summary>
        /// Efeito de secundário por rank. <c>Chave</c> é o nome do <c>AtributoSecundario</c> de
        /// Combate; <c>Multiplica</c> faz o valor ser um bônus percentual (0,15 é +15%).
        /// </summary>
        private readonly struct Secundario
        {
            public readonly string Chave;
            public readonly double ValorPorRank;
            public readonly bool Multiplica;

            public Secundario(string chave, double valorPorRank, bool multiplica = false)
            {
                Chave = chave;
                ValorPorRank = valorPorRank;
                Multiplica = multiplica;
            }
        }

        // Índice: o caminho (Força, Vigor, Agilidade, Intelecto, Espírito).
        private static readonly Secundario[] PrimeiroSecundario =
        {
            new Secundario("PenetracaoFisica", 0.02),
            new Secundario("RegeneracaoVida", 0.5),
            new Secundario("RegeneracaoStamina", 1.0),
            new Secundario("PenetracaoMagica", 0.02),
            new Secundario("RegeneracaoMana", 0.7),
        };

        private static readonly Secundario[] SegundoSecundario =
        {
            new Secundario("DanoCritico", 0.04),
            new Secundario("RegeneracaoStamina", 0.7),
            new Secundario("VelocidadeAtaqueFisico", 0.03),
            new Secundario("VelocidadeAtaqueMagico", 0.03),
            new Secundario("ChanceCritica", 0.02),
        };

        private static readonly Secundario[][] EfeitosDoGrande =
        {
            new[] { new Secundario("PenetracaoFisica", 0.03), new Secundario("DanoCritico", 0.06) },
            new[] { new Secundario("RegeneracaoVida", 1.0) },
            new[] { new Secundario("VelocidadeAtaqueFisico", 0.04) },
            new[]
            {
                new Secundario("PenetracaoMagica", 0.03),
                new Secundario("VelocidadeAtaqueMagico", 0.02),
            },
            new[] { new Secundario("RegeneracaoMana", 0.15, multiplica: true) },
        };

        // --- Tela (só a interface usa; não decide regra) -----------------------------------

        /// <summary>Raio (unidades da tela) do 1º, 2º e 3º principal, do grande e do marco.</summary>
        private static readonly float[] RaioPrincipal = { 1.5f, 2.6f, 3.7f };
        private const float RaioGrande = 4.8f;
        private const float RaioMarco = 5.9f;
        private const float RaioHibrido = 3.0f;
        private const float RaioPrimeiroSecundario = 2.1f;
        private const float RaioSegundoSecundario = 3.2f;
        private const double GrausDoSecundario = 18.0;

        private static readonly string[][] NomesPrincipais =
        {
            new[] { "Braço firme", "Golpe pesado", "Mão de ferro" },
            new[] { "Fôlego", "Couro grosso", "Coração forte" },
            new[] { "Passo leve", "Reflexo", "Pés de vento" },
            new[] { "Mente aguçada", "Estudo", "Visão arcana" },
            new[] { "Calma", "Fonte interior", "Alma serena" },
        };

        private static readonly string[][] NomesSecundarios =
        {
            new[] { "Ponta afiada", "Golpe mortal" },
            new[] { "Sangue quente", "Corpo treinado" },
            new[] { "Pulmões de corredor", "Mãos rápidas" },
            new[] { "Foco perfurante", "Conjuração ágil" },
            new[] { "Meditação", "Sorte arcana" },
        };

        private static readonly string[] NomesGrandes =
        {
            "Carrasco",
            "Muralha",
            "Lâmina veloz",
            "Arcanista",
            "Canal de mana",
        };

        private static readonly string[] NomesMarcos =
        {
            "Senhor da guerra",
            "Colosso",
            "Sombra",
            "Sábio",
            "Alma elevada",
        };

        private static readonly string[] NomesHibridos =
        {
            "Resistência bruta",
            "Corpo ágil",
            "Precisão",
            "Foco sereno",
            "Fúria contida",
        };

        public static ConfiguracaoDaArvore Criar()
        {
            var nos = new List<NoDaArvore>();
            for (var caminho = 0; caminho < 5; caminho++)
            {
                var atributo = (AtributoPrincipal)caminho;
                for (var posicao = 0; posicao < 3; posicao++)
                {
                    nos.Add(CriarPrincipal(atributo, posicao));
                }

                nos.Add(CriarSecundario(atributo, 0));
                nos.Add(CriarSecundario(atributo, 1));
                nos.Add(CriarGrande(atributo));
                nos.Add(CriarMarco(atributo));
            }

            for (var caminho = 0; caminho < 5; caminho++)
            {
                nos.Add(CriarHibrido(caminho, (caminho + 1) % 5));
            }

            return new ConfiguracaoDaArvore { Versao = Versao, Nos = nos.ToArray() };
        }

        public static string IdPrincipal(AtributoPrincipal caminho, int posicao) =>
            Prefixo(caminho) + "-" + (posicao + 1);

        public static string IdSecundario(AtributoPrincipal caminho, int posicao) =>
            Prefixo(caminho) + "-s" + (posicao + 1);

        public static string IdGrande(AtributoPrincipal caminho) => Prefixo(caminho) + "-grande";

        public static string IdMarco(AtributoPrincipal caminho) => Prefixo(caminho) + "-marco";

        public static string IdHibrido(AtributoPrincipal a, AtributoPrincipal b) =>
            "hibrido-" + Prefixo(a) + "-" + Prefixo(b);

        /// <summary>
        /// Pontos para comprar a árvore inteira: o que separa "tudo" dos 99 pontos do nível 100.
        /// </summary>
        public static int CustoTotal(ConfiguracaoDaArvore configuracao)
        {
            var total = 0;
            foreach (var no in configuracao.Nos)
            {
                total += RegrasDaArvore.CustoPago(no, no.RankMaximo);
            }

            return total;
        }

        private static string Prefixo(AtributoPrincipal caminho) =>
            caminho.ToString().ToLowerInvariant();

        private static float Angulo(double caminho) =>
            (float)((-90.0 + caminho * 72.0) * Math.PI / 180.0);

        private static float X(double graus, float raio) =>
            (float)Math.Cos(Angulo(0) + graus * Math.PI / 180.0) * raio;

        private static float Y(double graus, float raio) =>
            (float)Math.Sin(Angulo(0) + graus * Math.PI / 180.0) * raio;

        private static EfeitoDeNo EfeitoPrincipal(AtributoPrincipal atributo, int valor) =>
            new EfeitoDeNo
            {
                Tipo = TipoDeEfeito.AtributoPrincipal,
                Atributo = atributo,
                ValorPorRank = valor,
            };

        private static EfeitoDeNo EfeitoSecundario(Secundario secundario) =>
            new EfeitoDeNo
            {
                Tipo = TipoDeEfeito.AtributoSecundario,
                Chave = secundario.Chave,
                ValorPorRank = secundario.ValorPorRank,
                Multiplica = secundario.Multiplica,
            };

        private static RequisitoDeNo Requisito(string noId, int rankMinimo) =>
            new RequisitoDeNo { NoId = noId, RankMinimo = rankMinimo };

        private static NoDaArvore CriarPrincipal(AtributoPrincipal atributo, int posicao)
        {
            var graus = (int)atributo * 72.0;
            var requisitos = new List<RequisitoDeNo>();
            if (posicao > 0)
            {
                requisitos.Add(Requisito(IdPrincipal(atributo, posicao - 1), RankParaAbrirProximo));
            }

            return new NoDaArvore
            {
                Id = IdPrincipal(atributo, posicao),
                Nome = NomesPrincipais[(int)atributo][posicao],
                Descricao = "Aumenta " + atributo + ".",
                Tipo = TipoDeNo.Pequeno,
                Caminhos = new[] { atributo },
                RankMaximo = RankMaximoPrincipal,
                CustoPorRank = (int[])CustoPrincipal.Clone(),
                Requisitos = requisitos.ToArray(),
                Efeitos = new[] { EfeitoPrincipal(atributo, ValorPorRankPrincipal) },
                X = X(graus, RaioPrincipal[posicao]),
                Y = Y(graus, RaioPrincipal[posicao]),
            };
        }

        // O secundário fica ao lado do eixo do caminho: o 1º (depois do 1º principal) de um
        // lado, o 2º (depois do 2º principal) do outro.
        private static NoDaArvore CriarSecundario(AtributoPrincipal atributo, int posicao)
        {
            var secundario =
                posicao == 0 ? PrimeiroSecundario[(int)atributo] : SegundoSecundario[(int)atributo];
            var graus =
                (int)atributo * 72.0 + (posicao == 0 ? GrausDoSecundario : -GrausDoSecundario);
            var raio = posicao == 0 ? RaioPrimeiroSecundario : RaioSegundoSecundario;
            return new NoDaArvore
            {
                Id = IdSecundario(atributo, posicao),
                Nome = NomesSecundarios[(int)atributo][posicao],
                Descricao = "Reforça " + atributo + " com " + secundario.Chave + ".",
                Tipo = TipoDeNo.Pequeno,
                Caminhos = new[] { atributo },
                RankMaximo = RankMaximoSecundario,
                CustoPorRank = (int[])CustoSecundario.Clone(),
                Requisitos = new[]
                {
                    Requisito(IdPrincipal(atributo, posicao), RankParaAbrirSecundario),
                },
                Efeitos = new[] { EfeitoSecundario(secundario) },
                X = X(graus, raio),
                Y = Y(graus, raio),
            };
        }

        // Especialização. O bônus por categoria de skill (talentos.md, Efeitos condicionais)
        // depende das propriedades das skills; até lá o nó grande soma o atributo do caminho e
        // os secundários do tipo de dano que ele especializa, sem condição.
        private static NoDaArvore CriarGrande(AtributoPrincipal atributo)
        {
            var efeitos = new List<EfeitoDeNo> { EfeitoPrincipal(atributo, ValorPorRankGrande) };
            foreach (var secundario in EfeitosDoGrande[(int)atributo])
            {
                efeitos.Add(EfeitoSecundario(secundario));
            }

            var graus = (int)atributo * 72.0;
            return new NoDaArvore
            {
                Id = IdGrande(atributo),
                Nome = NomesGrandes[(int)atributo],
                Descricao = "Especialização de " + atributo + ".",
                Tipo = TipoDeNo.Grande,
                Caminhos = new[] { atributo },
                RankMaximo = RankMaximoGrande,
                CustoPorRank = (int[])CustoGrande.Clone(),
                Requisitos = new[] { Requisito(IdPrincipal(atributo, 2), RankParaAbrirGrande) },
                Efeitos = efeitos.ToArray(),
                X = X(graus, RaioGrande),
                Y = Y(graus, RaioGrande),
            };
        }

        // Marco: fim do caminho, com sinergia no caminho seguinte.
        private static NoDaArvore CriarMarco(AtributoPrincipal atributo)
        {
            var vizinho = (AtributoPrincipal)(((int)atributo + 1) % 5);
            var graus = (int)atributo * 72.0;
            return new NoDaArvore
            {
                Id = IdMarco(atributo),
                Nome = NomesMarcos[(int)atributo],
                Descricao = "Marco de " + atributo + ", com sinergia em " + vizinho + ".",
                Tipo = TipoDeNo.Avancado,
                Caminhos = new[] { atributo },
                RankMaximo = 1,
                CustoPorRank = new[] { CustoMarco },
                Requisitos = new[]
                {
                    Requisito(IdGrande(atributo), RankMaximoGrande),
                    Requisito(IdPrincipal(atributo, 2), RankMaximoPrincipal),
                },
                Efeitos = new[]
                {
                    EfeitoPrincipal(atributo, ValorMarcoProprio),
                    EfeitoPrincipal(vizinho, ValorMarcoVizinho),
                },
                X = X(graus, RaioMarco),
                Y = Y(graus, RaioMarco),
            };
        }

        // O híbrido fica no meio dos dois caminhos vizinhos e pede o 1º nó principal de cada um.
        private static NoDaArvore CriarHibrido(int primeiro, int segundo)
        {
            var a = (AtributoPrincipal)primeiro;
            var b = (AtributoPrincipal)segundo;
            var graus = primeiro * 72.0 + 36.0;
            return new NoDaArvore
            {
                Id = IdHibrido(a, b),
                Nome = NomesHibridos[primeiro],
                Descricao = "Liga " + a + " e " + b + ".",
                Tipo = TipoDeNo.Hibrido,
                Caminhos = new[] { a, b },
                RankMaximo = RankMaximoHibrido,
                CustoPorRank = (int[])CustoHibrido.Clone(),
                Requisitos = new[]
                {
                    Requisito(IdPrincipal(a, 0), RankParaAbrirHibrido),
                    Requisito(IdPrincipal(b, 0), RankParaAbrirHibrido),
                },
                Efeitos = new[]
                {
                    EfeitoPrincipal(a, ValorPorRankHibrido),
                    EfeitoPrincipal(b, ValorPorRankHibrido),
                },
                X = X(graus, RaioHibrido),
                Y = Y(graus, RaioHibrido),
            };
        }
    }
}
