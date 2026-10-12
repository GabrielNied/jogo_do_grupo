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
    long PrimeiroId
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
    public const int LimiteDeInimigosVivos = 30;

    public static readonly IReadOnlyList<PontoDeSpawn> Todos =
    [
        Ponto(0, "campo-norte", 0, 190, CatalogoCriaturas.JavaliDoCampo, 2, 4, 3, 5),
        Ponto(1, "campo-leste", 130, 170, CatalogoCriaturas.LoboDoCampo, 3, 5, 2, 4),
        Ponto(2, "campo-oeste", -150, 190, CatalogoCriaturas.TatuRochoso, 4, 6, 2, 3),
        Ponto(3, "campo-norte-longe", 0, 330, CatalogoCriaturas.JavaliDoCampo, 6, 8, 3, 5),
        Ponto(4, "campo-leste-longe", 200, 300, CatalogoCriaturas.LoboDoCampo, 8, 10, 2, 4),
        Ponto(5, "campo-oeste-longe", -220, 280, CatalogoCriaturas.TatuRochoso, 7, 9, 2, 3),
    ];

    private static PontoDeSpawn Ponto(
        int indice,
        string nome,
        double x,
        double z,
        int criaturaId,
        int nivelMinimo,
        int nivelMaximo,
        int tamanhoMinimo,
        int tamanhoMaximo
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
            ConfigInimigosDoCampo.PrimeiroInimigoId + (indice * EspacoDeIdsPorPonto)
        );
}
