using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>
    /// Árvore mínima de teste: três nós pequenos por caminho e um híbrido entre cada par de
    /// caminhos vizinhos. Todos os números são <b>valores de teste</b> (os finais são da
    /// issue #627), em constantes nomeadas para trocar sem mexer na regra.
    /// </summary>
    public static class ArvoreDeTeste
    {
        public const int Versao = 1;

        /// <summary>Ranks de cada nó pequeno.</summary>
        public const int RankMaximoPequeno = 3;

        /// <summary>Custo do 1º, 2º e 3º rank de um nó pequeno.</summary>
        public static readonly int[] CustoPequeno = { 1, 1, 2 };

        /// <summary>Atributo que cada rank de um nó pequeno soma.</summary>
        public const int ValorPorRankPequeno = 1;

        /// <summary>Rank que o nó anterior do caminho precisa ter para abrir o próximo.</summary>
        public const int RankParaAbrirProximo = 2;

        public const int RankMaximoHibrido = 2;
        public static readonly int[] CustoHibrido = { 2, 3 };
        public const int ValorPorRankHibrido = 1;

        /// <summary>Raio (em unidades da tela) do 1º, 2º e 3º nó do caminho e do híbrido.</summary>
        private static readonly float[] RaioPequeno = { 1.2f, 2.2f, 3.2f };
        private const float RaioHibrido = 2.7f;

        private static readonly string[][] NomesPequenos =
        {
            new[] { "Braço firme", "Golpe pesado", "Mão de ferro" },
            new[] { "Fôlego", "Couro grosso", "Coração forte" },
            new[] { "Passo leve", "Reflexo", "Pés de vento" },
            new[] { "Mente aguçada", "Estudo", "Visão arcana" },
            new[] { "Calma", "Fonte interior", "Alma serena" },
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
                    nos.Add(CriarPequeno(atributo, posicao));
                }
            }

            for (var caminho = 0; caminho < 5; caminho++)
            {
                nos.Add(CriarHibrido(caminho, (caminho + 1) % 5));
            }

            return new ConfiguracaoDaArvore { Versao = Versao, Nos = nos.ToArray() };
        }

        public static string IdPequeno(AtributoPrincipal caminho, int posicao) =>
            caminho.ToString().ToLowerInvariant() + "-" + (posicao + 1);

        public static string IdHibrido(AtributoPrincipal a, AtributoPrincipal b) =>
            "hibrido-" + a.ToString().ToLowerInvariant() + "-" + b.ToString().ToLowerInvariant();

        private static float Angulo(double caminho) =>
            (float)((-90.0 + caminho * 72.0) * Math.PI / 180.0);

        private static NoDaArvore CriarPequeno(AtributoPrincipal atributo, int posicao)
        {
            var angulo = Angulo((int)atributo);
            var requisitos = new List<RequisitoDeNo>();
            if (posicao > 0)
            {
                requisitos.Add(
                    new RequisitoDeNo
                    {
                        NoId = IdPequeno(atributo, posicao - 1),
                        RankMinimo = RankParaAbrirProximo,
                    }
                );
            }

            return new NoDaArvore
            {
                Id = IdPequeno(atributo, posicao),
                Nome = NomesPequenos[(int)atributo][posicao],
                Descricao = "Aumenta " + atributo + ".",
                Tipo = TipoDeNo.Pequeno,
                Caminhos = new[] { atributo },
                RankMaximo = RankMaximoPequeno,
                CustoPorRank = (int[])CustoPequeno.Clone(),
                Requisitos = requisitos.ToArray(),
                Efeitos = new[]
                {
                    new EfeitoDeNo
                    {
                        Tipo = TipoDeEfeito.AtributoPrincipal,
                        Atributo = atributo,
                        ValorPorRank = ValorPorRankPequeno,
                    },
                },
                X = (float)Math.Cos(angulo) * RaioPequeno[posicao],
                Y = (float)Math.Sin(angulo) * RaioPequeno[posicao],
            };
        }

        // O híbrido fica no meio dos dois caminhos vizinhos e pede o 2º nó de cada um.
        private static NoDaArvore CriarHibrido(int primeiro, int segundo)
        {
            var a = (AtributoPrincipal)primeiro;
            var b = (AtributoPrincipal)segundo;
            var angulo = Angulo(primeiro + 0.5);
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
                    new RequisitoDeNo { NoId = IdPequeno(a, 1), RankMinimo = 1 },
                    new RequisitoDeNo { NoId = IdPequeno(b, 1), RankMinimo = 1 },
                },
                Efeitos = new[]
                {
                    new EfeitoDeNo
                    {
                        Tipo = TipoDeEfeito.AtributoPrincipal,
                        Atributo = a,
                        ValorPorRank = ValorPorRankHibrido,
                    },
                    new EfeitoDeNo
                    {
                        Tipo = TipoDeEfeito.AtributoPrincipal,
                        Atributo = b,
                        ValorPorRank = ValorPorRankHibrido,
                    },
                },
                X = (float)Math.Cos(angulo) * RaioHibrido,
                Y = (float)Math.Sin(angulo) * RaioHibrido,
            };
        }
    }
}
