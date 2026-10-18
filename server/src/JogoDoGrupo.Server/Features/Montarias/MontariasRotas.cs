using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Shared.Features.Montarias;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JogoDoGrupo.Server.Features.Montarias;

public sealed record ComprarMontariaEstabuloRequest(int TipoMontaria);

public static class MontariasRotas
{
    public static void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas.MapGet(RotasMontarias.Estabulo, () => Results.Ok(ConfigEstabulo.Ofertas));

        rotas
            .MapPost(
                "/api/personagens/{personagemId:long}/montarias/estabulo/comprar",
                async (
                    long personagemId,
                    ComprarMontariaEstabuloRequest pedido,
                    ServicoDeMontarias servico,
                    CancellationToken ct
                ) =>
                {
                    var (sucesso, codigoErro, mensagem, saldoRestante) =
                        await servico.ComprarMontariaEstabuloAsync(
                            personagemId,
                            pedido.TipoMontaria,
                            sessao: null,
                            ct
                        );

                    if (!sucesso)
                    {
                        return RespostasApi.Erro(
                            StatusCodes.Status409Conflict,
                            codigoErro ?? CodigosErroMontarias.MontariaNaoAVenda
                        );
                    }

                    return Results.Ok(
                        new RespostaCompraEstabulo(
                            sucesso: true,
                            tipoMontaria: pedido.TipoMontaria,
                            saldoRestante: saldoRestante
                        )
                    );
                }
            )
            .RequireAuthorization();
    }
}
