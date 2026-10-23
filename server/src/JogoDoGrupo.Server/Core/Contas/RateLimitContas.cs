using System.Threading.RateLimiting;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Shared.Core.Api;
using Microsoft.AspNetCore.RateLimiting;

namespace JogoDoGrupo.Server.Core.Contas;

/// <summary>
/// Limite de requisições por IP em rotas sensíveis de contas: registrar, login e troca de senha
/// por minuto (#517) e recuperação por e-mail por hora (#1167).
/// </summary>
public static class RateLimitContas
{
    public const string PoliticaRateLimitContas = "ContasPorIp";
    public const int LimitePadraoPorMinuto = 15;

    /// <summary>Pedidos de código de recuperação por IP numa hora (#1167).</summary>
    public const string PoliticaPedidosRecuperacao = "RecuperacaoPedidosPorIp";
    public const int PedidosRecuperacaoPorHoraPadrao = 5;

    /// <summary>Tentativas de redefinir a senha com código por IP numa hora (#1167).</summary>
    public const string PoliticaRedefinicaoSenha = "RecuperacaoRedefinicoesPorIp";
    public const int RedefinicoesPorHoraPadrao = 10;

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

            AdicionarPoliticaPorIp(
                opcoes,
                PoliticaRateLimitContas,
                ativo,
                limite,
                TimeSpan.FromMinutes(1)
            );
            AdicionarPoliticaPorIp(
                opcoes,
                PoliticaPedidosRecuperacao,
                ativo,
                configuracao.GetValue<int?>("Contas:Recuperacao:PedidosPorHoraPorIp")
                    ?? PedidosRecuperacaoPorHoraPadrao,
                TimeSpan.FromHours(1)
            );
            AdicionarPoliticaPorIp(
                opcoes,
                PoliticaRedefinicaoSenha,
                ativo,
                configuracao.GetValue<int?>("Contas:Recuperacao:RedefinicoesPorHoraPorIp")
                    ?? RedefinicoesPorHoraPadrao,
                TimeSpan.FromHours(1)
            );
        });
    }

    private static void AdicionarPoliticaPorIp(
        RateLimiterOptions opcoes,
        string politica,
        bool ativo,
        int limite,
        TimeSpan janela
    )
    {
        opcoes.AddPolicy(
            politica,
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
                        Window = janela,
                        QueueLimit = 0,
                    }
                );
            }
        );
    }
}
