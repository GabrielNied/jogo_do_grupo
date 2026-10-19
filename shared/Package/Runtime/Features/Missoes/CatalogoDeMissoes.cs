using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Missoes
{
    /// <summary>
    /// Catálogo autoritativo de missões em dados (#179, #1061).
    /// </summary>
    public static class CatalogoDeMissoes
    {
        public const int Missao1Chegada = 1;
        public const int Missao2PrimeiraCacada = 2;
        public const int Missao3AbastecerForja = 3;
        public const int Missao4PatrulhaDiaria = 4;

        private static readonly Dictionary<int, DefinicaoDeMissao> _porId =
            new Dictionary<int, DefinicaoDeMissao>();

        static CatalogoDeMissoes()
        {
            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao1Chegada,
                    Titulo = "Chegada à Brasa Mansa",
                    Descricao =
                        "Você acaba de chegar aos portões da cidade. Apresente-se ao Mercador Alden no mercado central.",
                    Tipo = TipoDeMissao.Principal,
                    Regiao = "cidade",
                    NivelRecomendado = 1,
                    NivelMinimo = 1,
                    NpcDoadorId = 1, // Alden (recebe direto ao nascer)
                    NpcEntregadorId = 1,
                    RequisitosMissaoIds = System.Array.Empty<int>(),
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Fale com o Mercador Alden na praça",
                            Tipo = TipoDeObjetivo.Falar,
                            AlvoId = 1,
                            QuantidadeTotal = 1,
                            RegiaoAlvo = "cidade",
                            CoordenadaX = 12f,
                            CoordenadaZ = -8f,
                            RaioAreaMetros = 4f,
                        },
                    },
                    RecompensaXp = 100,
                    RecompensaCoroas = 25,
                    ItensParaEscolha = System.Array.Empty<int>(),
                    ProximaMissaoPrincipalId = Missao2PrimeiraCacada,
                }
            );

            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao2PrimeiraCacada,
                    Titulo = "Primeira Caçada nos Arredores",
                    Descricao =
                        "Os campos ao norte estão infestados de javalis vorazes. Elimine 4 deles para proteger as caravanas.",
                    Tipo = TipoDeMissao.Principal,
                    Regiao = "campo",
                    NivelRecomendado = 2,
                    NivelMinimo = 1,
                    NpcDoadorId = 1,
                    NpcEntregadorId = 1,
                    RequisitosMissaoIds = new int[] { Missao1Chegada },
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Derrote 4 Javalis do Campo",
                            Tipo = TipoDeObjetivo.Matar,
                            AlvoId = 1, // JavaliDoCampo
                            QuantidadeTotal = 4,
                            RegiaoAlvo = "campo",
                            CoordenadaX = 0f,
                            CoordenadaZ = 80f,
                            RaioAreaMetros = 50f,
                        },
                    },
                    RecompensaXp = 250,
                    RecompensaCoroas = 50,
                    ItensParaEscolha = new int[] { 1, 2 }, // Exemplo de itens de escolha
                    ProximaMissaoPrincipalId = 0,
                }
            );

            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao3AbastecerForja,
                    Titulo = "Abastecendo a Forja",
                    Descricao =
                        "O Ferreiro Brom precisa de recursos para forjar armaduras para os novos aventureiros.",
                    Tipo = TipoDeMissao.Secundaria,
                    Regiao = "campo",
                    NivelRecomendado = 2,
                    NivelMinimo = 1,
                    NpcDoadorId = 2, // Ferreiro
                    NpcEntregadorId = 2,
                    RequisitosMissaoIds = new int[] { Missao1Chegada },
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Colete 3 Minérios ou Recursos",
                            Tipo = TipoDeObjetivo.Coletar,
                            AlvoId = 1,
                            QuantidadeTotal = 3,
                            RegiaoAlvo = "campo",
                            CoordenadaX = 20f,
                            CoordenadaZ = 60f,
                            RaioAreaMetros = 40f,
                        },
                    },
                    RecompensaXp = 180,
                    RecompensaCoroas = 40,
                    ItensParaEscolha = System.Array.Empty<int>(),
                    ProximaMissaoPrincipalId = 0,
                }
            );

            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao4PatrulhaDiaria,
                    Titulo = "Patrulha Diária dos Campos",
                    Descricao =
                        "Mantenha a segurança das estradas derrotando criaturas perigosas hoje.",
                    Tipo = TipoDeMissao.Diaria,
                    Regiao = "campo",
                    NivelRecomendado = 3,
                    NivelMinimo = 2,
                    NpcDoadorId = 1,
                    NpcEntregadorId = 1,
                    RequisitosMissaoIds = new int[] { Missao2PrimeiraCacada },
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Derrote 5 Criaturas do Campo",
                            Tipo = TipoDeObjetivo.Matar,
                            AlvoId = 1,
                            QuantidadeTotal = 5,
                            RegiaoAlvo = "campo",
                            CoordenadaX = 0f,
                            CoordenadaZ = 80f,
                            RaioAreaMetros = 60f,
                        },
                    },
                    RecompensaXp = 200,
                    RecompensaCoroas = 30,
                    ReputacaoFaccao = "BrasaMansa",
                    ReputacaoValor = 50,
                    ItensParaEscolha = System.Array.Empty<int>(),
                    ProximaMissaoPrincipalId = 0,
                }
            );
        }

        private static void Registrar(DefinicaoDeMissao definicao)
        {
            _porId[definicao.Id] = definicao;
        }

        public static DefinicaoDeMissao? Obter(int id)
        {
            return _porId.TryGetValue(id, out var def) ? def : null;
        }

        public static IEnumerable<DefinicaoDeMissao> Todas => _porId.Values;
    }
}
