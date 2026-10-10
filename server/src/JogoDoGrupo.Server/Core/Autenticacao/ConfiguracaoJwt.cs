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
    public int ValidadeHoras { get; set; } = 12;

    internal static ConfiguracaoJwt Ler(IConfiguration configuracao)
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

        if (jwt.ValidadeHoras <= 0)
        {
            throw new InvalidOperationException("Jwt:ValidadeHoras precisa ser maior que zero.");
        }

        return jwt;
    }

    internal SymmetricSecurityKey ChaveDeAssinatura() => new(Encoding.UTF8.GetBytes(Chave));
}
