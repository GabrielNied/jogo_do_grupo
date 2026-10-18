using System.Threading.RateLimiting;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Shared.Core.Api;
using Microsoft.AspNetCore.RateLimiting;

namespace JogoDoGrupo.Server.Core.Contas;

/// <summary>
/// Limite de requisições por IP em rotas sensíveis de contas (/registrar e /login) (#517).
/// </summary>
public static class RateLimitContas
{
    public const string PoliticaRateLimitContas = "ContasPorIp";
    public const int LimitePadraoPorMinuto = 15;

    public static IServiceCollection AdicionarRateLimiting(
        this IServiceCollection servicos,
        IConfiguration configuracao
    )
    {
        var ativo = configuracao.GetValue<bool?>("Contas:RateLimit:Ativo") ?? true;
        var limite =
            configuracao.GetValue<int?>("Contas:RateLimit:LimitePorMinuto")
            ?? LimitePadraoPorMinuto;

        return servicos.AddRateLimiter(opcoes =>
        {
            opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opcoes.OnRejected = async (contexto, ct) =>
            {
                await RespostasApi.EscreverErroAsync(
                    contexto.HttpContext,
                    StatusCodes.Status429TooManyRequests,
                    CodigosErro.MuitasTentativas,
                    "Muitas requisições deste endereço. Aguarde antes de tentar novamente."
                );
            };

            opcoes.AddPolicy(
                PoliticaRateLimitContas,
                httpContext =>
                {
                    if (!ativo || limite <= 0)
                    {
                        return RateLimitPartition.GetNoLimiter("ilimitado");
                    }

                    var ip =
                        httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                        ?? "anonimo";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        ip,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = limite,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                        }
                    );
                }
            );
        });
    }
}
