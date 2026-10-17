namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Configurações do chefe raro no servidor. Valores em constante nomeada (regra 17).
/// </summary>
public static class ConfigChefeRaro
{
    /// <summary>
    /// Tempo máximo que o chefe raro fica no mundo antes de sumir se não for morto (30 minutos = 1800 s).
    /// Enquanto houver combate ativo, este timer pausa.
    /// </summary>
    public const int TempoDePermanenciaSegundos = 1800;

    /// <summary>Intervalo padrão entre tentativas de sorteio de aparição (30 minutos = 1800 s).</summary>
    public const int IntervaloSorteioSegundos = 1800;
}
