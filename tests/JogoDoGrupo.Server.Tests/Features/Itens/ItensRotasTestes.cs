using System.Net;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

/// <summary>Sorteador de mentira: devolve sempre o que o teste mandar.</summary>
public sealed class SorteadorFalso : ISorteadorBau
{
    public volatile int[] Resultado = { CatalogoItens.EspadaDeTeste };

    public int[] Sortear() => Resultado;
}

/// <summary>
/// Dois servidores sobre o mesmo banco: o de verdade (sorteio real) e um com o
/// <see cref="SorteadorFalso"/>.
/// </summary>
public sealed class ServidoresDeItens : IDisposable
{
    public ServidorDeTeste Real { get; } = new();
    public SorteadorFalso Sorteador { get; } = new();
    public WebApplicationFactory<Program> ComFalso { get; }

    public ServidoresDeItens()
    {
        ComFalso = Real.WithWebHostBuilder(b =>
            b.ConfigureServices(s => s.AddSingleton<ISorteadorBau>(Sorteador))
        );
    }

    public void Dispose()
    {
        ComFalso.Dispose();
        Real.Dispose();
    }
}

public class ItensRotasTestes(ServidoresDeItens servidores) : IClassFixture<ServidoresDeItens>
{
    private const int Espada = CatalogoItens.EspadaDeTeste;
    private const int Escudo = CatalogoItens.EscudoDeTeste;
    private const int Pocao = CatalogoItens.PocaoDeTeste;
    private const int Moeda = CatalogoItens.MoedaDeTeste;

    private static async Task<long> CriarPersonagem(HttpClient cliente)
    {
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;
    }

