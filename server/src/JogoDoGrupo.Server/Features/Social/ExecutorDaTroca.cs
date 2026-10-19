using System.Data;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Social;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Executa a troca direta no banco: uma transação Serializable em que as coroas e os itens dos
/// dois lados mudam de dono juntos ou não mudam. Só usa os contratos de Economia (carteira) e
/// Itens (custódia); as duas pontas de cada item rodam na mesma transação, então o item nunca
/// existe em dois lugares.
/// </summary>
public sealed class ExecutorDaTroca(IServiceScopeFactory escopos) : IExecutorDaTroca
{
    /// <summary>
    /// Uma troca por vez no processo: o servidor tem uma só instância e isto evita disputa de
    /// escrita entre duas trocas ao mesmo tempo (a regra continua valendo se houver mais de uma).
    /// </summary>
    private static readonly SemaphoreSlim Trava = new(1, 1);

    public async Task<(string? Erro, OfertaDeTroca? Oferta)> ValidarOfertaAsync(
        long personagemId,
        OfertaPedida oferta,
        CancellationToken c
    )
    {
        await using var escopo = escopos.CreateAsyncScope();
        var carteira = escopo.ServiceProvider.GetRequiredService<ICarteiraCoroas>();
        var custodia = escopo.ServiceProvider.GetRequiredService<ICustodiaDeItens>();

        if (oferta.Coroas > 0 && await carteira.SaldoAsync(personagemId, c) < oferta.Coroas)
            return (CodigosErroTroca.SaldoInsuficiente, null);

        var bolsa = await custodia.ListarNegociaveisAsync(personagemId, c);
        var itens = new List<JogoDoGrupo.Shared.Features.Itens.EntradaInventario>();
        foreach (var id in oferta.InstanciaIds)
        {
            var entrada = bolsa.FirstOrDefault(e => e.InstanciaId == id);
            if (entrada == null)
                return (CodigosErroTroca.ItemIndisponivel, null);
            itens.Add(entrada);
        }

        return (null, new OfertaDeTroca { Coroas = oferta.Coroas, Itens = itens.ToArray() });
    }

    public async Task<BolsaParaTroca> BolsaAsync(
        long personagemId,
        long trocaId,
        CancellationToken c
    )
    {
        await using var escopo = escopos.CreateAsyncScope();
        var carteira = escopo.ServiceProvider.GetRequiredService<ICarteiraCoroas>();
        var custodia = escopo.ServiceProvider.GetRequiredService<ICustodiaDeItens>();
        return new BolsaParaTroca
        {
            TrocaId = trocaId,
            Coroas = await carteira.SaldoAsync(personagemId, c),
            Itens = (await custodia.ListarNegociaveisAsync(personagemId, c)).ToArray(),
        };
    }

    public async Task<ResultadoDaTroca> ExecutarAsync(
        long personagemA,
        OfertaPedida ofertaA,
        long personagemB,
        OfertaPedida ofertaB,
        CancellationToken c
    )
    {
        await Trava.WaitAsync(c);
        try
        {
            await using var escopo = escopos.CreateAsyncScope();
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var carteira = escopo.ServiceProvider.GetRequiredService<ICarteiraCoroas>();
            var custodia = escopo.ServiceProvider.GetRequiredService<ICustodiaDeItens>();

            // Sem Commit, o Dispose desfaz tudo: qualquer return de erro abaixo não move nada.
            await using var transacao = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                c
            );

            // Coroas: debita os dois lados antes de creditar, para a troca de coroas por coroas
            // não depender da ordem.
            if (ofertaA.Coroas > 0 && !await carteira.DebitarAsync(personagemA, ofertaA.Coroas, c))
                return new ResultadoDaTroca(CodigosErroTroca.SaldoInsuficiente, personagemA);
            if (ofertaB.Coroas > 0 && !await carteira.DebitarAsync(personagemB, ofertaB.Coroas, c))
                return new ResultadoDaTroca(CodigosErroTroca.SaldoInsuficiente, personagemB);
            if (ofertaA.Coroas > 0)
                await carteira.CreditarAsync(personagemB, ofertaA.Coroas, c);
            if (ofertaB.Coroas > 0)
                await carteira.CreditarAsync(personagemA, ofertaB.Coroas, c);

            // Itens: tira todos dos dois lados (libera as casas) e só então entrega cruzado.
            var deA = new List<ItemEmCustodia>();
            foreach (var id in ofertaA.InstanciaIds)
            {
                var item = await custodia.RetirarAsync(personagemA, id, c);
                if (item == null)
                    return new ResultadoDaTroca(CodigosErroTroca.ItemIndisponivel, personagemA);
                deA.Add(item);
            }

            var deB = new List<ItemEmCustodia>();
            foreach (var id in ofertaB.InstanciaIds)
            {
                var item = await custodia.RetirarAsync(personagemB, id, c);
                if (item == null)
                    return new ResultadoDaTroca(CodigosErroTroca.ItemIndisponivel, personagemB);
                deB.Add(item);
            }

            foreach (var item in deA)
            {
                if (!await custodia.DevolverAsync(personagemB, item, c))
                    return new ResultadoDaTroca(CodigosErroTroca.BolsaCheia, personagemB);
            }

            foreach (var item in deB)
            {
                if (!await custodia.DevolverAsync(personagemA, item, c))
                    return new ResultadoDaTroca(CodigosErroTroca.BolsaCheia, personagemA);
            }

            await transacao.CommitAsync(c);
            return ResultadoDaTroca.Ok;
        }
        finally
        {
            Trava.Release();
        }
    }
}
