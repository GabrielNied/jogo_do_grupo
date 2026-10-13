using System.Net;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

public class EquipamentosRotasTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    public async Task BauDeTesteGeraGrauEscolhidoEPreservaAoReabrir(int grau, int requisito)
    {
        var (cliente, id) = await Personagem();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RegrasEquipamentos.RotaDe(id) + "/bau-teste?grau=" + grau,
            new { }
        );
        resposta.EnsureSuccessStatusCode();
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta);
        var entrada = Assert.Single(estado.Inventario);
        Assert.Contains(entrada.ItemId, CatalogoEquipamentos.ModelosDeTeste);
        Assert.Equal((GrauEquipamento)grau, entrada.Equipamento.Grau);
        Assert.Equal(requisito, entrada.Equipamento.NivelPersonagemMinimo);
        var volta = Assert.Single((await Ler(cliente, id)).Inventario);
        Assert.Equal(entrada.InstanciaId, volta.InstanciaId);
        Assert.Equal(
            entrada.Equipamento.Atributos.Select(a => a.Valor),
            volta.Equipamento.Atributos.Select(a => a.Valor)
        );
        Assert.Equal(entrada.Equipamento.Familia, volta.Equipamento.Familia);
    }

    [Fact]
    public async Task BauDeTesteRecusaGrauInvalidoSemCriarItem()
    {
        var (cliente, id) = await Personagem();
        foreach (var grau in new[] { -1, 3, 99 })
            Assert.Equal(
                HttpStatusCode.BadRequest,
                (
                    await ServidorDeTeste.PostJsonAsync(
                        cliente,
                        RegrasEquipamentos.RotaDe(id) + "/bau-teste?grau=" + grau,
                        new { }
                    )
                ).StatusCode
            );
        Assert.Empty((await Ler(cliente, id)).Inventario);
    }

    [Fact]
    public async Task LimparInventarioDeTesteApagaBolsaEEquipamentosPreservandoConjuntoAtivo()
    {
        using var fabrica = servidor.WithWebHostBuilder(builder =>
            builder.UseSetting("Desenvolvimento:PermitirComandos", "true")
        );
        var cliente = await ServidorDeTeste.CriarClienteLogadoEmAsync(fabrica);
        var personagem = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(personagem)).Id;
        var equipado = await Item(id, 100);
        var naBolsa = await Item(id, 101);
        (await Equipar(cliente, id, equipado, PosicaoEquipamento.Arma1)).EnsureSuccessStatusCode();
        (
            await ServidorDeTeste.PostJsonAsync(
                cliente,
                RegrasEquipamentos.RotaDe(id) + "/conjunto",
                new PedidoConjunto { Conjunto = 2 }
            )
        ).EnsureSuccessStatusCode();
        var antes = await Ler(cliente, id);
        Assert.Equal(naBolsa, Assert.Single(antes.Inventario).InstanciaId);
        Assert.Equal(equipado, Assert.Single(antes.Equipados).InstanciaId);

        var resposta = await cliente.PostAsync(
            RegrasEquipamentos.RotaDe(id) + "/limpar-teste",
            null
        );
        resposta.EnsureSuccessStatusCode();
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta);

        Assert.Empty(estado.Inventario);
        Assert.Empty(estado.Equipados);
        Assert.Equal(2, estado.ConjuntoAtivo);
        Assert.Empty((await Ler(cliente, id)).Inventario);
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        Assert.Empty(await db.Set<ItemInventario>().Where(i => i.PersonagemId == id).ToListAsync());
    }

    [Fact]
    public async Task LimparInventarioDeTesteRecusaQuandoPoliticaDesligadaOuContaNaoPermitida()
    {
        using var fabricaDesligada = servidor.WithWebHostBuilder(builder =>
            builder.UseSetting("Desenvolvimento:PermitirComandos", "false")
        );
        var cliente = await ServidorDeTeste.CriarClienteLogadoEmAsync(fabricaDesligada);
        var personagem = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(personagem)).Id;
        await Item(id, 101);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await cliente.PostAsync(RegrasEquipamentos.RotaDe(id) + "/limpar-teste", null)
            ).StatusCode
        );

        using var fabrica = servidor.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Desenvolvimento:PermitirComandos", "true");
            builder.UseSetting("Desenvolvimento:Contas:0", "-1");
        });
        var outroCliente = await ServidorDeTeste.CriarClienteLogadoEmAsync(fabrica);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await outroCliente.PostAsync(RegrasEquipamentos.RotaDe(id) + "/limpar-teste", null)
            ).StatusCode
        );
        Assert.Single((await Ler(cliente, id)).Inventario);
    }

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
        PosicaoEquipamento posicao = PosicaoEquipamento.Inventario,
        GrauEquipamento grau = GrauEquipamento.Inicial
    )
    {
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var item = new ItemInventario
        {
            PersonagemId = id,
            ItemId = modelo,
            Posicao = posicao,
        };
        var dados = new GeradorEquipamentos(
            new ConfigEquipamentos { QuantidadeAleatorios = 0 }
        ).Gerar(modelo, new Random(1), grau: grau);
        if (dados != null)
        {
            // Valor conhecido na escala de 0 a 100: 50 fica no meio da conversão linear de cada atributo.
            foreach (var atributo in dados.Atributos)
                atributo.Valor = 50;
            item.GravarEquipamento(dados);
        }
        db.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

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

    private static async Task<EquipamentosResponse> Ler(HttpClient cliente, long id) =>
        await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(
            await cliente.GetAsync(RegrasEquipamentos.RotaDe(id))
        );

    [Fact]
    public async Task Equipar_ReabrirEDesequiparNaoRerrolaNemDuplicaOQueDaAoCombate()
    {
        var (cliente, id) = await Personagem();
        var item = await Item(id, 100);
        (await Equipar(cliente, id, item, PosicaoEquipamento.Arma1)).EnsureSuccessStatusCode();
        (await Equipar(cliente, id, item, PosicaoEquipamento.Arma1)).EnsureSuccessStatusCode();
        var estado = await Ler(cliente, id);
        Assert.Empty(estado.Inventario);
        Assert.Equal(item, Assert.Single(estado.Equipados).InstanciaId);
        // Martelo: dano da arma de 14 a 44 e Força do item de 0 a 25% de dano; 50 é o meio.
        Assert.True(estado.Atributos.TemArma);
        Assert.Equal(29, estado.Atributos.DanoDaArma, 6);
        Assert.Equal(0.125, estado.Atributos.PercentualForca, 6);
        var contrato = await servidor
            .Services.GetRequiredService<IAtributosDosEquipamentos>()
            .ObterAsync(id, default);
        Assert.Equal(29, contrato.DanoDaArma, 6);
        Assert.Equal(0.125, contrato.PercentualForca, 6);
        // A Força do martelo é do item: a Força do personagem continua em 0.
        var recursos = await Recursos(cliente, id);
        Assert.Equal(0, recursos.Atributos.Forca);
        (
            await ServidorDeTeste.PostJsonAsync(
                cliente,
                RegrasEquipamentos.RotaDe(id) + "/desequipar",
                new PedidoDesequipar { Posicao = PosicaoEquipamento.Arma1 }
            )
        ).EnsureSuccessStatusCode();
        estado = await Ler(cliente, id);
        Assert.Empty(estado.Equipados);
        Assert.False(estado.Atributos.TemArma);
        Assert.Empty(estado.Atributos.Linhas);
        Assert.Equal(50, Assert.Single(estado.Inventario).Equipamento.Atributos[0].Valor);
        Assert.Equal(0, (await Recursos(cliente, id)).Atributos.Forca);
    }

    [Fact]
    public async Task EquiparEDesequiparNaoMudaOsAtributosNemOsPontosDoPersonagem()
    {
        var (cliente, id) = await Personagem();
        var antesProgresso = await Progresso(cliente, id);
        var antesRecursos = await Recursos(cliente, id);
        // Cinto: Vigor, Agilidade e Espírito do item. Peito: Força, Vigor, Intelecto, Agilidade
        // (o principal da família) e defesas. Nenhum deles pode somar nos cinco do personagem.
        var cinto = await Item(id, 106); // Cinto do Vigia
        var peito = await Item(id, 102); // Peitoral do Vigia
        var arma = await Item(id, 100);
        (await Equipar(cliente, id, cinto, PosicaoEquipamento.Cinto)).EnsureSuccessStatusCode();
        (await Equipar(cliente, id, peito, PosicaoEquipamento.Peito)).EnsureSuccessStatusCode();
        (await Equipar(cliente, id, arma, PosicaoEquipamento.Arma1)).EnsureSuccessStatusCode();

        var equipado = await Recursos(cliente, id);
        Assert.Equivalent(antesRecursos.Atributos, equipado.Atributos);
        // O que o item dá vem por fora dos cinco: a vida máxima sobe pelo Vigor do item.
        Assert.True(equipado.VidaMaxima > antesRecursos.VidaMaxima);
        Assert.NotEmpty(equipado.DoEquipamento);
        var progressoEquipado = await Progresso(cliente, id);
        Assert.Equivalent(antesProgresso, progressoEquipado);

        foreach (
            var posicao in new[]
            {
                PosicaoEquipamento.Cinto,
                PosicaoEquipamento.Peito,
                PosicaoEquipamento.Arma1,
            }
        )
            (
                await ServidorDeTeste.PostJsonAsync(
                    cliente,
                    RegrasEquipamentos.RotaDe(id) + "/desequipar",
                    new PedidoDesequipar { Posicao = posicao }
                )
            ).EnsureSuccessStatusCode();
        var depois = await Recursos(cliente, id);
        Assert.Equivalent(antesRecursos.Atributos, depois.Atributos);
        Assert.Equal(antesRecursos.VidaMaxima, depois.VidaMaxima);
        Assert.Empty(depois.DoEquipamento);
        Assert.Equivalent(antesProgresso, await Progresso(cliente, id));
    }

    [Fact]
    public async Task TrocaDeArmasESlotsDuplicadosSomamApenasFontesAtivas()
    {
        var (cliente, id) = await Personagem();
        var arma1 = await Item(id, 100);
        var arma2 = await Item(id, 100);
        var anel1 = await Item(id, 107);
        var anel2 = await Item(id, 107);
        (await Equipar(cliente, id, arma1, PosicaoEquipamento.Arma1)).EnsureSuccessStatusCode();
        (await Equipar(cliente, id, arma2, PosicaoEquipamento.Arma2)).EnsureSuccessStatusCode();
        (await Equipar(cliente, id, anel1, PosicaoEquipamento.Anel1)).EnsureSuccessStatusCode();
        (await Equipar(cliente, id, anel2, PosicaoEquipamento.Anel2)).EnsureSuccessStatusCode();
        var troca = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RegrasEquipamentos.RotaDe(id) + "/conjunto",
            new PedidoConjunto { Conjunto = 2 }
        );
        troca.EnsureSuccessStatusCode();
        var estado = await Ler(cliente, id);
        Assert.Equal(2, estado.ConjuntoAtivo);
        // Só a arma do conjunto ativo dá o dano-base e a Força do item (29 e 12,5%, uma vez).
        Assert.Equal(29, estado.Atributos.DanoDaArma, 6);
        Assert.Equal(0.125, estado.Atributos.PercentualForca, 6);
        // Os dois anéis contam: Vigor do anel de 0 a 150 de vida máxima; 50 vale 75 cada.
        Assert.Equal(150, estado.Atributos.VidaMaxima);
    }

    private static async Task<RecursosResponse> Recursos(HttpClient cliente, long id) =>
        await ServidorDeTeste.LerJsonAsync<RecursosResponse>(
            await cliente.GetAsync(RotasCombate.RecursosDe(id))
        );

    private static async Task<ProgressoResponse> Progresso(HttpClient cliente, long id) =>
        await ServidorDeTeste.LerJsonAsync<ProgressoResponse>(
            await cliente.GetAsync(RotasPersonagem.ProgressoDe(id))
        );

    [Fact]
    public async Task RecusaItemDeOutraContaPosicaoInvalidaNivelEConsumivel()
    {
        var (cliente, id) = await Personagem();
        var (outro, outroId) = await Personagem();
        var item = await Item(id, 100);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await Equipar(outro, id, item, PosicaoEquipamento.Arma1)).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await Equipar(outro, outroId, item, PosicaoEquipamento.Arma1)).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await Equipar(cliente, id, item, PosicaoEquipamento.Capacete)).StatusCode
        );
        var alto = await Item(id, 100, grau: GrauEquipamento.Forte);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await Equipar(cliente, id, alto, PosicaoEquipamento.Arma1)).StatusCode
        );
        var pocao = await Item(id, 3);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await Equipar(cliente, id, pocao, PosicaoEquipamento.Arma1)).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await servidor.CreateClient().GetAsync(RegrasEquipamentos.RotaDe(id))).StatusCode
        );
    }

    [Fact]
    public async Task DuasMaosRetiraEscudoMasNaoPerdeItemSeInventarioCheio()
    {
        var (cliente, id) = await Personagem();
        var escudo = await Item(id, 2);
        var martelo = await Item(id, 100);
        (
            await Equipar(cliente, id, escudo, PosicaoEquipamento.Secundaria1)
        ).EnsureSuccessStatusCode();
        (await Equipar(cliente, id, martelo, PosicaoEquipamento.Arma1)).EnsureSuccessStatusCode();
        var estado = await Ler(cliente, id);
        Assert.Equal(escudo, Assert.Single(estado.Inventario).InstanciaId);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await Equipar(cliente, id, escudo, PosicaoEquipamento.Secundaria1)).StatusCode
        );
        for (var i = 0; i < 39; i++)
            await Item(id, 3);
        var tirar = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RegrasEquipamentos.RotaDe(id) + "/desequipar",
            new PedidoDesequipar { Posicao = PosicaoEquipamento.Arma1 }
        );
        Assert.Equal(HttpStatusCode.Conflict, tirar.StatusCode);
        Assert.Equal(martelo, Assert.Single((await Ler(cliente, id)).Equipados).InstanciaId);
    }

    [Fact]
    public async Task EquiparConcorrentePreservaAmbosOsExemplares()
    {
        var (cliente, id) = await Personagem();
        var a = await Item(id, 107);
        var b = await Item(id, 107);
        var respostas = await Task.WhenAll(
            Equipar(cliente, id, a, PosicaoEquipamento.Anel1),
            Equipar(cliente, id, b, PosicaoEquipamento.Anel1)
        );
        Assert.All(respostas, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var estado = await Ler(cliente, id);
        Assert.Single(estado.Equipados);
        Assert.Single(estado.Inventario);
        Assert.Equal(
            new[] { a, b },
            estado.Equipados.Concat(estado.Inventario).Select(i => i.InstanciaId).Order().ToArray()
        );
    }

    private static Task<HttpResponseMessage> Descartar(HttpClient cliente, long id, long item) =>
        ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.DescartarDe(id),
            new PedidoDescartar { InstanciaId = item }
        );

    private static async Task<string> Codigo(HttpResponseMessage resposta) =>
        (await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta)).Codigo;

    [Fact]
    public async Task Descartar_RemoveDaBolsaEDevolveEstadoAtualizado()
    {
        var (cliente, id) = await Personagem();
        var a = await Item(id, 100);
        var b = await Item(id, 107);
        var resposta = await Descartar(cliente, id, a);
        resposta.EnsureSuccessStatusCode();
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta);
        Assert.Equal(b, Assert.Single(estado.Inventario).InstanciaId);
        Assert.Equal(b, Assert.Single((await Ler(cliente, id)).Inventario).InstanciaId);
    }

    [Fact]
    public async Task Descartar_LiberaEspacoDaBolsaCheia()
    {
        var (cliente, id) = await Personagem();
        long ultimo = 0;
        for (var n = 0; n < RegrasItens.EspacosInventario; n++)
            ultimo = await Item(id, 107);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (
                await ServidorDeTeste.PostJsonAsync(
                    cliente,
                    RegrasEquipamentos.RotaDe(id) + "/bau-teste",
                    new { }
                )
            ).StatusCode
        );
        (await Descartar(cliente, id, ultimo)).EnsureSuccessStatusCode();
        Assert.Equal(
            HttpStatusCode.OK,
            (
                await ServidorDeTeste.PostJsonAsync(
                    cliente,
                    RegrasEquipamentos.RotaDe(id) + "/bau-teste",
                    new { }
                )
            ).StatusCode
        );
    }

    [Fact]
    public async Task Descartar_RecusaOutraContaInexistenteEPersonagemAlheio()
    {
        var (cliente, id) = await Personagem();
        var (outro, outroId) = await Personagem();
        var item = await Item(id, 100);
        // Conta alheia no personagem de outro, ou o exemplar alheio no personagem próprio.
        var r1 = await Descartar(outro, id, item);
        Assert.Equal(HttpStatusCode.NotFound, r1.StatusCode);
        Assert.Equal(CodigosErroItens.PersonagemNaoEncontrado, await Codigo(r1));
        var r2 = await Descartar(outro, outroId, item);
        Assert.Equal(HttpStatusCode.NotFound, r2.StatusCode);
        Assert.Equal(CodigosErroItens.ItemNaoEncontrado, await Codigo(r2));
        var r3 = await Descartar(cliente, id, 999_999_999);
        Assert.Equal(CodigosErroItens.ItemNaoEncontrado, await Codigo(r3));
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await Descartar(servidor.CreateClient(), id, item)).StatusCode
        );
        Assert.Single((await Ler(cliente, id)).Inventario);
    }

    [Fact]
    public async Task Descartar_RecusaEquipadoENaoAltera()
    {
        var (cliente, id) = await Personagem();
        var item = await Item(id, 100);
        (await Equipar(cliente, id, item, PosicaoEquipamento.Arma1)).EnsureSuccessStatusCode();
        var resposta = await Descartar(cliente, id, item);
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal(CodigosErroItens.ItemEquipado, await Codigo(resposta));
        Assert.Equal(item, Assert.Single((await Ler(cliente, id)).Equipados).InstanciaId);
    }

    [Fact]
    public async Task Descartar_RepetirNaoQuebraNada()
    {
        var (cliente, id) = await Personagem();
        var item = await Item(id, 100);
        var outro = await Item(id, 107);
        (await Descartar(cliente, id, item)).EnsureSuccessStatusCode();
        var repetido = await Descartar(cliente, id, item);
        Assert.Equal(HttpStatusCode.NotFound, repetido.StatusCode);
        Assert.Equal(CodigosErroItens.ItemNaoEncontrado, await Codigo(repetido));
        Assert.Equal(outro, Assert.Single((await Ler(cliente, id)).Inventario).InstanciaId);
    }

    [Fact]
    public async Task Descartar_SimultaneoDoMesmoExemplarValeUmaVez()
    {
        var (cliente, id) = await Personagem();
        var item = await Item(id, 100);
        var outro = await Item(id, 107);
        var respostas = await Task.WhenAll(
            Descartar(cliente, id, item),
            Descartar(cliente, id, item),
            Descartar(cliente, id, item)
        );
        Assert.Equal(1, respostas.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(2, respostas.Count(r => r.StatusCode == HttpStatusCode.NotFound));
        Assert.Equal(outro, Assert.Single((await Ler(cliente, id)).Inventario).InstanciaId);
    }

    [Fact]
    public async Task Descartar_DuranteEquiparDeixaEstadoConsistente()
    {
        var (cliente, id) = await Personagem();
        var item = await Item(id, 107);
        var respostas = await Task.WhenAll(
            Equipar(cliente, id, item, PosicaoEquipamento.Anel1),
            Descartar(cliente, id, item)
        );
        var estado = await Ler(cliente, id);
        var equipar = respostas[0].StatusCode;
        var descartar = respostas[1].StatusCode;
        // Em série: ou equipou primeiro (descarte 409, peça equipada) ou descartou primeiro
        // (equipar 404, peça sumiu). Nunca as duas coisas, nem peça duplicada.
        if (descartar == HttpStatusCode.OK)
        {
            Assert.Equal(HttpStatusCode.NotFound, equipar);
            Assert.Empty(estado.Inventario);
            Assert.Empty(estado.Equipados);
        }
        else
        {
            Assert.Equal(HttpStatusCode.Conflict, descartar);
            Assert.Equal(HttpStatusCode.OK, equipar);
            Assert.Equal(item, Assert.Single(estado.Equipados).InstanciaId);
            Assert.Empty(estado.Inventario);
        }
    }
}
