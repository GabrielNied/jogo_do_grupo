using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Entrada na tabela de distribuição de criaturas por bioma e horário (issue #916).
    /// </summary>
    public sealed class EntradaInimigoPorBioma
    {
        public EntradaInimigoPorBioma(
            Bioma bioma,
            int criaturaId,
            string nomeEspecie,
            int nivelMinimo,
            int nivelMaximo,
            PeriodoDeAparicao periodo,
            string notas = ""
        )
        {
            Bioma = bioma;
            CriaturaId = criaturaId;
            NomeEspecie = nomeEspecie;
            NivelMinimo = nivelMinimo;
            NivelMaximo = nivelMaximo;
            Periodo = periodo;
            Notas = notas;
        }

        public Bioma Bioma { get; }

        public int CriaturaId { get; }

        public string NomeEspecie { get; }

        public int NivelMinimo { get; }

        public int NivelMaximo { get; }

        public PeriodoDeAparicao Periodo { get; }

        public string Notas { get; }
    }

    /// <summary>
    /// Tabela bioma × espécie × nível, de dia e de noite (docs/features/mobs.md).
    /// As espécies noturnas aparecem sem escalar atributos.
    /// </summary>
    public static class ConfigInimigosPorBioma
    {
        public static readonly IReadOnlyList<EntradaInimigoPorBioma> Tabela =
            new EntradaInimigoPorBioma[]
            {
                // Campo (níveis 1 a 10)
                new EntradaInimigoPorBioma(
                    Bioma.Campo,
                    CatalogoCriaturas.JavaliDoCampo,
                    "Javali do Campo",
                    1,
                    5,
                    PeriodoDeAparicao.Dia,
                    "Manadas e grupos no pasto e campo aberto"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Campo,
                    CatalogoCriaturas.LoboDoCampo,
                    "Lobo do Campo",
                    2,
                    5,
                    PeriodoDeAparicao.Sempre,
                    "Caçadores rápidos perto das bordas"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Campo,
                    CatalogoCriaturas.TatuRochoso,
                    "Tatu Rochoso",
                    4,
                    6,
                    PeriodoDeAparicao.Sempre,
                    "Defensores lentos nas encostas"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Campo,
                    CatalogoCriaturas.LoboDoCampo,
                    "Lobo do Campo",
                    5,
                    8,
                    PeriodoDeAparicao.Noite,
                    "Matilhas que rondam sob o luar"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Campo,
                    CatalogoCriaturas.MariposaDeBreu,
                    "Mariposa de Breu",
                    8,
                    10,
                    PeriodoDeAparicao.Noite,
                    "Enxames noturnos atraídos por fogueiras e postes"
                ),
                // Mata (níveis 10 a 20)
                new EntradaInimigoPorBioma(
                    Bioma.Mata,
                    CatalogoCriaturas.CervoDeGalhadaOca,
                    "Cervo de Galhada Oca",
                    10,
                    14,
                    PeriodoDeAparicao.Dia,
                    "Manadas pacíficas e esquivas nas clareiras"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Mata,
                    CatalogoCriaturas.JavaliFerrugem,
                    "Javali Ferrugem",
                    12,
                    16,
                    PeriodoDeAparicao.Dia,
                    "Grupos agressivos na mata"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Mata,
                    CatalogoCriaturas.LoboDoCampo,
                    "Lobo do Campo",
                    10,
                    15,
                    PeriodoDeAparicao.Sempre,
                    "Predadores da floresta"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Mata,
                    CatalogoCriaturas.MariposaDeBreu,
                    "Mariposa de Breu",
                    10,
                    15,
                    PeriodoDeAparicao.Noite,
                    "Enxames noturnos bioluminescentes"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Mata,
                    CatalogoCriaturas.LoboDoCampo,
                    "Lobo do Campo",
                    14,
                    18,
                    PeriodoDeAparicao.Noite,
                    "Alcateia noturna da mata fechada"
                ),
                // Colinas (níveis 15 a 25)
                new EntradaInimigoPorBioma(
                    Bioma.Colinas,
                    CatalogoCriaturas.TatuRochoso,
                    "Tatu Rochoso",
                    12,
                    18,
                    PeriodoDeAparicao.Sempre,
                    "Tocas nas rochas"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Colinas,
                    CatalogoCriaturas.JavaliFerrugem,
                    "Javali Ferrugem",
                    15,
                    20,
                    PeriodoDeAparicao.Dia,
                    "Bandos nos vales"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Colinas,
                    CatalogoCriaturas.LoboDoCampo,
                    "Lobo do Campo",
                    16,
                    22,
                    PeriodoDeAparicao.Noite,
                    "Caçadores do cume ao anoitecer"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Colinas,
                    CatalogoCriaturas.MariposaDeBreu,
                    "Mariposa de Breu",
                    15,
                    20,
                    PeriodoDeAparicao.Noite,
                    "Enxames nas fendas sombrias"
                ),
                // Deserto (níveis 20 a 30)
                new EntradaInimigoPorBioma(
                    Bioma.Deserto,
                    CatalogoCriaturas.TatuRochoso,
                    "Tatu Rochoso",
                    18,
                    24,
                    PeriodoDeAparicao.Dia,
                    "Resistentes ao sol das dunas"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Deserto,
                    CatalogoCriaturas.JavaliFerrugem,
                    "Javali Ferrugem",
                    20,
                    25,
                    PeriodoDeAparicao.Dia,
                    "Grupos nos oásis e desfiladeiros"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Deserto,
                    CatalogoCriaturas.LoboDoCampo,
                    "Lobo do Campo",
                    22,
                    28,
                    PeriodoDeAparicao.Noite,
                    "Predadores do frio noturno das dunas"
                ),
                // Neve (níveis 25 a 35)
                new EntradaInimigoPorBioma(
                    Bioma.Neve,
                    CatalogoCriaturas.CervoDeGalhadaOca,
                    "Cervo de Galhada Oca",
                    22,
                    26,
                    PeriodoDeAparicao.Dia,
                    "Manadas de pelagem espessa na nevasca"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Neve,
                    CatalogoCriaturas.LoboDeGeada,
                    "Lobo de Geada",
                    25,
                    30,
                    PeriodoDeAparicao.Sempre,
                    "Feras glaciais adaptadas ao gelo"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Neve,
                    CatalogoCriaturas.LoboDeGeada,
                    "Lobo de Geada",
                    28,
                    35,
                    PeriodoDeAparicao.Noite,
                    "Matilhas ferozes sob as auroras boreais"
                ),
                // Lava (níveis 30 a 40)
                new EntradaInimigoPorBioma(
                    Bioma.Lava,
                    CatalogoCriaturas.CarapacaDeBrasa,
                    "Carapaça de Brasa",
                    30,
                    36,
                    PeriodoDeAparicao.Sempre,
                    "Casca mineral incandescente"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Lava,
                    CatalogoCriaturas.CarapacaDeBrasa,
                    "Carapaça de Brasa",
                    32,
                    40,
                    PeriodoDeAparicao.Noite,
                    "Placas ardentes resfriando sob o céu escuro"
                ),
                // Pântano (níveis 20 a 30)
                new EntradaInimigoPorBioma(
                    Bioma.Pantano,
                    CatalogoCriaturas.TatuRochoso,
                    "Tatu Rochoso",
                    20,
                    25,
                    PeriodoDeAparicao.Dia,
                    "Blindados atolados nas margens"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Pantano,
                    CatalogoCriaturas.JavaliDoCampo,
                    "Javali do Campo",
                    18,
                    24,
                    PeriodoDeAparicao.Dia,
                    "Bandos enlameados"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Pantano,
                    CatalogoCriaturas.MariposaDeBreu,
                    "Mariposa de Breu",
                    22,
                    28,
                    PeriodoDeAparicao.Noite,
                    "Enxames luminescentes sobre as águas paradas"
                ),
                new EntradaInimigoPorBioma(
                    Bioma.Pantano,
                    CatalogoCriaturas.LoboDoCampo,
                    "Lobo do Campo",
                    24,
                    30,
                    PeriodoDeAparicao.Noite,
                    "Caçadores espreitando no nevoeiro"
                ),
            };
    }
}
