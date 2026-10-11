using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Personagem;

public class VerificadorPersonagemTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private async Task<(long ContaId, long PersonagemId)> CriarAsync()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico("V"), Raca = Raca.Humano }
        );
        resposta.EnsureSuccessStatusCode();
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var p = await db.Set<Server.Features.Personagem.Personagem>().SingleAsync(x => x.Id == id);
        return (p.ContaId, id);
    }

    private async Task<bool> VerificarAsync(long contaId, long personagemId)
    {
        using var escopo = servidor.Services.CreateScope();
        var verificador = escopo.ServiceProvider.GetRequiredService<IVerificadorPersonagem>();
        return await verificador.PertenceAContaAsync(contaId, personagemId, default);
    }

    [Fact]
    public async Task Personagem_da_propria_conta_autentica()
    {
        var (conta, personagem) = await CriarAsync();
        Assert.True(await VerificarAsync(conta, personagem));
    }

    [Fact]
    public async Task Personagem_de_outra_conta_e_recusado()
    {
        var (_, personagemA) = await CriarAsync();
        var (contaB, _) = await CriarAsync();
        Assert.False(await VerificarAsync(contaB, personagemA));
    }

    [Fact]
    public async Task Personagem_inexistente_e_recusado()
    {
        var (conta, _) = await CriarAsync();
        Assert.False(await VerificarAsync(conta, long.MaxValue));
    }
}
