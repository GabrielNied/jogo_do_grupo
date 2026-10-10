using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace JogoDoGrupo.Server.Core.Autenticacao;

/// <summary>Acesso à conta logada nas rotas protegidas com <c>RequireAuthorization()</c>.</summary>
public static class ContaLogada
{
    /// <summary>
    /// Id da conta do token (claim <c>sub</c>). Só chame em rota com <c>RequireAuthorization()</c>:
    /// lá o token já foi validado. Ex.: <c>(ClaimsPrincipal usuario) =&gt; usuario.ObterContaId()</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Rota sem <c>RequireAuthorization()</c>.</exception>
    public static long ObterContaId(this ClaimsPrincipal usuario)
    {
        var sub = usuario.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (
            sub == null
            || !long.TryParse(sub, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
        )
        {
            throw new InvalidOperationException(
                "Sem conta logada. A rota precisa de RequireAuthorization()."
            );
        }

        return id;
    }
}
