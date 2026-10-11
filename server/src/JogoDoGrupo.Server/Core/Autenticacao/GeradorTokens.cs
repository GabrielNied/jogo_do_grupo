using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace JogoDoGrupo.Server.Core.Autenticacao;

/// <summary>Gera o JWT do login. A claim <c>sub</c> é o Id da conta.</summary>
internal sealed class GeradorTokens(ConfiguracaoJwt jwt, TimeProvider relogio)
{
    private readonly JsonWebTokenHandler _handler = new();
    private readonly SigningCredentials _credenciais = new(
        jwt.ChaveDeAssinatura(),
        SecurityAlgorithms.HmacSha256
    );

    public (string Token, DateTimeOffset ExpiraEm) Gerar(long contaId)
    {
        var agora = relogio.GetUtcNow();
        var expiraEm = agora.AddHours(jwt.ValidadeHoras);

        var token = _handler.CreateToken(
            new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Sub, contaId.ToString()),
                ]),
                Issuer = jwt.Emissor,
                Audience = jwt.Audiencia,
                IssuedAt = agora.UtcDateTime,
                NotBefore = agora.UtcDateTime,
                Expires = expiraEm.UtcDateTime,
                SigningCredentials = _credenciais,
            }
        );

        return (token, expiraEm);
    }
}
