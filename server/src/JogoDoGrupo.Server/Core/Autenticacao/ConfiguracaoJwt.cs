using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace JogoDoGrupo.Server.Core.Autenticacao;

/// <summary>
/// Seção <c>Jwt</c> da configuração. Em produção, a chave vem da variável de ambiente
/// <c>Jwt__Chave</c> (nunca do appsettings commitado).
/// </summary>
public sealed class ConfiguracaoJwt
{
    public const string Secao = "Jwt";

    /// <summary>Segredo do HMAC-SHA256. Precisa de pelo menos 32 bytes.</summary>
    public string Chave { get; set; } = "";
    public string Emissor { get; set; } = "";
    public string Audiencia { get; set; } = "";

    /// <summary>
    /// O JWT precisa de uma data de expiração. 100 anos: para quem joga, a sessão dura até
    /// "Sair da conta", não uma janela curta de site.
    /// </summary>
    public const int ValidadePadraoHoras = 24 * 365 * 100;

    public int ValidadeHoras { get; set; } = ValidadePadraoHoras;

    /// <summary>
    /// Início da chave de desenvolvimento do appsettings.Development.json. Uma chave com este
    /// prefixo é recusada fora do ambiente Development.
    /// </summary>
    private const string PrefixoChaveDesenvolvimento = "SO-PARA-DESENVOLVIMENTO";

    /// <summary>Lê e valida a seção; configuração inválida impede o servidor de subir.</summary>
    internal static ConfiguracaoJwt Ler(IConfiguration configuracao, IHostEnvironment ambiente)
    {
        var jwt =
            configuracao.GetSection(Secao).Get<ConfiguracaoJwt>()
            ?? throw new InvalidOperationException("Falta a seção \"Jwt\" na configuração.");

        if (Encoding.UTF8.GetByteCount(jwt.Chave) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Chave ausente ou com menos de 32 bytes. Em desenvolvimento ela vem do "
                    + "appsettings.Development.json; em produção, defina a variável de ambiente Jwt__Chave."
            );
        }

        if (
            !ambiente.IsDevelopment()
            && jwt.Chave.StartsWith(PrefixoChaveDesenvolvimento, StringComparison.Ordinal)
        )
        {
            throw new InvalidOperationException(
                $"Jwt:Chave é a chave de desenvolvimento, mas o ambiente é \"{ambiente.EnvironmentName}\". "
                    + "Defina uma chave secreta própria na variável de ambiente Jwt__Chave."
            );
        }

        if (string.IsNullOrWhiteSpace(jwt.Emissor) || string.IsNullOrWhiteSpace(jwt.Audiencia))
        {
            throw new InvalidOperationException(
                "Jwt:Emissor e Jwt:Audiencia não podem ficar vazios (veja appsettings.json)."
            );
        }

        if (jwt.ValidadeHoras <= 0)
        {
            throw new InvalidOperationException("Jwt:ValidadeHoras precisa ser maior que zero.");
        }

        return jwt;
    }

    internal SymmetricSecurityKey ChaveDeAssinatura() => new(Encoding.UTF8.GetBytes(Chave));
}
