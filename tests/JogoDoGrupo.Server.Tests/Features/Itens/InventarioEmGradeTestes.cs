using System.Net;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

/// <summary>Bolsa em grade (#560): casas, mover, trocar, desequipar para casa e primeira casa livre.</summary>
public class InventarioEmGradeTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private const int Espada = CatalogoItens.EspadaDeTeste;
    private const int Escudo = CatalogoItens.EscudoDeTeste;
    private const int Martelo = 100;
    private const int Capacete = 101;
    private const int Anel = 107;

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

    /// <summary>Grava o item direto no banco; sem aba/casa grava como linha antiga (sem grade).</summary>
    private async Task<long> Item(
        long id,
        int modelo,
        int? casa = null,
        PosicaoEquipamento posicao = PosicaoEquipamento.Inventario
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
            item.GravarEquipamento(dados);
        db.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    private static async Task<EquipamentosResponse> Estado(HttpClient cliente, long id) =>
        await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(
            await cliente.GetAsync(RegrasEquipamentos.RotaDe(id))
        );

    private static Task<HttpResponseMessage> Mover(
        HttpClient cliente,
        long id,
        long item,
        int aba,
        int casa
    ) =>
        ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.MoverDe(id),
            new PedidoMover
            {
                InstanciaId = item,
                Aba = aba,
                Casa = casa,
            }
        );

    private static Task<HttpResponseMessage> Equipar(
        HttpClient cliente,
        long id,
        long item,
        PosicaoEquipamento posicao
    ) =>
        ServidorDeTeste.PostJsonAsync(
            cliente,
            RegrasEquipamentos.RotaDe(id) + "/equipar",
            new PedidoEquipar { InstanciaId = item, Posicao = posicao }
        );

    private static Task<HttpResponseMessage> Desequipar(
        HttpClient cliente,
        long id,
        PosicaoEquipamento posicao,
        int? aba = null,
        int casa = 0
    ) =>
        ServidorDeTeste.PostJsonAsync(
            cliente,
            RegrasEquipamentos.RotaDe(id) + "/desequipar",
            new PedidoDesequipar
            {
                Posicao = posicao,
                TemDestino = aba != null,
                Aba = aba ?? 0,
                Casa = casa,
            }
        );

    private static async Task<string> Codigo(HttpResponseMessage resposta) =>
        (await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta)).Codigo;

    private static int CasaDe(EquipamentosResponse estado, long instancia) =>
        estado.Inventario.Single(e => e.InstanciaId == instancia).Casa;

    private static void AssertCasasUnicasEValidas(EquipamentosResponse estado)
    {
        Assert.All(estado.Inventario, e => Assert.True(estado.Capacidade.Contem(e.Aba, e.Casa)));
        Assert.Equal(
            estado.Inventario.Length,
            estado.Inventario.Select(e => (e.Aba, e.Casa)).Distinct().Count()
        );
    }

    [Fact]
    public async Task EstadoTrazACapacidadeDaBolsa()
    {
        var (cliente, id) = await Personagem();
        var estado = await Estado(cliente, id);
        Assert.Equal(RegrasItens.AbasBolsaInicial, estado.Capacidade.Abas);
        Assert.Equal(RegrasItens.CasasPorAbaBolsa, estado.Capacidade.CasasPorAba);
    }

    [Fact]
    public async Task BauGuardaNasPrimeirasCasasLivresEmOrdem()
    {
        var (cliente, id) = await Personagem();
        await Item(id, Espada, casa: 0);
        await Item(id, Escudo, casa: 2);
        var resposta = await ServidorDeTeste.PostJsonAsync(cliente, RotasItens.BauDe(id), new { });
        resposta.EnsureSuccessStatusCode();
        var bau = await ServidorDeTeste.LerJsonAsync<AbrirBauResponse>(resposta);
        var esperadas = new[] { 1, 3, 4 }.Take(bau.Ganhos.Length);
        Assert.Equal(esperadas, bau.Ganhos.Select(g => g.Casa));
        Assert.All(bau.Ganhos, g => Assert.Equal(0, g.Aba));
        Assert.Equal(RegrasItens.EspacosInventario, bau.Capacidade.Total);
    }

    [Fact]
    public async Task LinhasAntigasSemCasaSaoNumeradasPelaOrdemDoIdEFicam()
    {
        var (cliente, id) = await Personagem();
        var a = await Item(id, Espada);
        var b = await Item(id, Escudo);
        var c = await Item(id, Capacete);

        var primeira = await Estado(cliente, id);
        Assert.Equal(
            new[] { 0, 1, 2 },
            new[] { CasaDe(primeira, a), CasaDe(primeira, b), CasaDe(primeira, c) }
        );

        // Uma operação que grava (mover o último) persiste a numeração; reler não muda nada.
        (await Mover(cliente, id, c, 0, 10)).EnsureSuccessStatusCode();
        var depois = await Estado(cliente, id);
        Assert.Equal(
            new[] { 0, 1, 10 },
            new[] { CasaDe(depois, a), CasaDe(depois, b), CasaDe(depois, c) }
        );
    }

    [Fact]
    public async Task MoverParaCasaVaziaMudaSoEsseItem()
    {
        var (cliente, id) = await Personagem();
        var a = await Item(id, Espada, casa: 0);
        var b = await Item(id, Escudo, casa: 1);
        var resposta = await Mover(cliente, id, a, 0, 20);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta);
        Assert.Equal(20, CasaDe(estado, a));
        Assert.Equal(1, CasaDe(estado, b));
        Assert.Equal(20, CasaDe(await Estado(cliente, id), a));
    }

    [Fact]
    public async Task MoverParaCasaOcupadaTrocaOsDois()
    {
        var (cliente, id) = await Personagem();
        var a = await Item(id, Espada, casa: 3);
        var b = await Item(id, Escudo, casa: 7);
        (await Mover(cliente, id, a, 0, 7)).EnsureSuccessStatusCode();
        var estado = await Estado(cliente, id);
        Assert.Equal(7, CasaDe(estado, a));
        Assert.Equal(3, CasaDe(estado, b));
        AssertCasasUnicasEValidas(estado);
    }

    [Fact]
    public async Task MoverParaACasaDeOrigemNaoMudaNada()
    {
        var (cliente, id) = await Personagem();
        var a = await Item(id, Espada, casa: 5);
        (await Mover(cliente, id, a, 0, 5)).EnsureSuccessStatusCode();
        Assert.Equal(5, CasaDe(await Estado(cliente, id), a));
    }

    [Theory]
    [InlineData(0, 40)]
    [InlineData(0, -1)]
    [InlineData(1, 0)]
    [InlineData(-1, 0)]
    public async Task MoverParaCasaOuAbaForaDaCapacidadeEh400ENadaMuda(int aba, int casa)
    {
        var (cliente, id) = await Personagem();
        var a = await Item(id, Espada, casa: 0);
        var resposta = await Mover(cliente, id, a, aba, casa);
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(CodigosErroItens.CasaInvalida, await Codigo(resposta));
        Assert.Equal(0, CasaDe(await Estado(cliente, id), a));
    }

    [Fact]
    public async Task MoverItemInexistenteOuDeOutraContaEh404()
    {
        var (cliente, id) = await Personagem();
        var (alheio, idAlheio) = await Personagem();
        var delaAlheia = await Item(idAlheio, Espada, casa: 0);

        var inexistente = await Mover(cliente, id, 999_999_999, 0, 1);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal(CodigosErroItens.ItemNaoEncontrado, await Codigo(inexistente));

        var deOutro = await Mover(cliente, id, delaAlheia, 0, 1);
        Assert.Equal(HttpStatusCode.NotFound, deOutro.StatusCode);
        Assert.Equal(0, CasaDe(await Estado(alheio, idAlheio), delaAlheia));

        var personagemAlheio = await Mover(cliente, idAlheio, delaAlheia, 0, 1);
        Assert.Equal(HttpStatusCode.NotFound, personagemAlheio.StatusCode);
        Assert.Equal(CodigosErroItens.PersonagemNaoEncontrado, await Codigo(personagemAlheio));
    }

    [Fact]
    public async Task MoverItemEquipadoEh409()
    {
        var (cliente, id) = await Personagem();
        var item = await Item(id, Espada, posicao: PosicaoEquipamento.Arma1);
        var resposta = await Mover(cliente, id, item, 0, 4);
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal(CodigosErroItens.ItemEquipado, await Codigo(resposta));
    }

    [Fact]
    public async Task DesequiparSemDestinoVaiParaAPrimeiraCasaLivre()
    {
        var (cliente, id) = await Personagem();
        await Item(id, Escudo, casa: 0);
        await Item(id, Capacete, casa: 2);
        var equipado = await Item(id, Espada, posicao: PosicaoEquipamento.Arma1);
        (await Desequipar(cliente, id, PosicaoEquipamento.Arma1)).EnsureSuccessStatusCode();
        var estado = await Estado(cliente, id);
        Assert.Empty(estado.Equipados);
        Assert.Equal(1, CasaDe(estado, equipado));
    }

    [Fact]
    public async Task DesequiparParaCasaLivreUsaEssaCasa()
    {
        var (cliente, id) = await Personagem();
        var equipado = await Item(id, Espada, posicao: PosicaoEquipamento.Arma1);
        (
            await Desequipar(cliente, id, PosicaoEquipamento.Arma1, aba: 0, casa: 33)
        ).EnsureSuccessStatusCode();
        Assert.Equal(33, CasaDe(await Estado(cliente, id), equipado));
    }

    [Fact]
    public async Task DesequiparParaCasaOcupadaTrocaQuandoOOcupanteCabeNoEncaixe()
    {
        var (cliente, id) = await Personagem();
        var vestida = await Item(id, Espada, posicao: PosicaoEquipamento.Arma1);
        var ocupante = await Item(id, Martelo, casa: 5);
        var resposta = await Desequipar(cliente, id, PosicaoEquipamento.Arma1, aba: 0, casa: 5);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var estado = await Estado(cliente, id);
        Assert.Equal(ocupante, estado.Equipados.Single().InstanciaId);
        Assert.Equal(5, CasaDe(estado, vestida));
    }

    [Fact]
    public async Task DesequiparParaCasaOcupadaPorItemQueNaoCabeNoEncaixeRecusaENadaMuda()
    {
        var (cliente, id) = await Personagem();
        var vestida = await Item(id, Espada, posicao: PosicaoEquipamento.Arma1);
        var capacete = await Item(id, Capacete, casa: 5);
        var resposta = await Desequipar(cliente, id, PosicaoEquipamento.Arma1, aba: 0, casa: 5);
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var estado = await Estado(cliente, id);
        Assert.Equal(vestida, estado.Equipados.Single().InstanciaId);
        Assert.Equal(5, CasaDe(estado, capacete));
    }

    [Fact]
    public async Task DesequiparParaCasaForaDaCapacidadeEh400()
    {
        var (cliente, id) = await Personagem();
        await Item(id, Espada, posicao: PosicaoEquipamento.Arma1);
        var resposta = await Desequipar(cliente, id, PosicaoEquipamento.Arma1, aba: 0, casa: 40);
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(CodigosErroItens.CasaInvalida, await Codigo(resposta));
        Assert.Single((await Estado(cliente, id)).Equipados);
    }

    [Fact]
    public async Task EquiparDevolvePecaDeslocadaParaACasaQueOItemDeixou()
    {
        var (cliente, id) = await Personagem();
        var velha = await Item(id, Espada, posicao: PosicaoEquipamento.Arma1);
        var nova = await Item(id, Espada, casa: 9);
        await Item(id, Escudo, casa: 0);
        (await Equipar(cliente, id, nova, PosicaoEquipamento.Arma1)).EnsureSuccessStatusCode();
        var estado = await Estado(cliente, id);
        Assert.Equal(nova, estado.Equipados.Single().InstanciaId);
        Assert.Equal(9, CasaDe(estado, velha));
    }

    [Fact]
    public async Task EquiparComBolsaCheiaTrocaPecaMasRecusaQuandoDeslocariaMaisQueOsEspacos()
    {
        var (cliente, id) = await Personagem();
        await Item(id, Anel, posicao: PosicaoEquipamento.Anel1);
        var martelo = await Item(id, Martelo, casa: 0);
        for (var casa = 1; casa < RegrasItens.EspacosInventario - 1; casa++)
            await Item(id, Escudo, casa: casa);

        // Cheia (40/40): trocar um anel por outro cabe, porque a casa do anel novo fica livre.
        var anel2 = await Item(id, Anel, casa: RegrasItens.EspacosInventario - 1);
        Assert.Equal(
            HttpStatusCode.OK,
            (await Equipar(cliente, id, anel2, PosicaoEquipamento.Anel1)).StatusCode
        );

        // Martelo (duas mãos) com arma e escudo vestidos desloca duas peças: só uma casa livre.
        await Item(id, Espada, posicao: PosicaoEquipamento.Arma1);
        await Item(id, Escudo, posicao: PosicaoEquipamento.Secundaria1);
        var recusa = await Equipar(cliente, id, martelo, PosicaoEquipamento.Arma1);
        Assert.Equal(HttpStatusCode.Conflict, recusa.StatusCode);
        Assert.Equal(CodigosErroItens.InventarioCheio, await Codigo(recusa));
        var estado = await Estado(cliente, id);
        Assert.Equal(0, CasaDe(estado, martelo));
        Assert.Contains(estado.Equipados, e => e.Posicao == PosicaoEquipamento.Secundaria1);
        AssertCasasUnicasEValidas(estado);
    }

    [Fact]
    public async Task DescartarLiberaACasaParaOProximoItem()
    {
        var (cliente, id) = await Personagem();
        var a = await Item(id, Espada, casa: 0);
        await Item(id, Escudo, casa: 1);
        (
            await ServidorDeTeste.PostJsonAsync(
                cliente,
                RotasItens.DescartarDe(id),
                new PedidoDescartar { InstanciaId = a }
            )
        ).EnsureSuccessStatusCode();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RegrasEquipamentos.RotaDe(id) + "/bau-teste",
            new { }
        );
        resposta.EnsureSuccessStatusCode();
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta);
        Assert.Equal(new[] { 0, 1 }, estado.Inventario.Select(e => e.Casa).OrderBy(c => c));
    }

    [Fact]
    public async Task DoisMoverParaAMesmaCasaNuncaDeixamDuasPecasNela()
    {
        var (cliente, id) = await Personagem();
        var a = await Item(id, Espada, casa: 0);
        var b = await Item(id, Escudo, casa: 1);
        var respostas = await Task.WhenAll(
            Mover(cliente, id, a, 0, 15),
            Mover(cliente, id, b, 0, 15)
        );
        Assert.All(respostas, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var estado = await Estado(cliente, id);
        AssertCasasUnicasEValidas(estado);
        Assert.Contains(15, estado.Inventario.Select(e => e.Casa));
    }

    [Fact]
    public async Task MoverEDescartarJuntosNaoDuplicamNemPerdemOutroItem()
    {
        var (cliente, id) = await Personagem();
        var a = await Item(id, Espada, casa: 0);
        var b = await Item(id, Escudo, casa: 1);
        var respostas = await Task.WhenAll(
            Mover(cliente, id, a, 0, 1),
            ServidorDeTeste.PostJsonAsync(
                cliente,
                RotasItens.DescartarDe(id),
                new PedidoDescartar { InstanciaId = a }
            )
        );
        Assert.All(
            respostas,
            r => Assert.True(r.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound)
        );
        var estado = await Estado(cliente, id);
        AssertCasasUnicasEValidas(estado);
        Assert.Contains(estado.Inventario, e => e.InstanciaId == b);
        Assert.DoesNotContain(estado.Inventario, e => e.InstanciaId == a);
    }

    private static Task<HttpResponseMessage> Ordenar(HttpClient cliente, long id, int aba = 0) =>
        ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.OrdenarDe(id),
            new PedidoOrdenar { Aba = aba }
        );

    private async Task Favoritar(long instancia)
    {
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var item = await db.Set<ItemInventario>().FindAsync(instancia);
        var dados = item!.LerEquipamento()!;
        dados.Favorito = true;
        item.GravarEquipamento(dados);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task OrdenarAgrupaPorTipoDaPrimeiraCasaSemBuracosENaoMexeNoEquipado()
    {
        var (cliente, id) = await Personagem();
        var espada = await Item(id, Espada, casa: 30);
        var anel = await Item(id, Anel, casa: 7);
        var capacete = await Item(id, Capacete, casa: 12);
        var outroAnel = await Item(id, Anel, casa: 1);
        var vestido = await Item(id, Escudo, posicao: PosicaoEquipamento.Secundaria1);

        var resposta = await Ordenar(cliente, id);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta);
        // Capacete, anéis (o de menor identidade antes) e espada; sem buracos.
        Assert.Equal(0, CasaDe(estado, capacete));
        Assert.Equal(1, CasaDe(estado, anel));
        Assert.Equal(2, CasaDe(estado, outroAnel));
        Assert.Equal(3, CasaDe(estado, espada));
        Assert.Equal(vestido, Assert.Single(estado.Equipados).InstanciaId);
        AssertCasasUnicasEValidas(estado);

        // Ordenar de novo uma aba já ordenada não muda nada.
        var denovo = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(
            await Ordenar(cliente, id)
        );
        Assert.Equal(
            estado.Inventario.Select(e => (e.InstanciaId, e.Casa)),
            denovo.Inventario.Select(e => (e.InstanciaId, e.Casa))
        );
    }

    [Fact]
    public async Task OrdenarDeixaOsFavoritosOndeEstao()
    {
        var (cliente, id) = await Personagem();
        var espada = await Item(id, Espada, casa: 9);
        var capacete = await Item(id, Capacete, casa: 4);
        var favorita = await Item(id, Escudo, casa: 1);
        await Favoritar(favorita);

        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(
            await Ordenar(cliente, id)
        );

        Assert.Equal(1, CasaDe(estado, favorita));
        // As outras peças ocupam as casas que sobram, a partir da primeira (a 1 é da favorita).
        Assert.Equal(0, CasaDe(estado, capacete));
        Assert.Equal(2, CasaDe(estado, espada));
        AssertCasasUnicasEValidas(estado);
    }

    [Fact]
    public async Task OrdenarAbaForaDaCapacidadeEh400OutroPersonagemEh404ESemTokenEh401()
    {
        var (cliente, id) = await Personagem();
        var (_, idAlheio) = await Personagem();
        var a = await Item(id, Espada, casa: 5);

        var fora = await Ordenar(cliente, id, aba: 3);
        Assert.Equal(HttpStatusCode.BadRequest, fora.StatusCode);
        Assert.Equal(CodigosErroItens.CasaInvalida, await Codigo(fora));
        Assert.Equal(5, CasaDe(await Estado(cliente, id), a));

        var alheio = await Ordenar(cliente, idAlheio);
        Assert.Equal(HttpStatusCode.NotFound, alheio.StatusCode);
        Assert.Equal(CodigosErroItens.PersonagemNaoEncontrado, await Codigo(alheio));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await Ordenar(servidor.CreateClient(), id)).StatusCode
        );
    }

    [Fact]
    public async Task DuasOrdenacoesSimultaneasNuncaDeixamDuasPecasNaMesmaCasa()
    {
        var (cliente, id) = await Personagem();
        for (var n = 0; n < 8; n++)
            await Item(id, n % 2 == 0 ? Espada : Anel, casa: 30 - n);

        var respostas = await Task.WhenAll(
            Enumerable.Range(0, 4).Select(_ => Ordenar(cliente, id))
        );

        Assert.All(respostas, r => Assert.True(r.StatusCode is HttpStatusCode.OK));
        var estado = await Estado(cliente, id);
        Assert.Equal(8, estado.Inventario.Length);
        AssertCasasUnicasEValidas(estado);
        Assert.Equal(Enumerable.Range(0, 8), estado.Inventario.Select(e => e.Casa).Order());
    }

    [Fact]
    public async Task MoverSemTokenEh401()
    {
        var anonimo = servidor.CreateClient();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            anonimo,
            RotasItens.MoverDe(1),
            new PedidoMover()
        );
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
