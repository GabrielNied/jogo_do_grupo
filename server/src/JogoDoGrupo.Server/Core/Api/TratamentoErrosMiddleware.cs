using JogoDoGrupo.Shared.Core.Api;

namespace JogoDoGrupo.Server.Core.Api;

/// <summary>
/// Converte exceções em <see cref="ErroApi"/>: corpo ausente ou JSON malformado viram
/// 400 <see cref="CodigosErro.RequisicaoInvalida"/>; qualquer outra exceção vira
/// 500 <see cref="CodigosErro.ErroInterno"/>, sem detalhes na resposta (só no log).
/// </summary>
internal sealed class TratamentoErrosMiddleware(
    RequestDelegate proximo,
    ILogger<TratamentoErrosMiddleware> log
)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await proximo(contexto);
        }
        catch (BadHttpRequestException erro) when (!contexto.Response.HasStarted)
        {
            // As rotas lançam esta exceção porque RouteHandlerOptions.ThrowOnBadRequest = true.
            log.LogInformation(
                "Requisição inválida em {Rota}: {Mensagem}",
                contexto.Request.Path,
                erro.Message
            );
            contexto.Response.Clear();
            await RespostasApi.EscreverErroAsync(
                contexto,
                StatusCodes.Status400BadRequest,
                CodigosErro.RequisicaoInvalida,
                "Corpo ausente ou JSON malformado."
            );
        }
        catch (Exception erro)
            when (!contexto.Response.HasStarted && !contexto.RequestAborted.IsCancellationRequested)
        {
            log.LogError(
                erro,
                "Erro não tratado em {Metodo} {Rota}",
                contexto.Request.Method,
                contexto.Request.Path
            );
            contexto.Response.Clear();
            await RespostasApi.EscreverErroAsync(
                contexto,
                StatusCodes.Status500InternalServerError,
                CodigosErro.ErroInterno
            );
        }
    }
}
