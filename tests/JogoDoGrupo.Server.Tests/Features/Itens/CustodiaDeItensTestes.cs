using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

public class CustodiaDeItensTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private async Task<long> CriarPersonagemAsync()
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

    [Fact]
    public async Task ObterAsync_ItemExisteNaBolsa_RetornaItemEmCustodia()
    {
        var personagemId = await CriarPersonagemAsync();
        long instanciaId;

        using (var escopo = servidor.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var item = new ItemInventario
            {
                PersonagemId = personagemId,
                ItemId = 100,
                Posicao = PosicaoEquipamento.Inventario,
                Conteiner = ConteinerItem.Bolsa,
                Aba = 0,
                Casa = 1,
            };
            db.Set<ItemInventario>().Add(item);
            await db.SaveChangesAsync();
            instanciaId = item.Id;
        }

        using (var escopo = servidor.Services.CreateScope())
        {
            var custodia = escopo.ServiceProvider.GetRequiredService<ICustodiaDeItens>();
            var obtido = await custodia.ObterAsync(
                personagemId,
                instanciaId,
                CancellationToken.None
            );

            Assert.NotNull(obtido);
            Assert.Equal(100, obtido.ItemId);
        }
    }

    [Fact]
    public async Task ObterAsync_ItemNaoExiste_RetornaNulo()
    {
        var personagemId = await CriarPersonagemAsync();

        using var escopo = servidor.Services.CreateScope();
        var custodia = escopo.ServiceProvider.GetRequiredService<ICustodiaDeItens>();
        var obtido = await custodia.ObterAsync(personagemId, 999999, CancellationToken.None);

        Assert.Null(obtido);
    }
}
