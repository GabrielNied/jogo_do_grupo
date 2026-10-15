using System.Data;
using System.Net;
using System.Text.Json;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

public class FusaoRotasTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private const long Saldo = 20000;

    private async Task<(HttpClient Cliente, long Id)> Personagem(long coroas = Saldo)
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

    /// <summary>
    /// Martelo com Força <paramref name="forca"/> (fixa), Vitalidade 30 e Espírito 10 na escala
    /// 0 a 100: os dois últimos servem para ver que a fusão não mexe no resto.
    /// </summary>
    private async Task<long> Item(
        long id,
        double forca = 20,
        Action<Equipamento>? configurar = null,
        PosicaoEquipamento posicao = PosicaoEquipamento.Inventario
    )
    {
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var dados = CatalogoEquipamentos.Obter(100);
        dados.Atributos = new[]
        {
            new AtributoItem
            {
                Status = StatusItem.DanoFisico,
                Valor = forca,
                Fixo = true,
            },
            new AtributoItem { Status = StatusItem.DanoDaArma, Valor = 30 },
            new AtributoItem { Status = StatusItem.ChanceCritica, Valor = 10 },
        };
        configurar?.Invoke(dados);
        // Exemplar já na escala 0 a 100 (o de escala antiga é coberto pelos testes de migração).
        dados.VersaoDosAtributos = RegrasTier.VersaoAtual;
        dados.TierOrigem = RegrasTier.TierDe(dados);
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
            RegrasEquipamentos.RotaDe(id) + "/fusao/" + acao,
            pedido ?? new { }
        );

    private static async Task<EquipamentosResponse> Estado(HttpClient cliente, long id) =>
        await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(await Post(cliente, id, "estado"));

    private static PedidoFusao Pedido(
        long principal,
        long material,
        StatusItem status = StatusItem.DanoFisico
    ) =>
        new()
        {
            PrincipalId = principal,
            MaterialId = material,
            Status = status,
        };

    private static async Task<PreviaFusao> Previa(
        HttpClient cliente,
        long id,
        long principal,
        long material,
        StatusItem status = StatusItem.DanoFisico
    )
    {
        var resposta = await Post(cliente, id, "previa", Pedido(principal, material, status));
        resposta.EnsureSuccessStatusCode();
        return (await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta)).Previa;
    }

    private static Task<HttpResponseMessage> Confirmar(
        HttpClient cliente,
        long id,
        PreviaFusao previa
    ) =>
        Post(
            cliente,
            id,
            "confirmar",
            new PedidoFusao
            {
                PrincipalId = previa.PrincipalId,
                MaterialId = previa.MaterialId,
                Status = previa.Status,
                Confirmacao = previa.Confirmacao,
            }
        );

    private static async Task<string> Codigo(HttpResponseMessage resposta) =>
        (await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta)).Codigo;

    private static Equipamento Dados(EquipamentosResponse estado, long instancia) =>
        estado
            .Inventario.Concat(estado.Equipados)
            .Single(e => e.InstanciaId == instancia)
            .Equipamento;

    /// <summary>Tudo do exemplar, menos o valor do atributo e o nível de fusão.</summary>
    private static string SemOQueMuda(Equipamento dados, StatusItem status)
    {
        var copia = JsonSerializer.Deserialize<Equipamento>(
            JsonSerializer.Serialize(dados, ServidorDeTeste.Json),
            ServidorDeTeste.Json
        )!;
        copia.NivelFusao = 0;
        copia.Atributos.Single(a => a.Status == status).Valor = 0;
        return JsonSerializer.Serialize(copia, ServidorDeTeste.Json);
    }

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
        Assert.Equal(20000, (await Estado(cliente, id)).Coroas);
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
        var principal = await Item(id, 20, posicao: PosicaoEquipamento.Arma1);
        var material = await Item(id, 40);
        var antes = Dados(await Estado(cliente, id), principal);

        var previa = await Previa(cliente, id, principal, material);

        Assert.Equal(StatusItem.DanoFisico, previa.Status);
        Assert.Equal((20, 40, 30), (previa.ValorAtual, previa.ValorMaterial, previa.ValorDepois));
        Assert.Equal((0, 1), (previa.NivelAtual, previa.NivelDepois));
        Assert.Equal(100, previa.Custo);
        Assert.Equal((0, 0), (previa.MateriaisDungeon, previa.MateriaisRaid));
        Assert.True(previa.MateriaisSuficientes);
        Assert.Equal(TipoEquipamento.Martelo, previa.Categoria);
        var semMudar = await Estado(cliente, id);
        Assert.Equal(Saldo, semMudar.Coroas);
        Assert.Single(semMudar.Inventario);
        Assert.Equal(20, Dados(semMudar, principal).Atributos[0].Valor);

        (await Confirmar(cliente, id, previa)).EnsureSuccessStatusCode();

        var depois = await Estado(cliente, id);
        Assert.Equal(Saldo - 100, depois.Coroas);
        Assert.Empty(depois.Inventario);
        var item = Assert.Single(depois.Equipados).Equipamento;
        Assert.Equal(30, item.Atributos[0].Valor);
        Assert.Equal(1, item.NivelFusao);
        Assert.Equal(30, item.Atributos[1].Valor);
        Assert.Equal(10, item.Atributos[2].Valor);
        Assert.Equal(
            SemOQueMuda(antes, StatusItem.DanoFisico),
            SemOQueMuda(item, StatusItem.DanoFisico)
        );
        using var escopo = servidor.Services.CreateScope();
        var atributos = await escopo
            .ServiceProvider.GetRequiredService<IAtributosDosEquipamentos>()
            .ObterAsync(id, default);
        Assert.Equal(7.5, atributos.BonusDanoFisico, 6);
    }

    [Theory]
    [InlineData(20, 40, 30)] // material maior
    [InlineData(40, 20, 30)] // material menor: o valor diminui
    [InlineData(20, 20, 20)] // igual: nada muda, mas cobra e sobe o nível
    [InlineData(20, 21, 21)] // diferença de 1
    [InlineData(21, 20, 20)]
    [InlineData(20, 25, 23)] // 22,5 vai na direção do material
    [InlineData(25, 20, 22)]
    [InlineData(0, 100, 50)] // limite mínimo
    [InlineData(100, 0, 50)]
    [InlineData(99, 100, 100)] // limite máximo (teto absoluto)
    [InlineData(100, 100, 100)]
    public async Task FormulaDaMetadeDaDiferencaComSubidaDeNivelEMaterialConsumido(
        int atual,
        int doMaterial,
        int esperado
    )
    {
        var (cliente, id) = await Personagem();
        var principal = await Item(id, atual);
        var material = await Item(id, doMaterial);

        var previa = await Previa(cliente, id, principal, material);
        (await Confirmar(cliente, id, previa)).EnsureSuccessStatusCode();

        var estado = await Estado(cliente, id);
        var item = Dados(estado, principal);
        Assert.Equal(esperado, item.Atributos[0].Valor);
        Assert.Equal(1, item.NivelFusao);
        Assert.Equal(Saldo - 100, estado.Coroas);
        Assert.DoesNotContain(estado.Inventario, e => e.InstanciaId == material);
    }

    [Fact]
    public async Task ValorFracionarioDoLegadoEArredondadoNaConta()
    {
        var (cliente, id) = await Personagem();
        var principal = await Item(id, 20.4);
        var material = await Item(id, 40.2);

        var previa = await Previa(cliente, id, principal, material);

        Assert.Equal((20, 40, 30), (previa.ValorAtual, previa.ValorMaterial, previa.ValorDepois));
    }

    [Fact]
    public async Task TierAtualERecalculadoEOTierDeOrigemFica()
    {
        var (cliente, id) = await Personagem();
        void SoForca(Equipamento e) =>
            e.Atributos = new[]
            {
                new AtributoItem { Status = StatusItem.DanoFisico, Valor = 20 },
            };
        var principal = await Item(id, 20, SoForca);
        var material = await Item(
            id,
            100,
            e =>
                e.Atributos = new[]
                {
                    new AtributoItem { Status = StatusItem.DanoFisico, Valor = 100 },
                }
        );

        var previa = await Previa(cliente, id, principal, material);

        Assert.Equal((TierItem.D, TierItem.B), (previa.TierAntes, previa.TierDepois));
        (await Confirmar(cliente, id, previa)).EnsureSuccessStatusCode();
        var item = Dados(await Estado(cliente, id), principal);
        Assert.Equal(60, item.Atributos[0].Valor);
        Assert.Equal(TierItem.B, RegrasTier.TierDe(item));
        Assert.Equal(TierItem.D, item.TierOrigem);
    }

    [Fact]
    public async Task FusaoPreservaOPoderDoPrincipalENaoUsaOPoderDoMaterial()
    {
        var (cliente, id) = await Personagem();
        var principal = await Item(id, 20, e => e.PoderId = CatalogoDePoderes.PoderPeito);
        var material = await Item(id, 100, e => e.PoderId = CatalogoDePoderes.PoderLuvas);
        var semPoder = await Item(id, 20);

        var previa = await Previa(cliente, id, principal, material);
        (await Confirmar(cliente, id, previa)).EnsureSuccessStatusCode();

        var estado = await Estado(cliente, id);
        var fundido = Dados(estado, principal);
        // DEC-003: o poder do principal fica. DEC-004: o poder não entra no tier.
        Assert.Equal(CatalogoDePoderes.PoderPeito, fundido.PoderId);
        var semOPoder = Dados(estado, semPoder);
        Assert.Equal("", semOPoder.PoderId);
        var copia = JsonSerializer.Deserialize<Equipamento>(
            JsonSerializer.Serialize(fundido, ServidorDeTeste.Json),
            ServidorDeTeste.Json
        )!;
        copia.PoderId = "";
        Assert.Equal(RegrasTier.TierDe(copia), RegrasTier.TierDe(fundido));
    }

    [Fact]
    public async Task FusaoQueDiminuiPodeBaixarOTierAtual()
    {
        var (cliente, id) = await Personagem();
        Action<Equipamento> forca(double valor) =>
            e =>
                e.Atributos = new[]
                {
                    new AtributoItem { Status = StatusItem.DanoFisico, Valor = valor },
                };
        var principal = await Item(id, 100, forca(100));
        var material = await Item(id, 0, forca(0));

        var previa = await Previa(cliente, id, principal, material);
        (await Confirmar(cliente, id, previa)).EnsureSuccessStatusCode();

        var item = Dados(await Estado(cliente, id), principal);
        Assert.Equal(50, item.Atributos[0].Valor);
        Assert.Equal(TierItem.SMais, item.TierOrigem);
        Assert.Equal(TierItem.B, RegrasTier.TierDe(item));
    }

    [Fact]
    public async Task EscolherOutroAtributoMudaSoEle()
    {
        var (cliente, id) = await Personagem();
        var principal = await Item(id, 20);
        var material = await Item(id, 90);

        var previa = await Previa(cliente, id, principal, material, StatusItem.DanoDaArma);
        (await Confirmar(cliente, id, previa)).EnsureSuccessStatusCode();

        var item = Dados(await Estado(cliente, id), principal);
        Assert.Equal(20, item.Atributos[0].Valor); // Força intacta, mesmo com material melhor
        Assert.Equal(30, item.Atributos[1].Valor); // Dano da arma 30 e 30: igual
        Assert.Equal(1, item.NivelFusao);
    }

    [Fact]
    public async Task NivelSeguePeloCustoAteOMaisCinco()
    {
        var (cliente, id) = await Personagem();
        var principal = await Item(id, 20, e => e.NivelFusao = 4);
        var material = await Item(id, 40);

        var previa = await Previa(cliente, id, principal, material);
        Assert.Equal(800, previa.Custo);
        (await Confirmar(cliente, id, previa)).EnsureSuccessStatusCode();

        var estado = await Estado(cliente, id);
        Assert.Equal(5, Dados(estado, principal).NivelFusao);
        Assert.Equal(Saldo - 800, estado.Coroas);
    }

    [Theory]
    [InlineData(5, 1200, 1, 0)]
    [InlineData(7, 2400, 3, 0)]
    [InlineData(8, 3400, 0, 1)]
    [InlineData(9, 4800, 0, 2)]
    public async Task SemMateriaisEspeciaisAPreviaMostraEAConfirmacaoNaoCobraNemConsome(
        int nivel,
        long custo,
        int dungeon,
        int raid
    )
    {
        var (cliente, id) = await Personagem();
        var principal = await Item(id, 20, e => e.NivelFusao = nivel);
        var material = await Item(id, 40);

        var previa = await Previa(cliente, id, principal, material);

        Assert.Equal(
            (custo, dungeon, raid),
            (previa.Custo, previa.MateriaisDungeon, previa.MateriaisRaid)
        );
        Assert.False(previa.MateriaisSuficientes);
        var resposta = await Confirmar(cliente, id, previa);
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal(CodigosErroItens.MateriaisInsuficientes, await Codigo(resposta));
        var estado = await Estado(cliente, id);
        Assert.Equal(Saldo, estado.Coroas);
        Assert.Equal(2, estado.Inventario.Length);
        Assert.Equal(nivel, Dados(estado, principal).NivelFusao);
        Assert.Equal(20, Dados(estado, principal).Atributos[0].Valor);
    }

    [Fact]
    public async Task ItemEmMaisDezERecusado()
    {
        var (cliente, id) = await Personagem();
        var principal = await Item(id, 20, e => e.NivelFusao = 10);
        var material = await Item(id, 40);

        var resposta = await Post(cliente, id, "previa", Pedido(principal, material));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal(CodigosErroItens.NivelMaximo, await Codigo(resposta));
        Assert.Equal(Saldo, (await Estado(cliente, id)).Coroas);
    }

    [Fact]
    public async Task SemCoroasNaoCobraNemConsomeNemAltera()
    {
        var (cliente, id) = await Personagem(99);
        var principal = await Item(id);
        var material = await Item(id, 40);

        var previa = await Previa(cliente, id, principal, material); // a prévia mostra mesmo assim
        var resposta = await Confirmar(cliente, id, previa);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal(CodigosErroItens.CoroasInsuficientes, await Codigo(resposta));
        var estado = await Estado(cliente, id);
        Assert.Equal(99, estado.Coroas);
        Assert.Equal(2, estado.Inventario.Length);
        Assert.All(estado.Inventario, e => Assert.Equal(0, e.Equipamento.NivelFusao));
        Assert.Equal(20, Dados(estado, principal).Atributos[0].Valor);
    }

    [Theory]
    [InlineData("categoria", CodigosErroItens.MaterialIncompativel)]
    [InlineData("favorito", CodigosErroItens.ItemFavorito)]
    [InlineData("equipado", CodigosErroItens.MaterialEquipado)]
    [InlineData("atributo", CodigosErroItens.AtributoInelegivel)]
    [InlineData("semFaixa", CodigosErroItens.AtributoInelegivel)]
    public async Task PreviaRecusaCombinacoesProtegidas(string caso, string codigo)
    {
        var (cliente, id) = await Personagem();
        var principal = await Item(
            id,
            20,
            e =>
            {
                if (caso == "semFaixa")
                    e.Atributos = new[]
                    {
                        new AtributoItem { Status = StatusItem.PotenciaFogo, Valor = 5 },
                    };
            }
        );
        var material = await Item(
            id,
            40,
            e =>
            {
                if (caso == "categoria")
                    e.Tipo = TipoEquipamento.Espada;
                if (caso == "favorito")
                    e.Favorito = true;
                if (caso == "atributo")
                    e.Atributos = new[]
                    {
                        new AtributoItem { Status = StatusItem.Destreza, Valor = 40 },
                    };
                if (caso == "semFaixa")
                    e.Atributos = new[]
                    {
                        new AtributoItem { Status = StatusItem.PotenciaFogo, Valor = 5 },
                    };
            },
            caso == "equipado" ? PosicaoEquipamento.Arma2 : PosicaoEquipamento.Inventario
        );

        var status = caso == "semFaixa" ? StatusItem.PotenciaFogo : StatusItem.DanoFisico;
        var resposta = await Post(cliente, id, "previa", Pedido(principal, material, status));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal(codigo, await Codigo(resposta));
        Assert.Equal(Saldo, (await Estado(cliente, id)).Coroas);
    }

    [Fact]
    public async Task PrincipalEquipadoOuFavoritoPodeSerFundido()
    {
        var (cliente, id) = await Personagem();
        var principal = await Item(id, 20, e => e.Favorito = true, PosicaoEquipamento.Arma1);
        var material = await Item(id, 40);

        var previa = await Previa(cliente, id, principal, material);
        (await Confirmar(cliente, id, previa)).EnsureSuccessStatusCode();

        var item = Assert.Single((await Estado(cliente, id)).Equipados).Equipamento;
        Assert.Equal(30, item.Atributos[0].Valor);
        Assert.True(item.Favorito);
    }

    [Fact]
    public async Task PreviaFalsaOuDesatualizadaRecusadaESemAutoconsumo()
    {
        var (cliente, id) = await Personagem();
        var principal = await Item(id);
        var material = await Item(id, 40);
        var mesmo = await Post(cliente, id, "previa", Pedido(principal, principal));
        Assert.Equal(HttpStatusCode.BadRequest, mesmo.StatusCode);
        var previa = await Previa(cliente, id, principal, material);
        var assinatura = previa.Confirmacao;
        previa.Confirmacao = "inventada";
        Assert.Equal(HttpStatusCode.Conflict, (await Confirmar(cliente, id, previa)).StatusCode);
        // Trocar o atributo da prévia não aproveita a assinatura de outro.
        previa.Confirmacao = assinatura;
        previa.Status = StatusItem.DanoDaArma;
        var outroAtributo = await Confirmar(cliente, id, previa);
        Assert.Equal(CodigosErroItens.PreviaDesatualizada, await Codigo(outroAtributo));
        previa.Status = StatusItem.DanoFisico;
        // O principal mudou depois da prévia (favorito): outra prévia.
        (
            await Post(
                cliente,
                id,
                "favorito",
                new PedidoFavorito { InstanciaId = principal, Favorito = true }
            )
        ).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await Confirmar(cliente, id, previa)).StatusCode);
        Assert.Equal(Saldo, (await Estado(cliente, id)).Coroas);
        var nova = await Previa(cliente, id, principal, material);
        (await Confirmar(cliente, id, nova)).EnsureSuccessStatusCode();
        Assert.True(Dados(await Estado(cliente, id), principal).Favorito);
    }

    [Fact]
    public async Task ConfirmacaoRepetidaNaoAplicaDuasVezes()
    {
        var (cliente, id) = await Personagem();
        var principal = await Item(id);
        var material = await Item(id, 40);
        var previa = await Previa(cliente, id, principal, material);

        (await Confirmar(cliente, id, previa)).EnsureSuccessStatusCode();
        var repetida = await Confirmar(cliente, id, previa);

        Assert.Equal(HttpStatusCode.NotFound, repetida.StatusCode);
        var estado = await Estado(cliente, id);
        Assert.Equal(Saldo - 100, estado.Coroas);
        var item = Dados(estado, principal);
        Assert.Equal(1, item.NivelFusao);
        Assert.Equal(30, item.Atributos[0].Valor);
    }

    [Fact]
    public async Task ContaAlheiaItemAlheioInexistenteESemTokenRecusados()
    {
        var (cliente, id) = await Personagem();
        var (outro, outroId) = await Personagem();
        var principal = await Item(id);
        var alheio = await Item(outroId, 40);
        Assert.Equal(HttpStatusCode.NotFound, (await Post(outro, id, "estado")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await Post(cliente, id, "previa", Pedido(principal, alheio))).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await Post(cliente, id, "previa", Pedido(alheio, principal))).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await Post(cliente, id, "previa", Pedido(principal, 999999))).StatusCode
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
        Assert.Equal(Saldo, (await Estado(cliente, id)).Coroas);
    }

    [Fact]
    public async Task MesmoMaterialConcorrenteSoConsomeECobraUmaVez()
    {
        var (cliente, id) = await Personagem();
        var principal1 = await Item(id);
        var principal2 = await Item(id);
        var material = await Item(id, 40);
        var previa1 = await Previa(cliente, id, principal1, material);
        var previa2 = await Previa(cliente, id, principal2, material);

        var respostas = await Task.WhenAll(
            Confirmar(cliente, id, previa1),
            Confirmar(cliente, id, previa2)
        );

        Assert.Single(respostas, r => r.IsSuccessStatusCode);
        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.NotFound);
        var estado = await Estado(cliente, id);
        Assert.Equal(Saldo - 100, estado.Coroas);
        Assert.Equal(2, estado.Inventario.Length);
        Assert.Equal(1, estado.Inventario.Sum(e => e.Equipamento.NivelFusao));
    }

    [Fact]
    public async Task DoisPedidosIguaisConcorrentesAplicamUmaVez()
    {
        var (cliente, id) = await Personagem();
        var principal = await Item(id);
        var material = await Item(id, 40);
        var previa = await Previa(cliente, id, principal, material);

        var respostas = await Task.WhenAll(
            Confirmar(cliente, id, previa),
            Confirmar(cliente, id, previa)
        );

        Assert.Single(respostas, r => r.IsSuccessStatusCode);
        var estado = await Estado(cliente, id);
        Assert.Equal(Saldo - 100, estado.Coroas);
        Assert.Equal(1, Dados(estado, principal).NivelFusao);
    }

    [Fact]
    public async Task FalhaDePersistenciaNaoCobraNemConsomeNemAltera()
    {
        var (cliente, id) = await Personagem();
        var principal = await Item(id);
        var material = await Item(id, 40);
        var previa = await Previa(cliente, id, principal, material);
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var nomeTrigger = "FalhaFusao" + material;
        await db.Database.ExecuteSqlRawAsync(
            $"CREATE TRIGGER {nomeTrigger} BEFORE DELETE ON ItensInventario WHEN OLD.Id = {material} BEGIN SELECT RAISE(ABORT, 'falha simulada'); END;"
        );
        try
        {
            Assert.Equal(
                HttpStatusCode.InternalServerError,
                (await Confirmar(cliente, id, previa)).StatusCode
            );
            var estado = await Estado(cliente, id);
            Assert.Equal(Saldo, estado.Coroas);
            Assert.Equal(2, estado.Inventario.Length);
            Assert.All(estado.Inventario, e => Assert.Equal(0, e.Equipamento.NivelFusao));
            Assert.Equal(20, Dados(estado, principal).Atributos[0].Valor);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync($"DROP TRIGGER {nomeTrigger};");
        }
    }

    [Fact]
    public async Task ComMateriaisSuficientesFusaoEmNiveisSuperioresConsomeCoroasEEssenciasEAvancaNivel()
    {
        var (cliente, id) = await Personagem();
        var principal = await Item(id, 20, e => e.NivelFusao = 5);
        var material = await Item(id, 40);

        var creditou = await Post(cliente, id, "materiais-teste", null);
        Assert.Equal(HttpStatusCode.OK, creditou.StatusCode);
        var estadoComCredito = await Estado(cliente, id);
        Assert.Equal(10, estadoComCredito.MateriaisDungeon);
        Assert.Equal(10, estadoComCredito.MateriaisRaid);

        var previa = await Previa(cliente, id, principal, material);
        Assert.Equal(1200, previa.Custo);
        Assert.Equal(1, previa.MateriaisDungeon);
        Assert.Equal(0, previa.MateriaisRaid);
        Assert.True(previa.MateriaisSuficientes);

        var resposta = await Confirmar(cliente, id, previa);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var estadoFinal = await Estado(cliente, id);
        Assert.Equal(Saldo - 1200, estadoFinal.Coroas);
        Assert.Equal(9, estadoFinal.MateriaisDungeon);
        Assert.Equal(10, estadoFinal.MateriaisRaid);
        Assert.Equal(6, Dados(estadoFinal, principal).NivelFusao);
    }

    [Fact]
    public async Task FusaoRaidNivelOitoParaNoveConsomeEssenciaDeRaid()
    {
        var (cliente, id) = await Personagem();
        var principal = await Item(id, 20, e => e.NivelFusao = 8);
        var material = await Item(id, 40);

        await Post(cliente, id, "materiais-teste", null);
        var previa = await Previa(cliente, id, principal, material);
        Assert.Equal(3400, previa.Custo);
        Assert.Equal(0, previa.MateriaisDungeon);
        Assert.Equal(1, previa.MateriaisRaid);
        Assert.True(previa.MateriaisSuficientes);

        var resposta = await Confirmar(cliente, id, previa);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var estadoFinal = await Estado(cliente, id);
        Assert.Equal(Saldo - 3400, estadoFinal.Coroas);
        Assert.Equal(10, estadoFinal.MateriaisDungeon);
        Assert.Equal(9, estadoFinal.MateriaisRaid);
        Assert.Equal(9, Dados(estadoFinal, principal).NivelFusao);
    }

    [Fact]
    public async Task ConcorrenciaDebitoDeMateriaisNaoDuplicaNemFicaNegativo()
    {
        var (_, id) = await Personagem();
        using var escopo = servidor.Services.CreateScope();
        var materiais = escopo.ServiceProvider.GetRequiredService<ISaldosMateriaisFusao>();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();

        await using (var t = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            await materiais.CreditarDungeonAsync(id, 1, CancellationToken.None);
            await t.CommitAsync();
        }

        var tarefa1 = Task.Run(async () =>
        {
            using var s1 = servidor.Services.CreateScope();
            var m1 = s1.ServiceProvider.GetRequiredService<ISaldosMateriaisFusao>();
            var db1 = s1.ServiceProvider.GetRequiredService<JogoDbContext>();
            await using var t1 = await db1.Database.BeginTransactionAsync(
                IsolationLevel.Serializable
            );
            var ok = await m1.DebitarAsync(id, 1, 0, CancellationToken.None);
            if (ok)
                await t1.CommitAsync();
            return ok;
        });

        var tarefa2 = Task.Run(async () =>
        {
            using var s2 = servidor.Services.CreateScope();
            var m2 = s2.ServiceProvider.GetRequiredService<ISaldosMateriaisFusao>();
            var db2 = s2.ServiceProvider.GetRequiredService<JogoDbContext>();
            await using var t2 = await db2.Database.BeginTransactionAsync(
                IsolationLevel.Serializable
            );
            var ok = await m2.DebitarAsync(id, 1, 0, CancellationToken.None);
            if (ok)
                await t2.CommitAsync();
            return ok;
        });

        var resultados = await Task.WhenAll(tarefa1, tarefa2);
        Assert.Single(resultados, r => r);
        Assert.Single(resultados, r => !r);

        var (saldoDungeon, _) = await materiais.SaldoAsync(id, CancellationToken.None);
        Assert.Equal(0, saldoDungeon);
    }
}
