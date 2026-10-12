using System.Data;
using System.Net;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

public class InfusaoRotasTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private async Task<(HttpClient Cliente, long Id)> Personagem(long coroas = 1000)
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;
        if (coroas > 0)
        {
            using var escopo = servidor.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            await using var transacao = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable
            );
            await escopo
                .ServiceProvider.GetRequiredService<ICarteiraCoroas>()
                .CreditarAsync(id, coroas, default);
            await transacao.CommitAsync();
        }
        return (cliente, id);
    }

    private async Task<long> Item(
        long id,
        Action<Equipamento>? configurar = null,
        PosicaoEquipamento posicao = PosicaoEquipamento.Inventario
    )
    {
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var dados = CatalogoEquipamentos.Obter(100);
        dados.ValorBaseInfusivel = 20;
        configurar?.Invoke(dados);
        var entrada = new ItemInventario
        {
            PersonagemId = id,
            ItemId = 100,
            Posicao = posicao,
        };
        entrada.GravarEquipamento(dados);
        db.Add(entrada);
        await db.SaveChangesAsync();
        return entrada.Id;
    }

    private static Task<HttpResponseMessage> Post(
        HttpClient cliente,
        long id,
        string acao,
        object? pedido = null
    ) =>
        ServidorDeTeste.PostJsonAsync(
            cliente,
            RegrasEquipamentos.RotaDe(id) + "/infusao/" + acao,
            pedido ?? new { }
        );

    private static async Task<EquipamentosResponse> Estado(HttpClient cliente, long id) =>
        await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(await Post(cliente, id, "estado"));

    private static async Task<PreviaInfusao> Previa(
        HttpClient cliente,
        long id,
        long receptor,
        long doador
    )
    {
        var resposta = await Post(
            cliente,
            id,
            "previa",
            new PedidoInfusao { ReceptorId = receptor, DoadorId = doador }
        );
        resposta.EnsureSuccessStatusCode();
        return (await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta)).Previa;
    }

    private static Task<HttpResponseMessage> Confirmar(
        HttpClient cliente,
        long id,
        PreviaInfusao previa
    ) =>
        Post(
            cliente,
            id,
            "confirmar",
            new PedidoInfusao
            {
                ReceptorId = previa.ReceptorId,
                DoadorId = previa.DoadorId,
                Confirmacao = previa.Confirmacao,
            }
        );

    [Fact]
    public async Task ProducaoNaoExponhaCreditoDeTeste()
    {
        using var producao = servidor.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.UseSetting("Jwt:Chave", new string('x', 40));
        });
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await Post(producao.CreateClient(), 1, "coroas-teste")).StatusCode
        );
    }

    [Fact]
    public async Task CreditoDeTesteESaldoIsoladoPorPersonagem()
    {
        var (cliente, id) = await Personagem(0);
        var (outro, outroId) = await Personagem(0);
        Assert.Equal(0, (await Estado(cliente, id)).Coroas);
        (await Post(cliente, id, "coroas-teste")).EnsureSuccessStatusCode();
        Assert.Equal(1000, (await Estado(cliente, id)).Coroas);
        Assert.Equal(0, (await Estado(outro, outroId)).Coroas);
        using var escopo = servidor.Services.CreateScope();
        var carteira = escopo.ServiceProvider.GetRequiredService<ICarteiraCoroas>();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            carteira.DebitarAsync(id, 1, default)
        );
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            carteira.CreditarAsync(id, -1, default)
        );
    }

    [Fact]
    public async Task PreviaNaoMudaEstadoEConfirmacaoConsomeCobraEPersisteUmaVez()
    {
        var (cliente, id) = await Personagem();
        var receptor = await Item(id, e => e.ValorBaseInfusivel = 0, PosicaoEquipamento.Arma1);
        var doador = await Item(id);
        var previa = await Previa(cliente, id, receptor, doador);
        Assert.Equal(20.4, previa.Depois);
        Assert.Equal(10, previa.Custo);
        var antes = await Estado(cliente, id);
        Assert.Equal(1000, antes.Coroas);
        Assert.Single(antes.Inventario);
        Assert.Equal(20, Assert.Single(antes.Equipados).Equipamento.Atributos[0].Valor);
        (await Confirmar(cliente, id, previa)).EnsureSuccessStatusCode();
        var depois = await Estado(cliente, id);
        Assert.Equal(990, depois.Coroas);
        Assert.Empty(depois.Inventario);
        var item = Assert.Single(depois.Equipados).Equipamento;
        Assert.Equal(20.4, item.Atributos[0].Valor);
        Assert.Equal(20, item.ValorBaseInfusivel);
        Assert.Equal(1, item.InfusoesUsadas);
        using var escopo = servidor.Services.CreateScope();
        Assert.Equal(
            20.4,
            Assert
                .Single(
                    await escopo
                        .ServiceProvider.GetRequiredService<IBonusEquipamentos>()
                        .ObterAsync(id, default)
                )
                .Valor
        );
        Assert.Equal(HttpStatusCode.NotFound, (await Confirmar(cliente, id, previa)).StatusCode);
        Assert.Equal(990, (await Estado(cliente, id)).Coroas);
    }

    [Fact]
    public async Task UltimoSlotAtingeTetoSemAlterarOutrosAtributos()
    {
        var (cliente, id) = await Personagem();
        var receptor = await Item(
            id,
            e =>
            {
                e.InfusoesUsadas = 9;
                e.Atributos = new[]
                {
                    new AtributoItem
                    {
                        Status = StatusItem.Forca,
                        Valor = 23.6,
                        Fixo = true,
                    },
                    new AtributoItem { Status = StatusItem.Vida, Valor = 30 },
                };
            }
        );
        var doador = await Item(id);
        var restante = await Item(id);
        (
            await Confirmar(cliente, id, await Previa(cliente, id, receptor, doador))
        ).EnsureSuccessStatusCode();
        var item = (await Estado(cliente, id))
            .Inventario.Single(i => i.InstanciaId == receptor)
            .Equipamento;
        Assert.Equal(24, item.Atributos[0].Valor);
        Assert.Equal(30, item.Atributos[1].Valor);
        Assert.Equal(10, item.InfusoesUsadas);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (
                await Post(
                    cliente,
                    id,
                    "previa",
                    new PedidoInfusao { ReceptorId = receptor, DoadorId = restante }
                )
            ).StatusCode
        );
        Assert.Equal(2, (await Estado(cliente, id)).Inventario.Length);
    }

    [Fact]
    public async Task CoroasInsuficientesNaoConsomemDoadorNemAlteramReceptor()
    {
        var (cliente, id) = await Personagem(9);
        var receptor = await Item(id);
        var doador = await Item(id);
        var previa = await Previa(cliente, id, receptor, doador);
        Assert.Equal(HttpStatusCode.Conflict, (await Confirmar(cliente, id, previa)).StatusCode);
        var estado = await Estado(cliente, id);
        Assert.Equal(9, estado.Coroas);
        Assert.Equal(2, estado.Inventario.Length);
        Assert.All(estado.Inventario, e => Assert.Equal(0, e.Equipamento.InfusoesUsadas));
    }

    [Theory]
    [InlineData("tipo")]
    [InlineData("raridade")]
    [InlineData("favorito")]
    [InlineData("equipado")]
    [InlineData("limite")]
    [InlineData("indice")]
    public async Task PreviaRecusaCombinacoesProtegidas(string caso)
    {
        var (cliente, id) = await Personagem();
        var receptor = await Item(
            id,
            e =>
            {
                if (caso == "limite")
                    e.InfusoesUsadas = 10;
                if (caso == "indice")
                    e.IndiceInfusivel = 99;
            }
        );
        var doador = await Item(
            id,
            e =>
            {
                if (caso == "tipo")
                    e.Tipo = TipoEquipamento.Botas;
                if (caso == "raridade")
                    e.Raridade = RaridadeItem.Raro;
                if (caso == "favorito")
                    e.Favorito = true;
            },
            caso == "equipado" ? PosicaoEquipamento.Arma2 : PosicaoEquipamento.Inventario
        );
        Assert.Equal(
            HttpStatusCode.Conflict,
            (
                await Post(
                    cliente,
                    id,
                    "previa",
                    new PedidoInfusao { ReceptorId = receptor, DoadorId = doador }
                )
            ).StatusCode
        );
        Assert.Equal(1000, (await Estado(cliente, id)).Coroas);
    }

    [Fact]
    public async Task PreviaFalsaOuDesatualizadaRecusadaESemAutoconsumo()
    {
        var (cliente, id) = await Personagem();
        var receptor = await Item(id);
        var doador = await Item(id);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (
                await Post(
                    cliente,
                    id,
                    "previa",
                    new PedidoInfusao { ReceptorId = receptor, DoadorId = receptor }
                )
            ).StatusCode
        );
        var previa = await Previa(cliente, id, receptor, doador);
        var assinatura = previa.Confirmacao;
        previa.Confirmacao = "inventada";
        Assert.Equal(HttpStatusCode.Conflict, (await Confirmar(cliente, id, previa)).StatusCode);
        previa.Confirmacao = assinatura;
        (
            await Post(
                cliente,
                id,
                "favorito",
                new PedidoFavorito { InstanciaId = receptor, Favorito = true }
            )
        ).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await Confirmar(cliente, id, previa)).StatusCode);
        Assert.Equal(1000, (await Estado(cliente, id)).Coroas);
        var nova = await Previa(cliente, id, receptor, doador);
        (await Confirmar(cliente, id, nova)).EnsureSuccessStatusCode();
        Assert.True(Assert.Single((await Estado(cliente, id)).Inventario).Equipamento.Favorito);
    }

    [Fact]
    public async Task ContaAlheiaItemAlheioESemTokenRecusados()
    {
        var (cliente, id) = await Personagem();
        var (outro, outroId) = await Personagem();
        var receptor = await Item(id);
        var alheio = await Item(outroId);
        Assert.Equal(HttpStatusCode.NotFound, (await Post(outro, id, "estado")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (
                await Post(
                    cliente,
                    id,
                    "previa",
                    new PedidoInfusao { ReceptorId = receptor, DoadorId = alheio }
                )
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.NotFound,
            (
                await Post(
                    cliente,
                    id,
                    "favorito",
                    new PedidoFavorito { InstanciaId = alheio, Favorito = true }
                )
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await Post(servidor.CreateClient(), id, "estado")).StatusCode
        );
    }

    [Fact]
    public async Task MesmoDoadorConcorrenteSoConsomeECobraUmaVez()
    {
        var (cliente, id) = await Personagem();
        var receptor1 = await Item(id);
        var receptor2 = await Item(id);
        var doador = await Item(id);
        var previa1 = await Previa(cliente, id, receptor1, doador);
        var previa2 = await Previa(cliente, id, receptor2, doador);
        var respostas = await Task.WhenAll(
            Confirmar(cliente, id, previa1),
            Confirmar(cliente, id, previa2)
        );
        Assert.Single(respostas, r => r.IsSuccessStatusCode);
        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.NotFound);
        var estado = await Estado(cliente, id);
        Assert.Equal(990, estado.Coroas);
        Assert.Equal(2, estado.Inventario.Length);
        Assert.Equal(1, estado.Inventario.Sum(e => e.Equipamento.InfusoesUsadas));
    }

    [Fact]
    public async Task FalhaDePersistenciaReverteCobrancaEConsumo()
    {
        var (cliente, id) = await Personagem();
        var receptor = await Item(id);
        var doador = await Item(id);
        var previa = await Previa(cliente, id, receptor, doador);
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var nomeTrigger = "FalhaInfusao" + doador;
        await db.Database.ExecuteSqlRawAsync(
            $"CREATE TRIGGER {nomeTrigger} BEFORE DELETE ON ItensInventario WHEN OLD.Id = {doador} BEGIN SELECT RAISE(ABORT, 'falha simulada'); END;"
        );
        try
        {
            Assert.Equal(
                HttpStatusCode.InternalServerError,
                (await Confirmar(cliente, id, previa)).StatusCode
            );
            var estado = await Estado(cliente, id);
            Assert.Equal(1000, estado.Coroas);
            Assert.Equal(2, estado.Inventario.Length);
            Assert.All(estado.Inventario, e => Assert.Equal(0, e.Equipamento.InfusoesUsadas));
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync($"DROP TRIGGER {nomeTrigger};");
        }
    }
}
