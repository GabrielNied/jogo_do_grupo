using System.Data;
using System.Net;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

public class ExpansaoBolsaTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private async Task<(HttpClient Cliente, long Id)> CriarPersonagemAsync(long coroas = 0)
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

    [Fact]
    public async Task Personagem_novo_inicia_com_uma_aba_e_quarenta_casas()
    {
        var (cliente, id) = await CriarPersonagemAsync();
        var resposta = await cliente.GetAsync(RegrasEquipamentos.RotaDe(id));
        resposta.EnsureSuccessStatusCode();
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta);

        Assert.NotNull(estado);
        Assert.Equal(1, estado.Capacidade.Abas);
        Assert.Equal(40, estado.Capacidade.CasasPorAba);
        Assert.Equal(40, estado.Capacidade.Total);
    }

    [Fact]
    public async Task Expandir_com_saldo_suficiente_debita_coroas_e_aumenta_capacidade()
    {
        var (cliente, id) = await CriarPersonagemAsync(coroas: 200);

        var resposta = await ServidorDeTeste.PostJsonAsync<object?>(
            cliente,
            RotasItens.ExpandirDe(id),
            null
        );
        resposta.EnsureSuccessStatusCode();
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(resposta);

        Assert.NotNull(estado);
        Assert.Equal(2, estado.Capacidade.Abas);
        Assert.Equal(40, estado.Capacidade.CasasPorAba);
        Assert.Equal(80, estado.Capacidade.Total);
        Assert.Equal(100, estado.Coroas); // 200 - 100 de custo para aba 2

        // Confere que no banco persistiu AbasBolsa == 2
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var estadoSalvo = await db.Set<EstadoEquipamentos>().FindAsync(id);
        Assert.NotNull(estadoSalvo);
        Assert.Equal(2, estadoSalvo.AbasBolsa);
    }

    [Fact]
    public async Task Expandir_sem_saldo_retorna_409_CoroasInsuficientes()
    {
        var (cliente, id) = await CriarPersonagemAsync(coroas: 50); // Custo da aba 2 é 100

        var resposta = await ServidorDeTeste.PostJsonAsync<object?>(
            cliente,
            RotasItens.ExpandirDe(id),
            null
        );
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta);
        Assert.Equal(CodigosErroItens.CoroasInsuficientes, erro.Codigo);

        // Capacidade continua inalterada
        var get = await cliente.GetAsync(RegrasEquipamentos.RotaDe(id));
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(get);
        Assert.Equal(1, estado.Capacidade.Abas);
    }

    [Fact]
    public async Task Expandir_alem_do_limite_maximo_retorna_400_BolsaNoLimite()
    {
        // 100 (aba 2) + 250 (aba 3) + 500 (aba 4) = 850
        var (cliente, id) = await CriarPersonagemAsync(coroas: 2000);

        // Expande para aba 2, 3 e 4
        for (var i = 2; i <= ConfigBolsa.AbasMaximas; i++)
        {
            var r = await ServidorDeTeste.PostJsonAsync<object?>(
                cliente,
                RotasItens.ExpandirDe(id),
                null
            );
            r.EnsureSuccessStatusCode();
            var est = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(r);
            Assert.Equal(i, est.Capacidade.Abas);
        }

        // Tentativa de 5a aba
        var quintaAba = await ServidorDeTeste.PostJsonAsync<object?>(
            cliente,
            RotasItens.ExpandirDe(id),
            null
        );
        Assert.Equal(HttpStatusCode.BadRequest, quintaAba.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(quintaAba);
        Assert.Equal(CodigosErroItens.BolsaNoLimite, erro.Codigo);
    }

    [Fact]
    public async Task Mover_item_para_segunda_aba_apos_expansao_funciona_corretamente()
    {
        var (cliente, id) = await CriarPersonagemAsync(coroas: 100);

        // Coloca um item na aba 0
        using (var escopo = servidor.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var item = new ItemInventario
            {
                PersonagemId = id,
                ItemId = CatalogoItens.EspadaDeTeste,
                Posicao = PosicaoEquipamento.Inventario,
                Aba = 0,
                Casa = 0,
            };
            db.Add(item);
            await db.SaveChangesAsync();
        }

        // Tentar mover para aba 1 antes de expandir deve falhar com CasaInvalida (400)
        var moverAntes = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.MoverDe(id),
            new PedidoMover
            {
                InstanciaId = 1,
                Aba = 1,
                Casa = 5,
            }
        );
        Assert.Equal(HttpStatusCode.BadRequest, moverAntes.StatusCode);

        // Expande bolsa
        var expandir = await ServidorDeTeste.PostJsonAsync<object?>(
            cliente,
            RotasItens.ExpandirDe(id),
            null
        );
        expandir.EnsureSuccessStatusCode();

        // Obtém o id real do item criado
        var estado = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(expandir);
        var itemCriado = estado.Inventario[0];

        // Mover para aba 1 casa 5 agora deve funcionar
        var moverDepois = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.MoverDe(id),
            new PedidoMover
            {
                InstanciaId = itemCriado.InstanciaId,
                Aba = 1,
                Casa = 5,
            }
        );
        moverDepois.EnsureSuccessStatusCode();
        var estadoAposMover = await ServidorDeTeste.LerJsonAsync<EquipamentosResponse>(moverDepois);
        var itemMovido = estadoAposMover.Inventario.Single(i =>
            i.InstanciaId == itemCriado.InstanciaId
        );
        Assert.Equal(1, itemMovido.Aba);
        Assert.Equal(5, itemMovido.Casa);
    }
}
