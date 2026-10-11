using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Shared.Core.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace JogoDoGrupo.Server.Core.Autenticacao;

internal static class AutenticacaoJwt
{
    /// <summary>
    /// Registra a validação do token (header <c>Authorization: Bearer &lt;token&gt;</c>) e o
    /// <see cref="GeradorTokens"/>. Rotas protegidas usam <c>RequireAuthorization()</c>.
    /// </summary>
    public static IServiceCollection AdicionarAutenticacaoJwt(
        this IServiceCollection servicos,
        IConfiguration configuracao,
        IHostEnvironment ambiente
    )
    {
        var jwt = ConfiguracaoJwt.Ler(configuracao, ambiente);
        servicos.AddSingleton(jwt);
        servicos.AddSingleton(TimeProvider.System);
        servicos.AddSingleton<GeradorTokens>();

        servicos
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opcoes =>
            {
                // Mantém "sub" com o nome original (sem mapear para ClaimTypes.NameIdentifier).
                opcoes.MapInboundClaims = false;
                opcoes.TokenValidationParameters = jwt.ParametrosDeValidacao();
                opcoes.Events = new JwtBearerEvents
                {
                    OnForbidden = contexto =>
                        RespostasApi.EscreverErroAsync(
                            contexto.HttpContext,
                            StatusCodes.Status403Forbidden,
                            CodigosErro.AcessoNegado
                        ),
                    // 401 sempre com corpo ErroApi, como o contrato pede.
                    OnChallenge = async contexto =>
                    {
                        contexto.HandleResponse();
                        await RespostasApi.EscreverErroAsync(
                            contexto.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            CodigosErro.NaoAutenticado,
                            "Token ausente, inválido ou expirado."
                        );
                    },
                };
            });
        servicos.AddAuthorization();

        return servicos;
    }
}
