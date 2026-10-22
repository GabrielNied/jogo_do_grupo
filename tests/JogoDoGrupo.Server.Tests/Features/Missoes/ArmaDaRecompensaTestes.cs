using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Features.Missoes;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Missoes;
using JogoDoGrupo.Shared.Features.Npcs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Missoes;

/// <summary>
/// A arma escolhida na etapa 2 sai equipada no conjunto 1 quando o personagem está sem arma; com
/// arma já vestida, vai para a bolsa (#1185).
/// </summary>
public sealed class ArmaDaRecompensaTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    private async Task<List<ItemInventario>> ItensAsync(long id)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        return await db.Set<ItemInventario>()
            .AsNoTracking()
            .Where(i => i.PersonagemId == id)
            .ToListAsync();
    }

    [Fact]
    public async Task EntregaDaEtapa2_SemArma_EquipaNoConjunto1()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var s = escopo.ServiceProvider.GetRequiredService<ServicoDeMissoes>();
            await s.GarantirMissaoInicialAsync(id, default);
            await s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdVendedor, default);
            var (ok1, e1, _, _) = await s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao1PortoesDaBrasaMansa,
                null,
                default
            );
            Assert.True(ok1, e1);
            await s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdArmeiro, default);
            var (ok2, e2, _, _) = await s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao2PrimeiraFerramenta,
                CatalogoEquipamentos.PrimeiroModeloDeArma,
                default
            );
            Assert.True(ok2, e2);
        }

        var itens = await ItensAsync(id);
        var arma = Assert.Single(itens);
        Assert.Equal(PosicaoEquipamento.Arma1, arma.Posicao);
        Assert.Equal(CatalogoEquipamentos.PrimeiroModeloDeArma, arma.ItemId);
    }

    [Fact]
    public async Task Concessao_ComArmaJaEquipada_VaiParaABolsa()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        var modelo = CatalogoEquipamentos.PrimeiroModeloDeArma;
        var destinos = new List<DestinoDaConcessao>();
        for (var i = 0; i < 2; i++)
        {
            await using var escopo = servidor.Services.CreateAsyncScope();
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            await using var tx = await db.Database.BeginTransactionAsync();
            destinos.Add(
                await escopo
                    .ServiceProvider.GetRequiredService<IConcessaoDeItens>()
                    .ConcederAsync(id, modelo, default)
            );
            await tx.CommitAsync();
        }

        Assert.Equal(DestinoDaConcessao.Equipado, destinos[0]);
        Assert.Equal(DestinoDaConcessao.Bolsa, destinos[1]);
        var itens = await ItensAsync(id);
        Assert.Equal(2, itens.Count);
        Assert.Single(itens, i => i.Posicao == PosicaoEquipamento.Arma1);
        Assert.Single(itens, i => i.Posicao == PosicaoEquipamento.Inventario);
    }

    [Fact]
    public async Task Concessao_DeArmadura_VaiParaABolsa()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            await using var tx = await db.Database.BeginTransactionAsync();
            var destino = await escopo
                .ServiceProvider.GetRequiredService<IConcessaoDeItens>()
                .ConcederAsync(id, CatalogoEquipamentos.PrimeiroModelo, default);
            await tx.CommitAsync();
            Assert.Equal(DestinoDaConcessao.Bolsa, destino);
        }

        var item = Assert.Single(await ItensAsync(id));
        Assert.Equal(PosicaoEquipamento.Inventario, item.Posicao);
    }
}
