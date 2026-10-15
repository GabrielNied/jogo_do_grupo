namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>Números dos inimigos comuns dentro das instâncias de dungeon (sem respawn).</summary>
public static class ConfigInimigosDaInstancia
{
    /// <summary>Id inicial dos inimigos de instância; cada grupo reserva um bloco.</summary>
    public const long PrimeiroId = 1_000_000_300_000;

    /// <summary>Quantos ids reservar por grupo (o grupo não repõe, mas o teste pode acrescentar).</summary>
    public const int IdsPorGrupo = 32;
}
