namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>Um lugar do mundo onde o chefe de mundo pode aparecer (plano do chão).</summary>
public readonly record struct LugarDeChefeDeMundo(string Nome, double X, double Z);

/// <summary>
/// Configurações do chefe de mundo (#191; proposta, a calibrar). A força é fixa e vem da ficha;
/// o sorteio de hora é o mesmo do chefe raro (chance por intervalo da ficha), mas o primeiro só
/// vem depois de um intervalo cheio e o chefe fica até morrer. Valores em constante nomeada
/// (regra 17).
/// </summary>
public static class ConfigChefeDeMundo
{
    /// <summary>
    /// Permanência prática "sem limite": o chefe de mundo não some por tempo (isso é do chefe
    /// raro, #896); só a morte o tira do mundo.
    /// </summary>
    public const int PermanenciaSegundos = 10 * 365 * 24 * 3600;

    /// <summary>
    /// Id do primeiro chefe de mundo; cada chefe ocupa um espaço de ids e cada aparição soma 1.
    /// Longe dos ids do campo, do chefe raro e de personagens.
    /// </summary>
    public const long PrimeiroId = 1_000_000_200_000;

    /// <summary>Espaço de ids de cada chefe de mundo.</summary>
    public const long EspacoDeIdsPorChefe = 1_000;

    /// <summary>
    /// Os lugares sorteados, longe da cidade e dos pontos do campo, em chão firme e fora da
    /// água (um teste confere). Um por região de fora, para o jogador ter motivo de explorar.
    /// </summary>
    public static readonly IReadOnlyList<LugarDeChefeDeMundo> Lugares =
    [
        new("planicie-norte", 0, 700),
        new("colinas-oeste", -700, 150),
        new("mata-leste", 700, 150),
    ];
}
