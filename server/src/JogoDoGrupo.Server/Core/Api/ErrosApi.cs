using JogoDoGrupo.Shared.Core.Api;

namespace JogoDoGrupo.Server.Core.Api;

internal static class ErrosApi
{
    /// <summary>
    /// Garante o corpo <see cref="ErroApi"/> nos erros gerados pelo próprio ASP.NET Core
    /// (exceções, JSON malformado, Content-Type errado). Vai no começo do pipeline.
    /// </summary>
    public static WebApplication UsarErrosApi(this WebApplication app)
    {
        // Sem Content-Type JSON o ASP.NET responde 415 sem corpo; para o contrato é 400.
        app.UseStatusCodePages(async contexto =>
        {
            var http = contexto.HttpContext;
            if (http.Response.StatusCode == StatusCodes.Status415UnsupportedMediaType)
            {
                await RespostasApi.EscreverErroAsync(
                    http,
                    StatusCodes.Status400BadRequest,
                    CodigosErro.RequisicaoInvalida,
                    "Envie o corpo como application/json."
                );
            }
        });
        app.UseMiddleware<TratamentoErrosMiddleware>();
        return app;
    }
}
