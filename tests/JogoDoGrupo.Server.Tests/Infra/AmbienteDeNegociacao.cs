using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Infra;

/// <summary>
/// Apoio aos testes de troca direta e mercado: cria personagens no banco do servidor de teste e
/// dá coroas e itens a eles, e lê de volta o que cada um tem (saldo e bolsa).
/// </summary>
public sealed class AmbienteDeNegociacao(ServidorDeTeste servidor)
{
    public async Task<long> NovoPersonagemAsync()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        return (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;
    }

    public async Task CreditarAsync(long personagemId, long valor)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync();
        await escopo
            .ServiceProvider.GetRequiredService<ICarteiraCoroas>()
            .CreditarAsync(personagemId, valor, default);
        await tx.CommitAsync();
    }

    public async Task<long> SaldoAsync(long personagemId)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        return await escopo
            .ServiceProvider.GetRequiredService<ICarteiraCoroas>()
            .SaldoAsync(personagemId, default);
    }

    /// <summary>Põe <paramref name="quantidade"/> itens do modelo na bolsa, pelo mesmo caminho da loja.</summary>
    public async Task DarItensAsync(long personagemId, int itemId, int quantidade = 1)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var ok = await escopo
            .ServiceProvider.GetRequiredService<IInventarioEconomia>()
            .AdicionarItemCompradoAsync(personagemId, itemId, quantidade, default);
        Assert.True(ok, "a bolsa de teste devia ter espaço");
    }

    public async Task<List<EntradaInventario>> BolsaAsync(long personagemId)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        return await escopo
            .ServiceProvider.GetRequiredService<ICustodiaDeItens>()
            .ListarNegociaveisAsync(personagemId, default);
    }
}
