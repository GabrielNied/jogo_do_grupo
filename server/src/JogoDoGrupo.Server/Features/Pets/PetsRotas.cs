using JogoDoGrupo.Shared.Features.Pets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JogoDoGrupo.Server.Features.Pets;

public static class PetsRotas
{
    public static void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas
            .MapGet(RotasPets.OfertasCuidador, () => Results.Ok(ConfigCuidadorDePets.Ofertas))
            .WithName("ObterOfertasCuidador")
            .WithTags("Pets");

        rotas
            .MapPost(
                RotasPets.ComprarCuidador,
                async (
                    long personagemId,
                    ComprarPetCuidador pedido,
                    ServicoDePets servico,
                    CancellationToken ct
                ) =>
                {
                    var (sucesso, codigoErro, mensagem, saldoRestante) =
                        await servico.ComprarPetCuidadorAsync(
                            personagemId,
                            pedido.Especie,
                            null,
                            ct
                        );

                    if (!sucesso)
                    {
                        return Results.BadRequest(
                            new RespostaCompraPet(
                                false,
                                pedido.Especie,
                                codigoErro,
                                mensagem,
                                saldoRestante
                            )
                        );
                    }

                    return Results.Ok(
                        new RespostaCompraPet(true, pedido.Especie, null, mensagem, saldoRestante)
                    );
                }
            )
            .WithName("ComprarPetCuidador")
            .WithTags("Pets");
    }
}
