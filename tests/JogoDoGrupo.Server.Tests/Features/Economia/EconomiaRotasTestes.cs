using System.Net;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Economia;

public class EconomiaRotasTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private async Task<(HttpClient Cliente, long Id)> NovoPersonagemAsync()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;
        return (cliente, id);
    }

    private async Task CreditarCoroasAsync(long personagemId, long valor)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync();
        var carteira = escopo.ServiceProvider.GetRequiredService<ICarteiraCoroas>();
        await carteira.CreditarAsync(personagemId, valor, default);
        await tx.CommitAsync();
    }

    [Fact]
    public async Task Saldo_InicialZero_RetornaOkComZero()
    {
        var (cliente, id) = await NovoPersonagemAsync();

        var res = await cliente.GetAsync(RotasEconomia.SaldoDe(id));
        res.EnsureSuccessStatusCode();

        var dados = await ServidorDeTeste.LerJsonAsync<SaldoCoroasResponse>(res);
        Assert.Equal(0, dados.Saldo);
    }

    [Fact]
    public async Task Catalogo_RetornaItensConfigurados()
    {
        var (cliente, id) = await NovoPersonagemAsync();

        var res = await cliente.GetAsync(RotasEconomia.LojaDe(id));
        res.EnsureSuccessStatusCode();

        var dados = await ServidorDeTeste.LerJsonAsync<RespostaCatalogoLoja>(res);
        Assert.NotEmpty(dados.Itens);
        Assert.Contains(dados.Itens, i => i.ItemId == CatalogoItens.PocaoDeTeste);
    }

    [Fact]
    public async Task Compra_SaldoInsuficiente_Retorna409()
    {
        var (cliente, id) = await NovoPersonagemAsync();

        var res = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasEconomia.ComprarDe(id),
            new PedidoComprarItem(CatalogoItens.PocaoDeTeste, 1)
        );

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(res);
        Assert.Equal(CodigosErroEconomia.SaldoInsuficiente, erro.Codigo);
    }

    [Fact]
    public async Task Compra_ComSaldo_DebitaCoroasEAdicionaItemNaBolsa()
    {
        var (cliente, id) = await NovoPersonagemAsync();
        await CreditarCoroasAsync(id, 100);

        var resCompra = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasEconomia.ComprarDe(id),
            new PedidoComprarItem(CatalogoItens.PocaoDeTeste, 1)
        );
        resCompra.EnsureSuccessStatusCode();

        var compra = await ServidorDeTeste.LerJsonAsync<ItemCompradoDaLoja>(resCompra);
        Assert.Equal(CatalogoItens.PocaoDeTeste, compra.ItemId);
        Assert.Equal(15, compra.CustoTotal);
        Assert.Equal(85, compra.SaldoAtualizado);

        // Confere que item está no inventário
        var resInv = await cliente.GetAsync(RotasItens.InventarioDe(id));
        resInv.EnsureSuccessStatusCode();
        var inv = await ServidorDeTeste.LerJsonAsync<InventarioResponse>(resInv);
        Assert.Contains(inv.Entradas, e => e.ItemId == CatalogoItens.PocaoDeTeste);
    }

    [Fact]
    public async Task Venda_ItemValido_CreditaCoroasPorTierERemoveItem()
    {
        var (cliente, id) = await NovoPersonagemAsync();

        // Insere um item no inventário
        long instanciaId;
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var item = new ItemInventario
            {
                PersonagemId = id,
                ItemId = CatalogoItens.EspadaDeTeste,
                Posicao = PosicaoEquipamento.Inventario,
                Aba = 1,
                Casa = 0,
            };
            var eq = new Equipamento
            {
                ModeloId = CatalogoItens.EspadaDeTeste,
                Nome = "Espada D",
                Tipo = TipoEquipamento.Espada,
                TierOrigem = TierItem.D,
                DurabilidadeAtual = 100,
                DurabilidadeMaxima = 100,
            };
            item.GravarEquipamento(eq);
            db.Set<ItemInventario>().Add(item);
            await db.SaveChangesAsync();
            instanciaId = item.Id;
        }

        var resVenda = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasEconomia.VenderDe(id),
            new PedidoVenderItem(instanciaId)
        );
        resVenda.EnsureSuccessStatusCode();

        var venda = await ServidorDeTeste.LerJsonAsync<ItemVendidoParaNpc>(resVenda);
        Assert.Equal(instanciaId, venda.InstanciaId);
        Assert.Equal(ConfigEconomia.PrecoVendaPorTier(TierItem.D), venda.CoroasRecebidas);
        Assert.Equal(ConfigEconomia.PrecoVendaPorTier(TierItem.D), venda.SaldoAtualizado);

        // Confere que o item saiu do inventário
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var itemNoBanco = await db.Set<ItemInventario>().FindAsync(instanciaId);
            Assert.Null(itemNoBanco);
        }
    }

    [Fact]
    public async Task Venda_ItemFavorito_Retorna409ItemFavorito()
    {
        var (cliente, id) = await NovoPersonagemAsync();

        long instanciaId;
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var item = new ItemInventario
            {
                PersonagemId = id,
                ItemId = CatalogoItens.EspadaDeTeste,
                Posicao = PosicaoEquipamento.Inventario,
                Aba = 1,
                Casa = 0,
            };
            var eq = new Equipamento
            {
                ModeloId = CatalogoItens.EspadaDeTeste,
                Nome = "Espada Favorita",
                Tipo = TipoEquipamento.Espada,
                Favorito = true,
            };
            item.GravarEquipamento(eq);
            db.Set<ItemInventario>().Add(item);
            await db.SaveChangesAsync();
            instanciaId = item.Id;
        }

        var resVenda = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasEconomia.VenderDe(id),
            new PedidoVenderItem(instanciaId)
        );

        Assert.Equal(HttpStatusCode.Conflict, resVenda.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(resVenda);
        Assert.Equal(CodigosErroEconomia.ItemFavorito, erro.Codigo);
    }

    [Fact]
    public async Task Reparo_ItemSemDano_Retorna409ItemJaReparado()
    {
        var (cliente, id) = await NovoPersonagemAsync();

        long instanciaId;
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var item = new ItemInventario
            {
                PersonagemId = id,
                ItemId = CatalogoItens.EspadaDeTeste,
                Posicao = PosicaoEquipamento.Arma1,
            };
            var eq = new Equipamento
            {
                ModeloId = CatalogoItens.EspadaDeTeste,
                Nome = "Espada 100%",
                Tipo = TipoEquipamento.Espada,
                DurabilidadeAtual = 100,
                DurabilidadeMaxima = 100,
            };
            item.GravarEquipamento(eq);
            db.Set<ItemInventario>().Add(item);
            await db.SaveChangesAsync();
            instanciaId = item.Id;
        }

        var res = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasEconomia.RepararDe(id),
            new PedidoRepararItem(instanciaId)
        );

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(res);
        Assert.Equal(CodigosErroEconomia.ItemJaReparado, erro.Codigo);
    }

    [Fact]
    public async Task Reparo_ItemDanificadoComSaldo_RestauraDurabilidadeEDebita()
    {
        var (cliente, id) = await NovoPersonagemAsync();
        await CreditarCoroasAsync(id, 50);

        long instanciaId;
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var item = new ItemInventario
            {
                PersonagemId = id,
                ItemId = CatalogoItens.EspadaDeTeste,
                Posicao = PosicaoEquipamento.Arma1,
            };
            var eq = new Equipamento
            {
                ModeloId = CatalogoItens.EspadaDeTeste,
                Nome = "Espada Danificada",
                Tipo = TipoEquipamento.Espada,
                DurabilidadeAtual = 50,
                DurabilidadeMaxima = 100,
            };
            item.GravarEquipamento(eq);
            db.Set<ItemInventario>().Add(item);
            await db.SaveChangesAsync();
            instanciaId = item.Id;
        }

        var res = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasEconomia.RepararDe(id),
            new PedidoRepararItem(instanciaId)
        );
        res.EnsureSuccessStatusCode();

        var rep = await ServidorDeTeste.LerJsonAsync<ItemReparado>(res);
        Assert.Equal(100, rep.DurabilidadeRestaurada);
        // Dano de 50: (50 + 4) / 5 = 10 coroas
        Assert.Equal(10, rep.CustoCoroas);
        Assert.Equal(40, rep.SaldoAtualizado);

        // Confere durabilidade no banco
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var item = await db.Set<ItemInventario>().FindAsync(instanciaId);
            var eq = item!.LerEquipamento();
            Assert.Equal(100, eq!.DurabilidadeAtual);
            Assert.False(eq.Quebrado);
        }
    }

    [Fact]
    public async Task ItemQuebrado_NaoConcedeBonusCombate()
    {
        var (cliente, id) = await NovoPersonagemAsync();

        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var item = new ItemInventario
            {
                PersonagemId = id,
                ItemId = CatalogoItens.EspadaDeTeste,
                Posicao = PosicaoEquipamento.Arma1,
            };
            var eq = new Equipamento
            {
                ModeloId = CatalogoItens.EspadaDeTeste,
                Nome = "Espada Quebrada",
                Tipo = TipoEquipamento.Espada,
                DurabilidadeAtual = 0,
                DurabilidadeMaxima = 100,
                Atributos = new[]
                {
                    new AtributoItem { Status = StatusItem.DanoFisico, Valor = 50 },
                },
            };
            item.GravarEquipamento(eq);
            db.Set<ItemInventario>().Add(item);
            await db.SaveChangesAsync();
        }

        // Verifica que fonte de modificadores e arma equipada ignoram o item quebrado
        using var escopoConsulta = servidor.Services.CreateScope();
        var fonteMod = escopoConsulta.ServiceProvider.GetRequiredService<IFonteDeModificadores>();
        var mods = await fonteMod.ObterAsync(id, default);
        Assert.Empty(mods);

        var armaEquipada = escopoConsulta.ServiceProvider.GetRequiredService<IArmaEquipada>();
        var arma = await armaEquipada.ObterAsync(id, default);
        Assert.Null(arma);
    }
}