    private static async Task<EntradaInventario[]> Inventario(HttpClient cliente, long id)
    {
        var resposta = await cliente.GetAsync(RotasItens.InventarioDe(id));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await ServidorDeTeste.LerJsonAsync<InventarioResponse>(resposta)).Entradas;
    }

    private static Task<HttpResponseMessage> Abrir(HttpClient cliente, long id) =>
        cliente.PostAsync(RotasItens.BauDe(id), null);

    private static async Task<AbrirBauResponse> AbrirComSucesso(HttpClient cliente, long id)
    {
        var resposta = await Abrir(cliente, id);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await ServidorDeTeste.LerJsonAsync<AbrirBauResponse>(resposta);
    }

    private static async Task AssertErro(
        HttpResponseMessage resposta,
        HttpStatusCode status,
        string codigo
    )
    {
        Assert.Equal(status, resposta.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta);
        Assert.Equal(codigo, erro.Codigo);
    }

    private static int[] Ids(EntradaInventario[] entradas) =>
        entradas.Select(e => e.ItemId).ToArray();

    private Task<HttpClient> ClienteComFalso() =>
        ServidorDeTeste.CriarClienteLogadoEmAsync(servidores.ComFalso);

    /// <summary>Enche o inventário com itens conhecidos, até <paramref name="quantidade"/>.</summary>
    private async Task Encher(HttpClient cliente, long id, int quantidade)
    {
        for (var restante = quantidade; restante > 0; restante -= 3)
        {
            var lote = Math.Min(3, restante);
            servidores.Sorteador.Resultado = Enumerable.Repeat(Moeda, lote).ToArray();
            await AbrirComSucesso(cliente, id);
        }
    }

    [Fact]
    public async Task Inventario_de_personagem_novo_vem_vazio()
    {
        var cliente = await servidores.Real.CriarClienteLogadoAsync();
        var id = await CriarPersonagem(cliente);

        var resposta = await cliente.GetAsync(RotasItens.InventarioDe(id));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await ServidorDeTeste.LerJsonAsync<InventarioResponse>(resposta);
        Assert.Empty(corpo.Entradas);
        Assert.Equal(RegrasItens.AbasBolsaInicial, corpo.Capacidade.Abas);
        Assert.Equal(RegrasItens.CasasPorAbaBolsa, corpo.Capacidade.CasasPorAba);
    }

    [Fact]
    public async Task Bau_de_verdade_da_um_a_tres_itens_do_catalogo_e_o_inventario_resultante()
    {
        var cliente = await servidores.Real.CriarClienteLogadoAsync();
        var id = await CriarPersonagem(cliente);

        var resposta = await AbrirComSucesso(cliente, id);

        Assert.InRange(
            resposta.Ganhos.Length,
            RegrasItens.MinimoItensPorBau,
            RegrasItens.MaximoItensPorBau
        );
        Assert.All(
            resposta.Ganhos,
            e =>
            {
                Assert.Contains(e.ItemId, CatalogoItens.Todos);
                Assert.Equal(CatalogoItens.ObterNome(e.ItemId), e.Nome);
            }
        );
        Assert.Equal(Ids(resposta.Ganhos), Ids(resposta.Inventario));
        Assert.Equal(Ids(resposta.Inventario), Ids(await Inventario(cliente, id)));
    }

    [Fact]
    public async Task Bau_grava_exatamente_o_sorteado_com_nomes_do_catalogo()
    {
        var cliente = await ClienteComFalso();
        var id = await CriarPersonagem(cliente);
        servidores.Sorteador.Resultado = new[] { Pocao, Espada };

        var resposta = await AbrirComSucesso(cliente, id);

        Assert.Equal(new[] { Pocao, Espada }, Ids(resposta.Ganhos));
        Assert.Equal(
            new[] { "Poção de teste", "Espada de teste" },
            resposta.Ganhos.Select(e => e.Nome)
        );
        Assert.Equal(Ids(resposta.Ganhos), Ids(resposta.Inventario));
    }

    [Fact]
    public async Task Aberturas_acumulam_em_ordem_e_repetidos_ocupam_espacos_separados()
    {
        var cliente = await ClienteComFalso();
        var id = await CriarPersonagem(cliente);
        servidores.Sorteador.Resultado = new[] { Moeda, Moeda, Escudo };
        await AbrirComSucesso(cliente, id);
        servidores.Sorteador.Resultado = new[] { Moeda };

        var segunda = await AbrirComSucesso(cliente, id);

        Assert.Equal(new[] { Moeda }, Ids(segunda.Ganhos));
        Assert.Equal(new[] { Moeda, Moeda, Escudo, Moeda }, Ids(segunda.Inventario));
    }

    [Fact]
    public async Task Quem_entra_de_novo_ve_os_mesmos_itens()
    {
        var primeira = await ClienteComFalso();
        var id = await CriarPersonagem(primeira);
        servidores.Sorteador.Resultado = new[] { Escudo, Pocao, Moeda };
        var aberto = await AbrirComSucesso(primeira, id);

        // Outro servidor (o de sorteio real) sobre o mesmo banco, com o mesmo token da conta.
        var outroCliente = servidores.Real.CreateClient();
        outroCliente.DefaultRequestHeaders.Authorization = primeira
            .DefaultRequestHeaders
            .Authorization;

        Assert.Equal(Ids(aberto.Inventario), Ids(await Inventario(outroCliente, id)));
        Assert.Equal(Ids(aberto.Inventario), Ids(await Inventario(primeira, id)));
    }

    [Fact]
    public async Task Inventario_e_por_personagem()
    {
        var cliente = await ClienteComFalso();
        var a = await CriarPersonagem(cliente);
        var b = await CriarPersonagem(cliente);
        servidores.Sorteador.Resultado = new[] { Espada };
        await AbrirComSucesso(cliente, a);

        Assert.Single(await Inventario(cliente, a));
        Assert.Empty(await Inventario(cliente, b));
    }

    [Fact]
    public async Task Inventario_com_quarenta_itens_lista_os_quarenta()
    {
        var cliente = await ClienteComFalso();
        var id = await CriarPersonagem(cliente);
        await Encher(cliente, id, RegrasItens.EspacosInventario - 1);
        servidores.Sorteador.Resultado = new[] { Espada };

        var resposta = await AbrirComSucesso(cliente, id);

        Assert.Equal(RegrasItens.EspacosInventario, resposta.Inventario.Length);
        Assert.Equal(Espada, resposta.Inventario[^1].ItemId);
        Assert.Equal(RegrasItens.EspacosInventario, (await Inventario(cliente, id)).Length);
    }

    [Fact]
    public async Task Bau_que_nao_cabe_da_409_e_nao_grava_nada()
    {
        var cliente = await ClienteComFalso();
        var id = await CriarPersonagem(cliente);
        await Encher(cliente, id, RegrasItens.EspacosInventario - 1);
        var antes = Ids(await Inventario(cliente, id));
        servidores.Sorteador.Resultado = new[] { Espada, Escudo, Pocao };

        await AssertErro(
            await Abrir(cliente, id),
            HttpStatusCode.Conflict,
            CodigosErroItens.InventarioCheio
        );

        Assert.Equal(antes, Ids(await Inventario(cliente, id)));
    }

    [Fact]
    public async Task Bau_com_inventario_cheio_da_409()
    {
        var cliente = await ClienteComFalso();
        var id = await CriarPersonagem(cliente);
        await Encher(cliente, id, RegrasItens.EspacosInventario);
        servidores.Sorteador.Resultado = new[] { Espada };

        await AssertErro(
            await Abrir(cliente, id),
            HttpStatusCode.Conflict,
            CodigosErroItens.InventarioCheio
        );

        Assert.Equal(RegrasItens.EspacosInventario, (await Inventario(cliente, id)).Length);
    }

    [Fact]
    public async Task Personagem_inexistente_e_de_outra_conta_dao_o_mesmo_404()
    {
        var dono = await ClienteComFalso();
        var intruso = await ClienteComFalso();
        var id = await CriarPersonagem(dono);
        servidores.Sorteador.Resultado = new[] { Espada };
        await AbrirComSucesso(dono, id);
        const long Inexistente = 999_999_999;

        var deOutro = await intruso.GetAsync(RotasItens.InventarioDe(id));
        var inexistente = await intruso.GetAsync(RotasItens.InventarioDe(Inexistente));
        var bauDeOutro = await Abrir(intruso, id);
        var bauInexistente = await Abrir(intruso, Inexistente);

        var corpoDeOutro = await deOutro.Content.ReadAsStringAsync();
        Assert.Equal(corpoDeOutro, await inexistente.Content.ReadAsStringAsync());
        Assert.Equal(corpoDeOutro, await bauDeOutro.Content.ReadAsStringAsync());
        Assert.Equal(corpoDeOutro, await bauInexistente.Content.ReadAsStringAsync());
        foreach (var resposta in new[] { deOutro, inexistente, bauDeOutro, bauInexistente })
        {
            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        }

        await AssertErro(
            deOutro,
            HttpStatusCode.NotFound,
            CodigosErroItens.PersonagemNaoEncontrado
        );
        // O dono continua com o que tinha: o intruso não gravou nada.
        Assert.Single(await Inventario(dono, id));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("99999999999999999999999")]
    public async Task Id_que_nao_e_de_personagem_da_404(string id)
    {
        var cliente = await ClienteComFalso();

        await AssertErro(
            await cliente.GetAsync($"/api/personagens/{id}/inventario"),
            HttpStatusCode.NotFound,
            CodigosErroItens.PersonagemNaoEncontrado
        );
        await AssertErro(
            await cliente.PostAsync($"/api/personagens/{id}/bau", null),
            HttpStatusCode.NotFound,
            CodigosErroItens.PersonagemNaoEncontrado
        );
    }

    [Fact]
    public async Task Sem_token_da_401()
    {
        var anonimo = servidores.Real.CreateClient();

        await AssertErro(
            await anonimo.GetAsync(RotasItens.InventarioDe(1)),
            HttpStatusCode.Unauthorized,
            CodigosErro.NaoAutenticado
        );
        await AssertErro(
            await Abrir(anonimo, 1),
            HttpStatusCode.Unauthorized,
            CodigosErro.NaoAutenticado
        );
    }

    [Fact]
    public async Task Pedidos_simultaneos_com_inventario_quase_cheio_nao_passam_de_quarenta()
    {
        var cliente = await ClienteComFalso();
        var id = await CriarPersonagem(cliente);
        await Encher(cliente, id, RegrasItens.EspacosInventario - 2);
        servidores.Sorteador.Resultado = new[] { Espada, Escudo };

        var respostas = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Abrir(cliente, id)));

        Assert.Equal(1, respostas.Count(r => r.StatusCode == HttpStatusCode.OK));
        foreach (var recusada in respostas.Where(r => r.StatusCode != HttpStatusCode.OK))
        {
            await AssertErro(recusada, HttpStatusCode.Conflict, CodigosErroItens.InventarioCheio);
        }

        var inventario = await Inventario(cliente, id);
        Assert.Equal(RegrasItens.EspacosInventario, inventario.Length);
        Assert.Equal(new[] { Espada, Escudo }, Ids(inventario[^2..]));
    }

    [Fact]
    public async Task Pedidos_simultaneos_que_cabem_gravam_todos()
    {
        var cliente = await ClienteComFalso();
        var id = await CriarPersonagem(cliente);
        servidores.Sorteador.Resultado = new[] { Pocao, Pocao };

        var respostas = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Abrir(cliente, id)));

        Assert.All(respostas, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(10, (await Inventario(cliente, id)).Length);
    }
}
