using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Npcs;

namespace JogoDoGrupo.Shared.Features.Missoes
{
    /// <summary>
    /// Catálogo autoritativo de missões em dados (#179, #328, #1058, #1060, #1061).
    /// </summary>
    public static class CatalogoDeMissoes
    {
        // =========================================================================
        // CAPÍTULO 1: A CIDADE (História Principal / Tutorial da Primeira Hora #328)
        // =========================================================================
        public const int Missao1PortoesDaBrasaMansa = 1;
        public const int Missao1Chegada = Missao1PortoesDaBrasaMansa; // compatibilidade

        public const int Missao2PrimeiraFerramenta = 2;
        public const int Missao2PrimeiraCacada = Missao2PrimeiraFerramenta; // compatibilidade

        public const int Missao3CascaDeFerroECouro = 3;
        public const int Missao4ProvisoesEGuardaVolumes = 4;
        public const int Missao5LimpandoATrilhaNorte = 5;
        public const int Missao6OEnxameNaMataFria = 6;
        public const int Missao7OSenhorDaColinaDoVigia = 7;

        // =========================================================================
        // MISSÕES SECUNDÁRIAS (Desbloqueadas com o fim do Capítulo 1 #1058, #1060)
        // =========================================================================
        public const int Missao101CaldeiraoDaLareira = 101;
        public const int Missao102MinerioParaABigorna = 102;
        public const int Missao3AbastecerForja = Missao102MinerioParaABigorna; // compatibilidade

        public const int Missao103OMascoteFugitivo = 103;
        public const int Missao104CorrespondenciaExtraviada = 104;
        public const int Missao105VigilanciaNoPortaoLeste = 105;
        public const int Missao106RemedioDasSombras = 106;

        // =========================================================================
        // MISSÕES DIÁRIAS (#1061)
        // =========================================================================
        public const int Missao201PatrulhaDiaria = 201;
        public const int Missao4PatrulhaDiaria = Missao201PatrulhaDiaria; // compatibilidade

        private static readonly Dictionary<int, DefinicaoDeMissao> _porId =
            new Dictionary<int, DefinicaoDeMissao>();

