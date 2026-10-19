using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Um ponto onde um grupo de inimigos comuns vive: onde fica, que espécie, a faixa de nível
/// e a faixa de tamanho do grupo (sorteado de novo a cada reaparição do grupo).
/// </summary>
public sealed record PontoDeSpawn(
    string Nome,
    string Area,
    double X,
    double Z,
    int CriaturaId,
    int NivelMinimo,
    int NivelMaximo,
    int TamanhoMinimo,
    int TamanhoMaximo,
    long PrimeiroId,
    RotaDaManada? Rota = null,
    PeriodoDeAparicao Periodo = PeriodoDeAparicao.Sempre,
    int? CriaturaRetaguardaId = null
);

/// <summary>
/// Os pontos de spawn do campo, em dados: quanto mais longe da cidade (origem), mais forte.
/// Para acrescentar um ponto, acrescente uma linha. Proposta, a calibrar.
/// </summary>
public static class PontosDeSpawnDoCampo
{
    /// <summary>Espaço de ids de cada ponto: ids de pontos diferentes nunca se cruzam.</summary>
    public const long EspacoDeIdsPorPonto = 1_000;

    /// <summary>
    /// Quantos inimigos comuns do campo podem estar vivos ao mesmo tempo, somando todos os
    /// pontos.
    /// </summary>
    public const int LimiteDeInimigosVivos = 40;

    /// <summary>
    /// A manada de javalis do pasto: uma volta fechada entre a cidade e o campo (pasto, rio,
    /// repouso, retorno), sempre fora da zona urbana (proposta, a calibrar).
    /// </summary>
    public static readonly RotaDaManada RotaDaManadaDoPasto = new RotaDaManada([
        new PontoDeRota(TipoDePontoDeRota.Pasto, -40, 110, RotaDaManada.EsperaPadraoSegundos),
        new PontoDeRota(TipoDePontoDeRota.Rio, 40, 125, RotaDaManada.EsperaPadraoSegundos),
        new PontoDeRota(TipoDePontoDeRota.Repouso, 55, 90, RotaDaManada.EsperaPadraoSegundos),
        new PontoDeRota(TipoDePontoDeRota.Retorno, 0, 85, RotaDaManada.EsperaPadraoSegundos / 2),
    ]).Validada();

    /// <summary>
    /// Meta de densidade: de qualquer ponto do campo próximo há um ponto de spawn a esta
    /// distância, em metros (cerca de 30 s correndo a 5,5 m/s, descontada a visão do inimigo).
    /// </summary>
    public const double DistanciaMaximaAteUmGrupoMetros = 150.0;

    /// <summary>
    /// O campo próximo onde a meta vale: o retângulo de x entre <c>-LarguraDoCampoProximo</c> e
    /// <c>+LarguraDoCampoProximo</c>, de 40 m (o portão norte) até <c>FimDoCampoProximoZ</c>.
    /// </summary>
    public const double LarguraDoCampoProximoMetros = 200.0;

    public const double FimDoCampoProximoZ = 340.0;

    // A ordem é a prioridade no teto de vivos quando o servidor sobe: o que fica perto do
    // portão enche primeiro, e os pontos longe esperam as vagas. Os ids seguem o índice do
    // ponto (o primeiro argumento de Ponto), não a posição na lista.
    public static readonly IReadOnlyList<PontoDeSpawn> Todos =
    [
        Ponto(0, "campo-norte", 0, 190, CatalogoCriaturas.JavaliDoCampo, 2, 4, 3, 5),
        Ponto(1, "campo-leste", 130, 170, CatalogoCriaturas.LoboDoCampo, 3, 5, 2, 4),
        Ponto(2, "campo-oeste", -150, 190, CatalogoCriaturas.TatuRochoso, 4, 6, 2, 3),
        Manada(
            6,
            "manada-do-pasto",
            CatalogoCriaturas.JavaliDoCampo,
            1,
            2,
            5,
            8,
            RotaDaManadaDoPasto
        ),
        // Pontos pequenos entre os grandes e ao lado do caminho do portão (issue #489).
        Ponto(7, "campo-portao-oeste", -80, 150, CatalogoCriaturas.JavaliDoCampo, 2, 3, 2, 3),
        Ponto(8, "campo-portao-leste", 80, 150, CatalogoCriaturas.LoboDoCampo, 2, 3, 2, 3),
        Ponto(9, "campo-oeste-perto", -180, 110, CatalogoCriaturas.LoboDoCampo, 3, 4, 2, 3),
        Ponto(
            10,
            "campo-leste-perto",
            180,
            100,
            CatalogoCriaturas.JavaliDoCampo,
            3,
            4,
            2,
            3,
            PeriodoDeAparicao.Dia
        ),
        Ponto(
            11,
            "campo-norte-meio",
            0,
            260,
            CatalogoCriaturas.JavaliDoCampo,
            4,
            5,
            3,
            4,
            PeriodoDeAparicao.Dia,
            criaturaRetaguardaId: CatalogoCriaturas.ArqueiroDoCampo
        ),
        Ponto(
            12,
            "campo-oeste-meio",
            -90,
            260,
            CatalogoCriaturas.LoboDoCampo,
            4,
            5,
            2,
            3,
            PeriodoDeAparicao.Noite
        ),
        Ponto(13, "campo-leste-meio", 95, 255, CatalogoCriaturas.TatuRochoso, 4, 5, 2, 3),
        Ponto(3, "campo-norte-longe", -20, 330, CatalogoCriaturas.JavaliDoCampo, 6, 8, 3, 5),
        Ponto(4, "campo-leste-longe", 200, 300, CatalogoCriaturas.LoboDoCampo, 8, 10, 2, 4),
        Ponto(5, "campo-oeste-longe", -220, 280, CatalogoCriaturas.TatuRochoso, 7, 9, 2, 3),
        Ponto(
            14,
            "campo-noroeste-longe",
            -100,
            330,
            CatalogoCriaturas.LoboDoCampo,
            6,
            8,
            2,
            3,
            PeriodoDeAparicao.Noite
        ),
    ];

    // Uma manada nasce no primeiro ponto da rota.
    private static PontoDeSpawn Manada(
        int indice,
        string nome,
        int criaturaId,
        int nivelMinimo,
        int nivelMaximo,
        int tamanhoMinimo,
        int tamanhoMaximo,
        RotaDaManada rota,
        PeriodoDeAparicao periodo = PeriodoDeAparicao.Sempre
    ) =>
        Ponto(
            indice,
            nome,
            rota.Pontos[0].X,
            rota.Pontos[0].Z,
            criaturaId,
            nivelMinimo,
            nivelMaximo,
            tamanhoMinimo,
            tamanhoMaximo,
            periodo
        ) with
        {
            Rota = rota,
        };

    private static PontoDeSpawn Ponto(
        int indice,
        string nome,
        double x,
        double z,
        int criaturaId,
        int nivelMinimo,
        int nivelMaximo,
        int tamanhoMinimo,
        int tamanhoMaximo,
        PeriodoDeAparicao periodo = PeriodoDeAparicao.Sempre,
        int? criaturaRetaguardaId = null
    ) =>
        new(
            nome,
            AreasDoMundo.Cidade,
            x,
            z,
            criaturaId,
            nivelMinimo,
            nivelMaximo,
            tamanhoMinimo,
            tamanhoMaximo,
            ConfigInimigosDoCampo.PrimeiroInimigoId + (indice * EspacoDeIdsPorPonto),
            Periodo: periodo,
            CriaturaRetaguardaId: criaturaRetaguardaId
        );
}
