using JogoDoGrupo.Shared.Core.Api;

namespace JogoDoGrupo.Server.Core.Api;

/// <summary>
/// Respostas de erro no formato do contrato (<see cref="ErroApi"/>). Toda rota que responde
/// 4xx ou 5xx usa estes métodos, para o client sempre receber um <see cref="ErroApi.Codigo"/>.
/// </summary>
public static class RespostasApi
{
    /// <summary>
    /// Resultado de erro para devolver numa rota.
    /// Ex.: <c>return RespostasApi.Erro(StatusCodes.Status409Conflict, CodigosErro.UsuarioJaExiste);</c>
    /// </summary>
    public static IResult Erro(int status, string codigo, string mensagem = "") =>
        Results.Json(new ErroApi { Codigo = codigo, Mensagem = mensagem }, statusCode: status);

    /// <summary>Escreve o erro direto na resposta (para middlewares e eventos de autenticação).</summary>
    public static Task EscreverErroAsync(
        HttpContext contexto,
        int status,
        string codigo,
        string mensagem = ""
    )
    {
        contexto.Response.StatusCode = status;
        return contexto.Response.WriteAsJsonAsync(
            new ErroApi { Codigo = codigo, Mensagem = mensagem }
        );
    }
}