        static CatalogoDeMissoes()
        {
            // ---------------------------------------------------------------------
            // ETAPA 1: Portões da Brasa Mansa (#1058)
            // ---------------------------------------------------------------------
            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao1PortoesDaBrasaMansa,
                    Titulo = "Portões da Brasa Mansa",
                    Descricao =
                        "Você conseguiu atravessar a estrada da serra, mas está em farrapos. Entre na cidadela e procure Alden no mercado da praça; ele fornecerá suas primeiras informações.",
                    Tipo = TipoDeMissao.Principal,
                    Regiao = CatalogoNpcs.AreaCidade,
                    NivelRecomendado = 1,
                    NivelMinimo = 1,
                    NpcDoadorId = CatalogoNpcs.IdValeria,
                    NpcEntregadorId = CatalogoNpcs.IdVendedor, // Mercador Alden
                    RequisitosMissaoIds = System.Array.Empty<int>(),
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Fale com o Mercador Alden na praça central",
                            Tipo = TipoDeObjetivo.Falar,
                            AlvoId = CatalogoNpcs.IdVendedor,
                            QuantidadeTotal = 1,
                            RegiaoAlvo = CatalogoNpcs.AreaCidade,
                            CoordenadaX = 12f,
                            CoordenadaZ = -8f,
                            RaioAreaMetros = 4f,
                        },
                    },
                    RecompensaXp = 100,
                    RecompensaCoroas = 25,
                    ItensParaEscolha = System.Array.Empty<int>(),
                    ProximaMissaoPrincipalId = Missao2PrimeiraFerramenta,
                }
            );

            // ---------------------------------------------------------------------
            // ETAPA 2: A Primeira Ferramenta (#1058)
            // ---------------------------------------------------------------------
            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao2PrimeiraFerramenta,
                    Titulo = "A Primeira Ferramenta",
                    Descricao =
                        "Você não durará cinco minutos além das paliçadas desarmado. Vá até Gareth no beco das forjas e escolha a arma que melhor se adapta às suas mãos.",
                    Tipo = TipoDeMissao.Principal,
                    Regiao = CatalogoNpcs.AreaCidade,
                    NivelRecomendado = 1,
                    NivelMinimo = 1,
                    NpcDoadorId = CatalogoNpcs.IdVendedor,
                    NpcEntregadorId = CatalogoNpcs.IdArmeiro, // Armeiro Gareth
                    RequisitosMissaoIds = new int[] { Missao1PortoesDaBrasaMansa },
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Fale com o Armeiro Gareth no beco das forjas",
                            Tipo = TipoDeObjetivo.Falar,
                            AlvoId = CatalogoNpcs.IdArmeiro,
                            QuantidadeTotal = 1,
                            RegiaoAlvo = CatalogoNpcs.AreaCidade,
                            CoordenadaX = 10f,
                            CoordenadaZ = -4f,
                            RaioAreaMetros = 4f,
                        },
                    },
                    RecompensaXp = 120,
                    RecompensaCoroas = 30,
                    ItensParaEscolha = new int[]
                    {
                        CatalogoEquipamentos.PrimeiroModeloDeArma, // Espada (173)
                        CatalogoEquipamentos.PrimeiroModeloDeArma + 1, // Arco (174)
                        CatalogoEquipamentos.PrimeiroModeloDeArma + 2, // Cajado (175)
                    },
                    ProximaMissaoPrincipalId = Missao3CascaDeFerroECouro,
                }
            );

            // ---------------------------------------------------------------------
            // ETAPA 3: Casca de Ferro e Couro (#1058)
            // ---------------------------------------------------------------------
            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao3CascaDeFerroECouro,
                    Titulo = "Casca de Ferro e Couro",
                    Descricao =
                        "Uma boa arma exige um corpo protegido. Leve este lingote inicial até a forja de Brom e peça a ele para reforçar sua veste.",
                    Tipo = TipoDeMissao.Principal,
                    Regiao = CatalogoNpcs.AreaCidade,
                    NivelRecomendado = 1,
                    NivelMinimo = 1,
                    NpcDoadorId = CatalogoNpcs.IdArmeiro,
                    NpcEntregadorId = CatalogoNpcs.IdFerreiro, // Ferreiro Brom
                    RequisitosMissaoIds = new int[] { Missao2PrimeiraFerramenta },
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Fale com o Ferreiro Brom na forja",
                            Tipo = TipoDeObjetivo.Falar,
                            AlvoId = CatalogoNpcs.IdFerreiro,
                            QuantidadeTotal = 1,
                            RegiaoAlvo = CatalogoNpcs.AreaCidade,
                            CoordenadaX = -12f,
                            CoordenadaZ = -6f,
                            RaioAreaMetros = 4f,
                        },
                    },
                    RecompensaXp = 140,
                    RecompensaCoroas = 35,
                    ItensParaEscolha = new int[]
                    {
                        CatalogoEquipamentos.PrimeiroModelo, // Peitoral de Couro/Vigia (101)
                    },
                    ProximaMissaoPrincipalId = Missao4ProvisoesEGuardaVolumes,
                }
            );

            // ---------------------------------------------------------------------
            // ETAPA 4: Provisões e Guarda-Volumes (#1058)
            // ---------------------------------------------------------------------
            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao4ProvisoesEGuardaVolumes,
                    Titulo = "Provisões e Guarda-Volumes",
                    Descricao =
                        "Antes de marchar para o descampado, passe na taverna de Barnabé para encher o cantil e depois deixe suas sobras no banco com Cassian.",
                    Tipo = TipoDeMissao.Principal,
                    Regiao = CatalogoNpcs.AreaCidade,
                    NivelRecomendado = 1,
                    NivelMinimo = 1,
                    NpcDoadorId = CatalogoNpcs.IdFerreiro,
                    NpcEntregadorId = CatalogoNpcs.IdBanqueiro, // Banqueiro Cassian
                    RequisitosMissaoIds = new int[] { Missao3CascaDeFerroECouro },
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Fale com o Taverneiro Barnabé na taverna",
                            Tipo = TipoDeObjetivo.Falar,
                            AlvoId = CatalogoNpcs.IdTaverneiro,
                            QuantidadeTotal = 1,
                            RegiaoAlvo = CatalogoNpcs.AreaCidade,
                            CoordenadaX = -6f,
                            CoordenadaZ = -12f,
                            RaioAreaMetros = 4f,
                        },
                        new ObjetivoMissao
                        {
                            Id = 2,
                            Descricao = "Fale com o Banqueiro Cassian no cofre central",
                            Tipo = TipoDeObjetivo.Falar,
                            AlvoId = CatalogoNpcs.IdBanqueiro,
                            QuantidadeTotal = 1,
                            RegiaoAlvo = CatalogoNpcs.AreaCidade,
                            CoordenadaX = 0f,
                            CoordenadaZ = -14f,
                            RaioAreaMetros = 4f,
                        },
                    },
                    RecompensaXp = 160,
                    RecompensaCoroas = 40,
                    ItensParaEscolha = System.Array.Empty<int>(),
                    ProximaMissaoPrincipalId = Missao5LimpandoATrilhaNorte,
                }
            );

            // ---------------------------------------------------------------------
            // ETAPA 5: Limpando a Trilha Norte (#1058)
            // ---------------------------------------------------------------------
            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao5LimpandoATrilhaNorte,
                    Titulo = "Limpando a Trilha Norte",
                    Descricao =
                        "Chega de passeios pela praça. Uma vara de javalis selvagens está revirando os fardos dos comboios a norte da muralha. Vá lá e elimine a ameaça.",
                    Tipo = TipoDeMissao.Principal,
                    Regiao = "campo",
                    NivelRecomendado = 2,
                    NivelMinimo = 1,
                    NpcDoadorId = CatalogoNpcs.IdValeria,
                    NpcEntregadorId = CatalogoNpcs.IdValeria,
                    RequisitosMissaoIds = new int[] { Missao4ProvisoesEGuardaVolumes },
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Derrote 6 Javalis do Campo na trilha norte",
                            Tipo = TipoDeObjetivo.Matar,
                            AlvoId = 1, // Javali do Campo
                            QuantidadeTotal = 6,
                            RegiaoAlvo = "campo",
                            CoordenadaX = 0f,
                            CoordenadaZ = 80f,
                            RaioAreaMetros = 50f,
                        },
                    },
                    RecompensaXp = 280,
                    RecompensaCoroas = 65,
                    ItensParaEscolha = System.Array.Empty<int>(),
                    ProximaMissaoPrincipalId = Missao6OEnxameNaMataFria,
                }
            );

            // ---------------------------------------------------------------------
            // ETAPA 6: O Enxame na Mata Fria (#1058)
            // ---------------------------------------------------------------------
            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao6OEnxameNaMataFria,
                    Titulo = "O Enxame na Mata Fria",
                    Descricao =
                        "Os javalis eram apenas o começo. Os lobos cinzentos desceram das ravinas e estão encurralando nossos lenhadores. Derrube as feras e garanta a segurança dos comboios.",
                    Tipo = TipoDeMissao.Principal,
                    Regiao = "campo",
                    NivelRecomendado = 2,
                    NivelMinimo = 2,
                    NpcDoadorId = CatalogoNpcs.IdValeria,
                    NpcEntregadorId = CatalogoNpcs.IdValeria,
                    RequisitosMissaoIds = new int[] { Missao5LimpandoATrilhaNorte },
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Derrote 5 Lobos do Campo na mata fria",
                            Tipo = TipoDeObjetivo.Matar,
                            AlvoId = 2, // Lobo do Campo
                            QuantidadeTotal = 5,
                            RegiaoAlvo = "campo",
                            CoordenadaX = 30f,
                            CoordenadaZ = 100f,
                            RaioAreaMetros = 60f,
                        },
                    },
                    RecompensaXp = 360,
                    RecompensaCoroas = 85,
                    ItensParaEscolha = System.Array.Empty<int>(),
                    ProximaMissaoPrincipalId = Missao7OSenhorDaColinaDoVigia,
                }
            );

            // ---------------------------------------------------------------------
            // ETAPA 7: O Senhor da Colina do Vigia (Clímax do Capítulo 1 #1058, #1059)
            // ---------------------------------------------------------------------
            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao7OSenhorDaColinaDoVigia,
                    Titulo = "O Senhor da Colina do Vigia",
                    Descricao =
                        "Descobrimos o que está empurrando as feras contra nossos muros. O Presa Velha, um javali colossal com presas de obsidiana, estabeleceu território no topo da Colina do Vigia. Suba a colina e acabe com o monstro!",
                    Tipo = TipoDeMissao.Principal,
                    Regiao = "campo",
                    NivelRecomendado = 3,
                    NivelMinimo = 2,
                    NpcDoadorId = CatalogoNpcs.IdValeria,
                    NpcEntregadorId = CatalogoNpcs.IdValeria,
                    RequisitosMissaoIds = new int[] { Missao6OEnxameNaMataFria },
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Derrote o Presa Velha no topo da Colina do Vigia",
                            Tipo = TipoDeObjetivo.Matar,
                            AlvoId = 200, // Presa Velha (#1059)
                            QuantidadeTotal = 1,
                            RegiaoAlvo = "campo",
                            CoordenadaX = 50f,
                            CoordenadaZ = 140f,
                            RaioAreaMetros = 30f,
                        },
                    },
                    RecompensaXp = 750,
                    RecompensaCoroas = 200,
                    ItensParaEscolha = new int[]
                    {
                        CatalogoEquipamentos.AdornoPresaVelha, // 200
                        CatalogoEquipamentos.PrimeiroModeloDeArma, // 173 (Espada)
                        CatalogoEquipamentos.PrimeiroModelo, // 101 (Peitoral)
                    },
                    ProximaMissaoPrincipalId = 0, // Fim do capítulo 1: abre secundárias (#1060)
                }
            );

            // =====================================================================
            // SECUNDÁRIAS DA BRASA MANSA E ARREDORES (#1058, #1060)
            // Todas exigem a conclusão do Capítulo 1 (Missao7OSenhorDaColinaDoVigia)
            // =====================================================================
            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao101CaldeiraoDaLareira,
                    Titulo = "Caldeirão da Lareira",
                    Descricao =
                        "O Taverneiro Barnabé precisa de carne fresca dos campos norte para repor o ensopado comunitário da lareira.",
                    Tipo = TipoDeMissao.Secundaria,
                    Regiao = CatalogoNpcs.AreaCidade,
                    NivelRecomendado = 3,
                    NivelMinimo = 3,
                    NpcDoadorId = CatalogoNpcs.IdTaverneiro,
                    NpcEntregadorId = CatalogoNpcs.IdTaverneiro,
                    RequisitosMissaoIds = new int[] { Missao7OSenhorDaColinaDoVigia },
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Derrote 4 Javalis do Campo para coletar carne",
                            Tipo = TipoDeObjetivo.Matar,
                            AlvoId = 1,
                            QuantidadeTotal = 4,
                            RegiaoAlvo = "campo",
                            CoordenadaX = 0f,
                            CoordenadaZ = 80f,
                            RaioAreaMetros = 50f,
                        },
                    },
                    RecompensaXp = 200,
                    RecompensaCoroas = 45,
                    ItensParaEscolha = System.Array.Empty<int>(),
                    ProximaMissaoPrincipalId = 0,
                }
            );

            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao102MinerioParaABigorna,
                    Titulo = "Minério para a Bigorna",
                    Descricao =
                        "O Ferreiro Brom precisa de minérios e metais pesados para manter a forja produzindo armaduras para a guarda.",
                    Tipo = TipoDeMissao.Secundaria,
                    Regiao = CatalogoNpcs.AreaCidade,
                    NivelRecomendado = 3,
                    NivelMinimo = 3,
                    NpcDoadorId = CatalogoNpcs.IdFerreiro,
                    NpcEntregadorId = CatalogoNpcs.IdFerreiro,
                    RequisitosMissaoIds = new int[] { Missao7OSenhorDaColinaDoVigia },
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Colete 6 Minérios de Ferro nos afloramentos rochosos",
                            Tipo = TipoDeObjetivo.Coletar,
                            AlvoId = 1,
                            QuantidadeTotal = 6,
                            RegiaoAlvo = "campo",
                            CoordenadaX = 20f,
                            CoordenadaZ = 60f,
                            RaioAreaMetros = 40f,
                        },
                    },
                    RecompensaXp = 220,
                    RecompensaCoroas = 50,
                    ItensParaEscolha = System.Array.Empty<int>(),
                    ProximaMissaoPrincipalId = 0,
                }
            );

            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao103OMascoteFugitivo,
                    Titulo = "O Mascote Fugitivo",
                    Descricao =
                        "A Cuidadora Mirna relata que pequenos filhotes assustados fugiram para perto do moinho decrépito.",
                    Tipo = TipoDeMissao.Secundaria,
                    Regiao = CatalogoNpcs.AreaCidade,
                    NivelRecomendado = 3,
                    NivelMinimo = 3,
                    NpcDoadorId = CatalogoNpcs.IdCuidadorDePets,
                    NpcEntregadorId = CatalogoNpcs.IdCuidadorDePets,
                    RequisitosMissaoIds = new int[] { Missao7OSenhorDaColinaDoVigia },
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Fale com a Cuidadora Mirna após patrulhar o moinho",
                            Tipo = TipoDeObjetivo.Falar,
                            AlvoId = CatalogoNpcs.IdCuidadorDePets,
                            QuantidadeTotal = 1,
                            RegiaoAlvo = CatalogoNpcs.AreaCidade,
                            CoordenadaX = -14f,
                            CoordenadaZ = 14f,
                            RaioAreaMetros = 4f,
                        },
                    },
                    RecompensaXp = 180,
                    RecompensaCoroas = 35,
                    ItensParaEscolha = System.Array.Empty<int>(),
                    ProximaMissaoPrincipalId = 0,
                }
            );

            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao104CorrespondenciaExtraviada,
                    Titulo = "Correspondência Extraviada",
                    Descricao =
                        "O Banqueiro Cassian precisa recuperar a pasta de ordens mercantis que caiu na estrada após um ataque de salteadores.",
                    Tipo = TipoDeMissao.Secundaria,
                    Regiao = CatalogoNpcs.AreaCidade,
                    NivelRecomendado = 3,
                    NivelMinimo = 3,
                    NpcDoadorId = CatalogoNpcs.IdBanqueiro,
                    NpcEntregadorId = CatalogoNpcs.IdBanqueiro,
                    RequisitosMissaoIds = new int[] { Missao7OSenhorDaColinaDoVigia },
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Colete a Pasta de Ordens Mercantis no vale",
                            Tipo = TipoDeObjetivo.Coletar,
                            AlvoId = 4,
                            QuantidadeTotal = 1,
                            RegiaoAlvo = "campo",
                            CoordenadaX = 15f,
                            CoordenadaZ = 70f,
                            RaioAreaMetros = 30f,
                        },
                    },
                    RecompensaXp = 210,
                    RecompensaCoroas = 40,
                    ItensParaEscolha = System.Array.Empty<int>(),
                    ProximaMissaoPrincipalId = 0,
                }
            );

            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao105VigilanciaNoPortaoLeste,
                    Titulo = "Vigilância no Portão Leste",
                    Descricao =
                        "A Capitã Valéria ordenou o reforço da patrulha e o abate de lobos que rondam a paliçada oriental sob a névoa.",
                    Tipo = TipoDeMissao.Secundaria,
                    Regiao = "campo",
                    NivelRecomendado = 3,
                    NivelMinimo = 3,
                    NpcDoadorId = CatalogoNpcs.IdValeria,
                    NpcEntregadorId = CatalogoNpcs.IdValeria,
                    RequisitosMissaoIds = new int[] { Missao7OSenhorDaColinaDoVigia },
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Derrote 5 Lobos que rondam o portão leste",
                            Tipo = TipoDeObjetivo.Matar,
                            AlvoId = 2,
                            QuantidadeTotal = 5,
                            RegiaoAlvo = "campo",
                            CoordenadaX = 60f,
                            CoordenadaZ = 20f,
                            RaioAreaMetros = 40f,
                        },
                    },
                    RecompensaXp = 250,
                    RecompensaCoroas = 55,
                    ItensParaEscolha = System.Array.Empty<int>(),
                    ProximaMissaoPrincipalId = 0,
                }
            );

            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao106RemedioDasSombras,
                    Titulo = "Remédio das Sombras",
                    Descricao =
                        "O Merceeiro Tomas precisa de fungos bioluminescentes colhidos na mata fria para a infusão de tônicos medicinais.",
                    Tipo = TipoDeMissao.Secundaria,
                    Regiao = CatalogoNpcs.AreaCidade,
                    NivelRecomendado = 3,
                    NivelMinimo = 3,
                    NpcDoadorId = CatalogoNpcs.IdMerceeiro,
                    NpcEntregadorId = CatalogoNpcs.IdMerceeiro,
                    RequisitosMissaoIds = new int[] { Missao7OSenhorDaColinaDoVigia },
                    Objetivos = new ObjetivoMissao[]
                    {
                        new ObjetivoMissao
                        {
                            Id = 1,
                            Descricao = "Colete 5 Fungos Luminescentes nos troncos da mata",
                            Tipo = TipoDeObjetivo.Coletar,
                            AlvoId = 2,
                            QuantidadeTotal = 5,
                            RegiaoAlvo = "campo",
                            CoordenadaX = 35f,
                            CoordenadaZ = 110f,
                            RaioAreaMetros = 45f,
                        },
                    },
                    RecompensaXp = 230,
                    RecompensaCoroas = 50,
                    ItensParaEscolha = System.Array.Empty<int>(),
                    ProximaMissaoPrincipalId = 0,
                }
            );

            // =====================================================================
            // MISSÃO DIÁRIA (#1061)
            // =====================================================================
            Registrar(
                new DefinicaoDeMissao
                {
                    Id = Missao201PatrulhaDiaria,
                    Titulo = "Patrulha Diária dos Campos",
                    Descricao =
                        "Mantenha a segurança das estradas derrotando criaturas perigosas ao norte da Brasa Mansa hoje.",
                    Tipo = TipoDeMissao.Diaria,
                    Regiao = "campo",
                    NivelRecomendado = 3,
                    NivelMinimo = 2,
                    NpcDoadorId = CatalogoNpcs.IdValeria,
                    NpcEntregadorId = CatalogoNpcs.IdValeria,
                    RequisitosMissaoIds = new int[] { Missao5LimpandoATrilhaNorte },
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
