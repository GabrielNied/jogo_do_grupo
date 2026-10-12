namespace JogoDoGrupo.Server.Features.Desenvolvimento;

/// <summary>
/// Decide quem pode usar os comandos de teste: só com a chave ligada E no ambiente
/// Development (mesmo padrão do <c>ConfiguracaoJwt</c>), e, se a lista de contas existir,
/// só as contas dela.
/// </summary>
public sealed class PoliticaDeComandos
{
    /// <summary>Chave que libera os comandos (só em appsettings.Development.json).</summary>
    public const string ChavePermitirComandos = "Desenvolvimento:PermitirComandos";

    /// <summary>Ids de conta autorizados. Vazia ou ausente: qualquer conta (só em Development).</summary>
    public const string ChaveContas = "Desenvolvimento:Contas";

    private readonly HashSet<long> _contas;

    private PoliticaDeComandos(bool ligada, HashSet<long> contas)
    {
        Ligada = ligada;
        _contas = contas;
    }

    /// <summary>Verdadeiro só quando a chave está ligada e o ambiente é Development.</summary>
    public bool Ligada { get; }

    /// <summary>Verdadeiro se a política está ligada e a conta pode usar os comandos.</summary>
    public bool ContaPermitida(long contaId) =>
        Ligada && (_contas.Count == 0 || _contas.Contains(contaId));

    /// <summary>
    /// Lê a configuração. Chave ligada fora de Development desliga os comandos e avisa no
    /// log; o servidor continua subindo.
    /// </summary>
    public static PoliticaDeComandos Ler(
        IConfiguration configuracao,
        IHostEnvironment ambiente,
        ILogger log
    )
    {
        var chave = configuracao.GetValue<bool>(ChavePermitirComandos);
        if (chave && !ambiente.IsDevelopment())
        {
            log.LogWarning(
                "{Chave} está ligada, mas o ambiente é \"{Ambiente}\": comandos de teste desligados.",
                ChavePermitirComandos,
                ambiente.EnvironmentName
            );
            chave = false;
        }

        var contas = configuracao.GetSection(ChaveContas).Get<long[]>() ?? [];
        return new PoliticaDeComandos(chave, [.. contas]);
    }
}
