using System.Net;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

/// <summary>
/// Testes de favoritar, marcar como lixo e ações em lote (#563).
/// </summary>
public class LixoEFavoritoTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private const int Espada = CatalogoItens.EspadaDeTeste;
    private const int Escudo = CatalogoItens.EscudoDeTeste;

    private async Task<(HttpClient Cliente, long Id)> Personagem()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        return (cliente, (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id);
    }

    private async Task<long> Item(
        long id,
        int modelo,
        int? casa = null,
        PosicaoEquipamento posicao = PosicaoEquipamento.Inventario,
        bool favorito = false,
        bool lixo = false
    )
    {
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var item = new ItemInventario
        {
            PersonagemId = id,
            ItemId = modelo,
            Posicao = posicao,
            Aba = posicao == PosicaoEquipamento.Inventario && casa != null ? 0 : null,
            Casa = posicao == PosicaoEquipamento.Inventario ? casa : null,
        };
        var dados = new GeradorEquipamentos(
            new ConfigEquipamentos { QuantidadeAleatorios = 0 }
        ).Gerar(modelo, new Random(1));
        if (dados != null)
        {
            dados.Favorito = favorito;
            dados.Lixo = lixo;
            item.GravarEquipamento(dados);
        }
        db.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    private static Task<HttpResponseMessage> Descartar(
        HttpClient cliente,
        long id,
        long instanciaId
    ) =>
        ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.DescartarDe(id),
            new PedidoDescartar { InstanciaId = instanciaId }
        );

    private static Task<HttpResponseMessage> DescartarLote(
        HttpClient cliente,
        long id,
        long[]? ids = null,
        bool apenasLixo = false
    ) =>
        ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.DescartarLoteDe(id),
            new PedidoDescartarLote
            {
                InstanciaIds = ids ?? Array.Empty<long>(),
                ApenasLixo = apenasLixo,
            }
        );

    private static Task<HttpResponseMessage> MarcarLixo(
        HttpClient cliente,
        long id,
        long instanciaId,
        bool lixo,
        long[]? ids = null
    ) =>
        ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.LixoDe(id),
            new PedidoLixo
            {
                InstanciaId = instanciaId,
                Lixo = lixo,
                InstanciaIds = ids ?? Array.Empty<long>(),
            }
        );

    private static Task<HttpResponseMessage> Favoritar(
        HttpClient cliente,
        long id,
        long instanciaId,
        bool favorito,
        long[]? ids = null
    ) =>
        ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.FavoritoDe(id),
            new PedidoFavorito
            {
                InstanciaId = instanciaId,
                Favorito = favorito,
                InstanciaIds = ids ?? Array.Empty<long>(),
            }
        );

    [Fact]
    public async Task DescartarItemFavoritoRetorna409ItemFavoritoENaoApaga()
    {
        var (cliente, id) = await Personagem();
        var item = await Item(id, Espada, casa: 0, favorito: true);

        var resposta = await Descartar(cliente, id, item);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta);
        Assert.Equal(CodigosErroItens.ItemFavorito, erro.Codigo);

        // O item continua no banco
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var persistido = await db.Set<ItemInventario>().FindAsync(item);
        Assert.NotNull(persistido);
    }

    [Fact]
    public async Task MarcarLixoPersisteNoServidor()
    {
        var (cliente, id) = await Personagem();
        var item = await Item(id, Espada, casa: 0);

        var resposta = await MarcarLixo(cliente, id, item, lixo: true);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta);
        var entrada = Assert.Single(estado.Inventario);
        Assert.True(entrada.Equipamento?.Lixo);

        // Desmarcar lixo
        var resposta2 = await MarcarLixo(cliente, id, item, lixo: false);
        Assert.Equal(HttpStatusCode.OK, resposta2.StatusCode);
        var estado2 = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta2);
        var entrada2 = Assert.Single(estado2.Inventario);
        Assert.False(entrada2.Equipamento?.Lixo);
    }

    [Fact]
    public async Task MarcarLixoEmItemFavoritoRetorna409ItemFavorito()
    {
        var (cliente, id) = await Personagem();
        var item = await Item(id, Espada, casa: 0, favorito: true);

        var resposta = await MarcarLixo(cliente, id, item, lixo: true);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta);
        Assert.Equal(CodigosErroItens.ItemFavorito, erro.Codigo);
    }

    [Fact]
    public async Task FavoritarItemRemoveLixo()
    {
        var (cliente, id) = await Personagem();
        var item = await Item(id, Espada, casa: 0, lixo: true);

        var resposta = await Favoritar(cliente, id, item, favorito: true);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta);
        var entrada = Assert.Single(estado.Inventario);
        Assert.True(entrada.Equipamento?.Favorito);
        Assert.False(entrada.Equipamento?.Lixo);
    }

    [Fact]
    public async Task DescartarLoteApenasLixoRemoveTodosOsItensMarcados()
    {
        var (cliente, id) = await Personagem();
        var lixo1 = await Item(id, Espada, casa: 0, lixo: true);
        var lixo2 = await Item(id, Escudo, casa: 1, lixo: true);
        var guardado = await Item(id, Espada, casa: 2, lixo: false);

        var resposta = await DescartarLote(cliente, id, apenasLixo: true);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta);
        Assert.Single(estado.Inventario);
        Assert.Equal(guardado, estado.Inventario[0].InstanciaId);

        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        Assert.Null(await db.Set<ItemInventario>().FindAsync(lixo1));
        Assert.Null(await db.Set<ItemInventario>().FindAsync(lixo2));
        Assert.NotNull(await db.Set<ItemInventario>().FindAsync(guardado));
    }

    [Fact]
    public async Task DescartarLotePorIdsRemoveItensEspecificados()
    {
        var (cliente, id) = await Personagem();
        var item1 = await Item(id, Espada, casa: 0);
        var item2 = await Item(id, Escudo, casa: 1);
        var item3 = await Item(id, Espada, casa: 2);

        var resposta = await DescartarLote(cliente, id, ids: new[] { item1, item3 });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta);
        Assert.Single(estado.Inventario);
        Assert.Equal(item2, estado.Inventario[0].InstanciaId);
    }

    [Fact]
    public async Task DescartarLoteComItemFavoritoRecusa409ENaoRemoveNenhum()
    {
        var (cliente, id) = await Personagem();
        var comum = await Item(id, Espada, casa: 0);
        var favorito = await Item(id, Escudo, casa: 1, favorito: true);

        var resposta = await DescartarLote(cliente, id, ids: new[] { comum, favorito });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta);
        Assert.Equal(CodigosErroItens.ItemFavorito, erro.Codigo);

        // Nenhum foi apagado (transação atômica)
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        Assert.NotNull(await db.Set<ItemInventario>().FindAsync(comum));
        Assert.NotNull(await db.Set<ItemInventario>().FindAsync(favorito));
    }

    [Fact]
    public async Task DescartarLoteComItemEquipadoRecusa409()
    {
        var (cliente, id) = await Personagem();
        var vestido = await Item(id, Escudo, posicao: PosicaoEquipamento.Secundaria1);

        var resposta = await DescartarLote(cliente, id, ids: new[] { vestido });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta);
        Assert.Equal(CodigosErroItens.ItemEquipado, erro.Codigo);
    }

    [Fact]
    public async Task AcoesEmLoteDeFavoritarELixoEmMultiplosItens()
    {
        var (cliente, id) = await Personagem();
        var a = await Item(id, Espada, casa: 0);
        var b = await Item(id, Escudo, casa: 1);

        // Marcar ambos como lixo
        var respLixo = await MarcarLixo(cliente, id, 0, lixo: true, ids: new[] { a, b });
        Assert.Equal(HttpStatusCode.OK, respLixo.StatusCode);
        var estadoLixo = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(respLixo);
        Assert.All(estadoLixo.Inventario, e => Assert.True(e.Equipamento?.Lixo));

        // Travar ambos como favorito (deve remover lixo)
        var respFav = await Favoritar(cliente, id, 0, favorito: true, ids: new[] { a, b });
        Assert.Equal(HttpStatusCode.OK, respFav.StatusCode);
        var estadoFav = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(respFav);
        Assert.All(
            estadoFav.Inventario,
            e =>
            {
                Assert.True(e.Equipamento?.Favorito);
                Assert.False(e.Equipamento?.Lixo);
            }
        );
    }

    [Fact]
    public async Task DescartarLoteVazioRetorna200SemErro()
    {
        var (cliente, id) = await Personagem();
        var resposta = await DescartarLote(cliente, id, apenasLixo: true);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta);
        Assert.Equal("Nenhum item descartado.", estado.Aviso);
    }
}
