namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>
/// Decide se o pedido de velocidade de teste (o <c>.modify speed</c> de GM) vale: só com a
/// chave ligada E no ambiente Development, no mesmo padrão dos comandos de teste
/// (<c>PoliticaDeComandos</c>, #505).
/// </summary>
public static class PoliticaDeVelocidadeDeTeste
{
    /// <summary>Chave que libera o pedido (só em appsettings.Development.json).</summary>
    public const string ChavePermitir = "Mundo:PermitirVelocidadeDeTeste";

    /// <summary>
    /// Lê a configuração. Chave ligada fora de Development fica desligada e avisa no log; o
    /// servidor continua subindo.
    /// </summary>
    public static bool Ligada(IConfiguration configuracao, IHostEnvironment ambiente, ILogger log)
    {
        if (!configuracao.GetValue<bool>(ChavePermitir))
        {
            return false;
        }

        if (!ambiente.IsDevelopment())
        {
            log.LogWarning(
                "{Chave} está ligada, mas o ambiente é \"{Ambiente}\": velocidade de teste desligada.",
                ChavePermitir,
                ambiente.EnvironmentName
            );
            return false;
        }

        return true;
    }
}
