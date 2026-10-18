using System.Globalization;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Contas;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.TempoReal;
using Microsoft.IdentityModel.JsonWebTokens;

namespace JogoDoGrupo.Server.Core.TempoReal;

/// <summary>Resultado da autenticação: <see cref="Codigo"/> vazio quando aceita.</summary>
internal readonly record struct ResultadoAutenticacao(
    string Codigo,
    long ContaId,
    long PersonagemId,
    bool EhGm = false
)
{
    public bool Aceita => Codigo.Length == 0;
}

/// <summary>
/// Valida o JWT do login (mesmas regras do HTTP) e confere se o personagem é da conta.
/// </summary>
internal sealed class AutenticadorTempoReal(
    ConfiguracaoJwt jwt,
    IServiceScopeFactory escopos,
    ILogger<AutenticadorTempoReal> log
)
{
    private readonly JsonWebTokenHandler _handler = new() { MapInboundClaims = false };

    public async Task<ResultadoAutenticacao> AutenticarAsync(
        AutenticarTempoReal pedido,
        CancellationToken cancelamento
    )
    {
        if (string.IsNullOrWhiteSpace(pedido.Token))
        {
            return new(CodigosErro.NaoAutenticado, 0, 0);
        }

        var validacao = await _handler.ValidateTokenAsync(
            pedido.Token,
            jwt.ParametrosDeValidacao()
        );
        if (
            !validacao.IsValid
            || !validacao.Claims.TryGetValue(JwtRegisteredClaimNames.Sub, out var sub)
            || !long.TryParse(
                sub as string,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var contaId
            )
        )
        {
            return new(CodigosErro.NaoAutenticado, 0, 0);
        }

        if (
            !validacao.Claims.TryGetValue(ConfiguracaoJwt.ClaimCarimbo, out var carimboObj)
            || carimboObj is not string carimbo
            || string.IsNullOrEmpty(carimbo)
        )
        {
            return new(CodigosErro.NaoAutenticado, 0, 0);
        }

        using var escopo = escopos.CreateScope();
        var verificadorCarimbo = escopo.ServiceProvider.GetService<IVerificadorCarimboConta>();
        if (verificadorCarimbo != null)
        {
            var carimboValido = await verificadorCarimbo.CarimboValidoAsync(
                contaId,
                carimbo,
                cancelamento
            );
            if (!carimboValido)
            {
                return new(CodigosErro.NaoAutenticado, 0, 0);
            }
        }

        var ehGm = false;
        var db = escopo.ServiceProvider.GetService<Persistencia.JogoDbContext>();
        if (db != null)
        {
            var dadosConta =
                await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    System.Linq.Queryable.Select(
                        System.Linq.Queryable.Where(
                            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(
                                db.Set<Conta>()
                            ),
                            c => c.Id == contaId
                        ),
                        c => new { c.EhGm, c.BanidaAte }
                    ),
                    cancelamento
                );

            if (dadosConta != null)
            {
                if (dadosConta.BanidaAte is { } banidaAte && banidaAte > DateTime.UtcNow)
                {
                    log.LogWarning(
                        "Conexão UDP recusada: conta {ContaId} banida até {BanidaAte}.",
                        contaId,
                        banidaAte
                    );
                    return new(CodigosErro.ContaBanida, contaId, 0, false);
                }

                ehGm = dadosConta.EhGm;
            }
        }

        if (pedido.PersonagemId <= 0)
        {
            return new(CodigosErro.AcessoNegado, contaId, 0);
        }

        var verificador = escopo.ServiceProvider.GetService<IVerificadorPersonagem>();
        if (verificador == null)
        {
            log.LogWarning(
                "Nenhum IVerificadorPersonagem registrado: autenticação de tempo real recusada."
            );
            return new(CodigosErro.AcessoNegado, contaId, 0);
        }

        return await verificador.PertenceAContaAsync(contaId, pedido.PersonagemId, cancelamento)
            ? new("", contaId, pedido.PersonagemId, ehGm)
            : new(CodigosErro.AcessoNegado, contaId, 0, false);
    }
}
