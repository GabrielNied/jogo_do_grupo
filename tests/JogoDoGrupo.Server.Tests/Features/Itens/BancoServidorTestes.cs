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
using PersonagemEntidade = JogoDoGrupo.Server.Features.Personagem.Personagem;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

public class BancoServidorTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private async Task<(HttpClient Cliente, long Id)> CriarPersonagemAsync(
        long coroas = 0,
        HttpClient? clienteExistente = null
    )
    {
        var cliente = clienteExistente ?? await servidor.CriarClienteLogadoAsync();
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

    private async Task<long> InserirItemBolsaAsync(long personagemId, int itemId = 100)
    {
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var item = new ItemInventario
        {
            PersonagemId = personagemId,
            ItemId = itemId,
            Posicao = PosicaoEquipamento.Inventario,
            Conteiner = ConteinerItem.Bolsa,
            Aba = 0,
            Casa = null,
        };
        db.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    [Fact]
    public async Task Banco_novo_inicia_com_uma_aba_e_quarenta_casas_e_vazio()
    {
        var (cliente, id) = await CriarPersonagemAsync();
        var resposta = await cliente.GetAsync(RotasItens.BancoDe(id));
        resposta.EnsureSuccessStatusCode();
        var banco = await ServidorDeTeste.LerJsonAsync<BancoResponse>(resposta);

        Assert.NotNull(banco);
        Assert.Equal(1, banco.Capacidade.Abas);
        Assert.Equal(40, banco.Capacidade.CasasPorAba);
        Assert.Equal(40, banco.Capacidade.Total);
        Assert.Empty(banco.Itens);
        Assert.NotNull(banco.Bolsa);
    }

    [Fact]
    public async Task Depositar_sem_destino_move_item_da_bolsa_para_o_banco()
    {
        var (cliente, id) = await CriarPersonagemAsync();
        var itemId = await InserirItemBolsaAsync(id, 100);

        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.BancoDepositarDe(id),
            new PedidoDepositarBanco { InstanciaId = itemId, TemDestino = false }
        );
        resposta.EnsureSuccessStatusCode();
        var banco = await ServidorDeTeste.LerJsonAsync<BancoResponse>(resposta);

        Assert.NotNull(banco);
        var itemBanco = Assert.Single(banco.Itens);
        Assert.Equal(100, itemBanco.ItemId);
        Assert.Equal(ConteinerItem.Banco, itemBanco.Conteiner);
        Assert.Equal(0, itemBanco.Aba);
        Assert.Equal(0, itemBanco.Casa);
        Assert.Empty(banco.Bolsa.Inventario);
    }

    [Fact]
    public async Task Retirar_sem_destino_move_item_do_banco_para_a_bolsa()
    {
        var (cliente, id) = await CriarPersonagemAsync();
        var itemId = await InserirItemBolsaAsync(id, 100);

        var depResp = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.BancoDepositarDe(id),
            new PedidoDepositarBanco { InstanciaId = itemId, TemDestino = false }
        );
        depResp.EnsureSuccessStatusCode();
        var aposDep = await ServidorDeTeste.LerJsonAsync<BancoResponse>(depResp);
        var idBanco = Assert.Single(aposDep.Itens).InstanciaId;

        var retResp = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.BancoRetirarDe(id),
            new PedidoRetirarBanco { InstanciaId = idBanco, TemDestino = false }
        );
        retResp.EnsureSuccessStatusCode();
        var banco = await ServidorDeTeste.LerJsonAsync<BancoResponse>(retResp);

        Assert.Empty(banco.Itens);
        var itemBolsa = Assert.Single(banco.Bolsa.Inventario);
        Assert.Equal(100, itemBolsa.ItemId);
        Assert.Equal(ConteinerItem.Bolsa, itemBolsa.Conteiner);
    }

    [Fact]
    public async Task Banco_e_compartilhado_entre_personagens_da_mesma_conta()
    {
        var (cliente, id1) = await CriarPersonagemAsync();
        var (_, id2) = await CriarPersonagemAsync(clienteExistente: cliente);

        var itemId = await InserirItemBolsaAsync(id1, 105);

        // Personagem 1 deposita
        var depResp = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.BancoDepositarDe(id1),
            new PedidoDepositarBanco { InstanciaId = itemId, TemDestino = false }
        );
        depResp.EnsureSuccessStatusCode();

        // Personagem 2 consulta o banco
        var getResp = await cliente.GetAsync(RotasItens.BancoDe(id2));
        getResp.EnsureSuccessStatusCode();
        var bancoP2 = await ServidorDeTeste.LerJsonAsync<BancoResponse>(getResp);

        var itemBanco = Assert.Single(bancoP2.Itens);
        Assert.Equal(105, itemBanco.ItemId);

        // Personagem 2 retira para sua bolsa
        var retResp = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.BancoRetirarDe(id2),
            new PedidoRetirarBanco { InstanciaId = itemBanco.InstanciaId, TemDestino = false }
        );
        retResp.EnsureSuccessStatusCode();
        var bancoAposRetirada = await ServidorDeTeste.LerJsonAsync<BancoResponse>(retResp);

        Assert.Empty(bancoAposRetirada.Itens);
        Assert.Single(bancoAposRetirada.Bolsa.Inventario);

        // Personagem 1 consulta o banco e vê que está vazio
        var getP1 = await cliente.GetAsync(RotasItens.BancoDe(id1));
        getP1.EnsureSuccessStatusCode();
        var bancoP1 = await ServidorDeTeste.LerJsonAsync<BancoResponse>(getP1);
        Assert.Empty(bancoP1.Itens);
    }

    [Fact]
    public async Task Contas_diferentes_sao_estritamente_isoladas()
    {
        var (clienteA, idA) = await CriarPersonagemAsync();
        var (clienteB, idB) = await CriarPersonagemAsync();

        var itemIdA = await InserirItemBolsaAsync(idA, 100);

        var depResp = await ServidorDeTeste.PostJsonAsync(
            clienteA,
            RotasItens.BancoDepositarDe(idA),
            new PedidoDepositarBanco { InstanciaId = itemIdA, TemDestino = false }
        );
        depResp.EnsureSuccessStatusCode();
        var idNoBanco = Assert
            .Single((await ServidorDeTeste.LerJsonAsync<BancoResponse>(depResp)).Itens)
            .InstanciaId;

        // Cliente B vê banco vazio
        var getB = await clienteB.GetAsync(RotasItens.BancoDe(idB));
        getB.EnsureSuccessStatusCode();
        var bancoB = await ServidorDeTeste.LerJsonAsync<BancoResponse>(getB);
        Assert.Empty(bancoB.Itens);

        // Cliente B tenta retirar item da conta A
        var retB = await ServidorDeTeste.PostJsonAsync(
            clienteB,
            RotasItens.BancoRetirarDe(idB),
            new PedidoRetirarBanco { InstanciaId = idNoBanco, TemDestino = false }
        );
        Assert.Equal(HttpStatusCode.NotFound, retB.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(retB);
        Assert.Equal(CodigosErroItens.ItemNaoEncontrado, erro.Codigo);

        // Cliente B tenta acessar rota com ID do personagem A
        var crossGet = await clienteB.GetAsync(RotasItens.BancoDe(idA));
        Assert.Equal(HttpStatusCode.NotFound, crossGet.StatusCode);
    }

    [Fact]
    public async Task Depositar_com_destino_e_mover_no_banco()
    {
        var (cliente, id) = await CriarPersonagemAsync();
        var itemId = await InserirItemBolsaAsync(id, 100);

        // Deposita na aba 0 casa 5
        var depResp = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.BancoDepositarDe(id),
            new PedidoDepositarBanco
            {
                InstanciaId = itemId,
                TemDestino = true,
                Aba = 0,
                Casa = 5,
            }
        );
        depResp.EnsureSuccessStatusCode();
        var banco = await ServidorDeTeste.LerJsonAsync<BancoResponse>(depResp);
        var itemBanco = Assert.Single(banco.Itens);
        Assert.Equal(0, itemBanco.Aba);
        Assert.Equal(5, itemBanco.Casa);

        // Move dentro do banco para aba 0 casa 12
        var movResp = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.BancoMoverDe(id),
            new PedidoMoverBanco
            {
                InstanciaId = itemBanco.InstanciaId,
                Aba = 0,
                Casa = 12,
            }
        );
        movResp.EnsureSuccessStatusCode();
        var aposMov = await ServidorDeTeste.LerJsonAsync<BancoResponse>(movResp);
        var itemMovido = Assert.Single(aposMov.Itens);
        Assert.Equal(0, itemMovido.Aba);
        Assert.Equal(12, itemMovido.Casa);
    }

    [Fact]
    public async Task Depositar_recusa_item_equipado()
    {
        var (cliente, id) = await CriarPersonagemAsync();
        var itemId = await InserirItemBolsaAsync(id, 100);

        // Equipa
        var eqResp = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RegrasEquipamentos.RotaDe(id) + "/equipar",
            new PedidoEquipar { InstanciaId = itemId, Posicao = PosicaoEquipamento.Arma1 }
        );
        eqResp.EnsureSuccessStatusCode();

        // Tenta depositar item equipado
        var depResp = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.BancoDepositarDe(id),
            new PedidoDepositarBanco { InstanciaId = itemId, TemDestino = false }
        );
        Assert.Equal(HttpStatusCode.Conflict, depResp.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(depResp);
        Assert.Equal(CodigosErroItens.ItemEquipado, erro.Codigo);
    }

    [Fact]
    public async Task Depositar_recusa_quando_banco_esta_cheio()
    {
        var (cliente, id) = await CriarPersonagemAsync();

        // Insere 40 itens direto no banco para lotar a primeira aba
        using (var escopo = servidor.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var contaId = await db.Set<PersonagemEntidade>()
                .Where(p => p.Id == id)
                .Select(p => p.ContaId)
                .SingleAsync();
            for (var c = 0; c < RegrasBanco.CasasPorAbaBanco; c++)
            {
                db.Add(
                    new ItemBanco
                    {
                        ContaId = contaId,
                        ItemId = 100,
                        Aba = 0,
                        Casa = c,
                    }
                );
            }
            await db.SaveChangesAsync();
        }

        var itemExtra = await InserirItemBolsaAsync(id, 101);
        var depResp = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.BancoDepositarDe(id),
            new PedidoDepositarBanco { InstanciaId = itemExtra, TemDestino = false }
        );
        Assert.Equal(HttpStatusCode.Conflict, depResp.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(depResp);
        Assert.Equal(CodigosErroItens.BancoCheio, erro.Codigo);
    }

    [Fact]
    public async Task Retirar_recusa_quando_bolsa_esta_cheia()
    {
        var (cliente, id) = await CriarPersonagemAsync();

        // Insere 40 itens na bolsa
        for (var c = 0; c < RegrasItens.CasasPorAbaBolsa; c++)
        {
            await InserirItemBolsaAsync(id, 100);
        }

        // Insere 1 item no banco
        long itemBancoId;
        using (var escopo = servidor.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var contaId = await db.Set<PersonagemEntidade>()
                .Where(p => p.Id == id)
                .Select(p => p.ContaId)
                .SingleAsync();
            var itemBanco = new ItemBanco
            {
                ContaId = contaId,
                ItemId = 101,
                Aba = 0,
                Casa = 0,
            };
            db.Add(itemBanco);
            await db.SaveChangesAsync();
            itemBancoId = itemBanco.Id;
        }

        var retResp = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasItens.BancoRetirarDe(id),
            new PedidoRetirarBanco { InstanciaId = itemBancoId, TemDestino = false }
        );
        Assert.Equal(HttpStatusCode.Conflict, retResp.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(retResp);
        Assert.Equal(CodigosErroItens.InventarioCheio, erro.Codigo);
    }

    [Fact]
    public async Task Expandir_banco_com_saldo_suficiente_debita_coroas_e_aumenta_capacidade()
    {
        var (cliente, id) = await CriarPersonagemAsync(coroas: 1000);

        var resposta = await ServidorDeTeste.PostJsonAsync<object?>(
            cliente,
            RotasItens.BancoExpandirDe(id),
            null
        );
        resposta.EnsureSuccessStatusCode();
        var banco = await ServidorDeTeste.LerJsonAsync<BancoResponse>(resposta);

        Assert.NotNull(banco);
        Assert.Equal(2, banco.Capacidade.Abas);
        Assert.Equal(40, banco.Capacidade.CasasPorAba);
        Assert.Equal(80, banco.Capacidade.Total);
        Assert.Equal(500, banco.Coroas); // 1000 - 500 = 500
    }

    [Fact]
    public async Task Expandir_banco_sem_saldo_retorna_409_CoroasInsuficientes()
    {
        var (cliente, id) = await CriarPersonagemAsync(coroas: 200);

        var resposta = await ServidorDeTeste.PostJsonAsync<object?>(
            cliente,
            RotasItens.BancoExpandirDe(id),
            null
        );
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta);
        Assert.Equal(CodigosErroItens.CoroasInsuficientes, erro.Codigo);
    }
}
