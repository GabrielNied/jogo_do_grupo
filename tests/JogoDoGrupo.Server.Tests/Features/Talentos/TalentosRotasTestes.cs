using System.Net;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Features.Talentos;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Talentos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Talentos;

public class TalentosRotasTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private static readonly string Forca1 = ArvoreDeTeste.IdPequeno(AtributoPrincipal.Forca, 0);
    private static readonly string Forca2 = ArvoreDeTeste.IdPequeno(AtributoPrincipal.Forca, 1);
    private static readonly string Vigor1 = ArvoreDeTeste.IdPequeno(AtributoPrincipal.Vigor, 0);

    private async Task<(HttpClient Cliente, long Id)> NovoPersonagemAsync(int pontos)
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;

        await using var escopo = servidor.Services.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var personagem = await db.Set<JogoDoGrupo.Server.Features.Personagem.Personagem>()
            .SingleAsync(p => p.Id == id);
        personagem.PontosLivres = pontos;
        await db.SaveChangesAsync();
        return (cliente, id);
    }

    private static Task<HttpResponseMessage> ComprarAsync(
        HttpClient cliente,
        long id,
        string no,
        int rankAlvo,
        int? versao = null
    ) =>
        ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasTalentos.ComprarDe(id),
            new ComprarNoRequest
            {
                NoId = no,
                RankAlvo = rankAlvo,
                VersaoConfiguracao = versao ?? ArvoreDeTeste.Versao,
            }
        );

    private static async Task<ArvoreResponse> LerArvoreAsync(HttpClient cliente, long id)
    {
        var resposta = await cliente.GetAsync(RotasTalentos.ArvoreDe(id));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await ServidorDeTeste.LerJsonAsync<ArvoreResponse>(resposta);
    }

    private static async Task AssertErroAsync(
        HttpResponseMessage resposta,
        HttpStatusCode status,
        string codigo
    )
    {
        Assert.Equal(status, resposta.StatusCode);
        Assert.Equal(codigo, (await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta)).Codigo);
    }

    private static int RankDe(ArvoreResponse arvore, string no) =>
        arvore.Ranks.FirstOrDefault(r => r.NoId == no)?.Rank ?? 0;

    [Fact]
    public async Task Arvore_nova_traz_a_configuracao_e_nenhum_rank()
    {
        var (cliente, id) = await NovoPersonagemAsync(3);

        var arvore = await LerArvoreAsync(cliente, id);

        Assert.Equal(ArvoreDeTeste.Versao, arvore.Configuracao.Versao);
        Assert.Equal(ArvoreDeTeste.Criar().Nos.Length, arvore.Configuracao.Nos.Length);
        Assert.Empty(arvore.Ranks);
        Assert.Equal(3, arvore.PontosLivres);
        Assert.Equal(0, arvore.PontosGastos);
    }

    [Fact]
    public async Task Compra_valida_cobra_o_custo_e_sobe_o_rank_e_o_atributo()
    {
        var (cliente, id) = await NovoPersonagemAsync(5);

        var resposta = await ComprarAsync(cliente, id, Forca1, 1);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var arvore = await ServidorDeTeste.LerJsonAsync<ArvoreResponse>(resposta);
        Assert.Equal(1, RankDe(arvore, Forca1));
        Assert.Equal(5 - ArvoreDeTeste.CustoPequeno[0], arvore.PontosLivres);
        Assert.Equal(ArvoreDeTeste.CustoPequeno[0], arvore.PontosGastos);
        Assert.Equal(ArvoreDeTeste.ValorPorRankPequeno, arvore.Atributos.Forca);
        // O saldo é o mesmo que o progresso do personagem mostra.
        var progresso = await ServidorDeTeste.LerJsonAsync<ProgressoResponse>(
            await cliente.GetAsync(RotasPersonagem.ProgressoDe(id))
        );
        Assert.Equal(arvore.PontosLivres, progresso.PontosLivres);
    }

    [Fact]
    public async Task Sem_pontos_a_compra_e_recusada_e_nada_muda()
    {
        var (cliente, id) = await NovoPersonagemAsync(0);

        await AssertErroAsync(
            await ComprarAsync(cliente, id, Forca1, 1),
            HttpStatusCode.Conflict,
            CodigosErroTalentos.PontosInsuficientes
        );

        var arvore = await LerArvoreAsync(cliente, id);
        Assert.Empty(arvore.Ranks);
        Assert.Equal(0, arvore.PontosLivres);
    }

    [Fact]
    public async Task Sem_requisito_a_compra_e_recusada_e_nao_cobra()
    {
        var (cliente, id) = await NovoPersonagemAsync(5);

        await AssertErroAsync(
            await ComprarAsync(cliente, id, Forca2, 1),
            HttpStatusCode.Conflict,
            CodigosErroTalentos.RequisitoNaoCumprido
        );

        Assert.Equal(5, (await LerArvoreAsync(cliente, id)).PontosLivres);
    }

    [Fact]
    public async Task Requisito_cumprido_abre_o_proximo_no_e_o_maximo_e_respeitado()
    {
        var (cliente, id) = await NovoPersonagemAsync(20);
        for (var rank = 1; rank <= ArvoreDeTeste.RankMaximoPequeno; rank++)
        {
            (await ComprarAsync(cliente, id, Forca1, rank)).EnsureSuccessStatusCode();
        }

        await AssertErroAsync(
            await ComprarAsync(cliente, id, Forca1, ArvoreDeTeste.RankMaximoPequeno + 1),
            HttpStatusCode.Conflict,
            CodigosErroTalentos.RankMaximo
        );
        var comRequisito = await ComprarAsync(cliente, id, Forca2, 1);

        Assert.Equal(HttpStatusCode.OK, comRequisito.StatusCode);
        var arvore = await LerArvoreAsync(cliente, id);
        Assert.Equal(ArvoreDeTeste.RankMaximoPequeno, RankDe(arvore, Forca1));
        Assert.Equal(1, RankDe(arvore, Forca2));
        Assert.Equal(
            ArvoreDeTeste.CustoPequeno.Sum() + ArvoreDeTeste.CustoPequeno[0],
            arvore.PontosGastos
        );
        Assert.Equal(20 - arvore.PontosGastos, arvore.PontosLivres);
    }

    [Fact]
    public async Task Pedido_repetido_nao_cobra_duas_vezes()
    {
        var (cliente, id) = await NovoPersonagemAsync(5);

        var primeira = await ComprarAsync(cliente, id, Forca1, 1);
        var repetida = await ComprarAsync(cliente, id, Forca1, 1);

        Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repetida.StatusCode);
        var arvore = await LerArvoreAsync(cliente, id);
        Assert.Equal(1, RankDe(arvore, Forca1));
        Assert.Equal(5 - ArvoreDeTeste.CustoPequeno[0], arvore.PontosLivres);
    }

    [Fact]
    public async Task Dois_pedidos_iguais_ao_mesmo_tempo_cobram_uma_vez()
    {
        var (cliente, id) = await NovoPersonagemAsync(5);

        var respostas = await Task.WhenAll(
            ComprarAsync(cliente, id, Forca1, 1),
            ComprarAsync(cliente, id, Forca1, 1),
            ComprarAsync(cliente, id, Forca1, 1)
        );

        Assert.All(respostas, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var arvore = await LerArvoreAsync(cliente, id);
        Assert.Equal(1, RankDe(arvore, Forca1));
        Assert.Equal(5 - ArvoreDeTeste.CustoPequeno[0], arvore.PontosLivres);
        Assert.Equal(ArvoreDeTeste.CustoPequeno[0], arvore.PontosGastos);
    }

    [Fact]
    public async Task Compras_diferentes_ao_mesmo_tempo_nunca_gastam_mais_que_o_saldo()
    {
        // Só dá para pagar uma das duas raízes.
        var (cliente, id) = await NovoPersonagemAsync(ArvoreDeTeste.CustoPequeno[0]);

        var respostas = await Task.WhenAll(
            ComprarAsync(cliente, id, Forca1, 1),
            ComprarAsync(cliente, id, Vigor1, 1)
        );

        Assert.Equal(1, respostas.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, respostas.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        var arvore = await LerArvoreAsync(cliente, id);
        Assert.Single(arvore.Ranks);
        Assert.Equal(0, arvore.PontosLivres);
    }

    [Fact]
    public async Task Pedido_invalido_e_recusado_sem_cobrar()
    {
        var (cliente, id) = await NovoPersonagemAsync(5);

        await AssertErroAsync(
            await ComprarAsync(cliente, id, "nao-existe", 1),
            HttpStatusCode.BadRequest,
            CodigosErroTalentos.NoInvalido
        );
        await AssertErroAsync(
            await ComprarAsync(cliente, id, Forca1, 2),
            HttpStatusCode.BadRequest,
            CodigosErroTalentos.RankAlvoInvalido
        );
        await AssertErroAsync(
            await ComprarAsync(cliente, id, Forca1, 0),
            HttpStatusCode.BadRequest,
            CodigosErroTalentos.RankAlvoInvalido
        );
        await AssertErroAsync(
            await ComprarAsync(cliente, id, Forca1, 1, versao: ArvoreDeTeste.Versao + 1),
            HttpStatusCode.Conflict,
            CodigosErroTalentos.ArvoreDesatualizada
        );

        var arvore = await LerArvoreAsync(cliente, id);
        Assert.Empty(arvore.Ranks);
        Assert.Equal(5, arvore.PontosLivres);
    }

    [Fact]
    public async Task Arvore_salva_em_outra_versao_recusa_a_compra_sem_perder_nada()
    {
        var (cliente, id) = await NovoPersonagemAsync(5);
        (await ComprarAsync(cliente, id, Forca1, 1)).EnsureSuccessStatusCode();
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var cabecalho = await db.Set<ArvoreDoPersonagem>()
                .SingleAsync(a => a.PersonagemId == id);
            cabecalho.Versao = ArvoreDeTeste.Versao + 1;
            await db.SaveChangesAsync();
        }

        await AssertErroAsync(
            await ComprarAsync(cliente, id, Forca1, 2),
            HttpStatusCode.Conflict,
            CodigosErroTalentos.ArvoreDesatualizada
        );

        var arvore = await LerArvoreAsync(cliente, id);
        Assert.Equal(1, RankDe(arvore, Forca1));
        Assert.Equal(5 - ArvoreDeTeste.CustoPequeno[0], arvore.PontosLivres);
    }

    [Fact]
    public async Task So_o_dono_mexe_na_arvore_e_sem_token_da_401()
    {
        var (dono, id) = await NovoPersonagemAsync(5);
        var outro = await servidor.CriarClienteLogadoAsync();

        await AssertErroAsync(
            await ComprarAsync(outro, id, Forca1, 1),
            HttpStatusCode.NotFound,
            CodigosErroPersonagem.PersonagemNaoEncontrado
        );
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await outro.GetAsync(RotasTalentos.ArvoreDe(id))).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await servidor.CreateClient().GetAsync(RotasTalentos.ArvoreDe(id))).StatusCode
        );
        Assert.Equal(5, (await LerArvoreAsync(dono, id)).PontosLivres);
    }

    [Fact]
    public async Task A_arvore_fica_gravada_no_banco_e_vale_em_outra_leitura()
    {
        var (cliente, id) = await NovoPersonagemAsync(5);
        (await ComprarAsync(cliente, id, Forca1, 1)).EnsureSuccessStatusCode();
        (await ComprarAsync(cliente, id, Forca1, 2)).EnsureSuccessStatusCode();

        // Escopo novo: sem nada em memória do pedido anterior.
        await using var escopo = servidor.Services.CreateAsyncScope();
        var arvore = await escopo
            .ServiceProvider.GetRequiredService<ServicoDaArvore>()
            .ObterAsync(id, default);

        Assert.Equal(2, RankDe(arvore, Forca1));
        Assert.Equal(
            ArvoreDeTeste.CustoPequeno[0] + ArvoreDeTeste.CustoPequeno[1],
            arvore.PontosGastos
        );
        Assert.Equal(2 * ArvoreDeTeste.ValorPorRankPequeno, arvore.Atributos.Forca);
    }

    [Fact]
    public async Task Os_atributos_da_arvore_chegam_ao_combate()
    {
        var (cliente, id) = await NovoPersonagemAsync(5);
        (await ComprarAsync(cliente, id, Vigor1, 1)).EnsureSuccessStatusCode();
        (await ComprarAsync(cliente, id, Vigor1, 2)).EnsureSuccessStatusCode();

        var recursos = await cliente.GetAsync(RotasCombate.RecursosDe(id));

        Assert.Equal(HttpStatusCode.OK, recursos.StatusCode);
        var corpo = await ServidorDeTeste.LerJsonAsync<RecursosResponse>(recursos);
        var vigor = 2 * ArvoreDeTeste.ValorPorRankPequeno;
        Assert.Equal(vigor, corpo.Atributos.Vigor);
        Assert.Equal(RegrasRecursos.VidaMaxima(vigor), corpo.VidaMaxima);
    }

    [Fact]
    public async Task Trocar_equipamento_nao_muda_pontos_nem_ranks()
    {
        var (cliente, id) = await NovoPersonagemAsync(5);
        (await ComprarAsync(cliente, id, Forca1, 1)).EnsureSuccessStatusCode();
        var antes = await LerArvoreAsync(cliente, id);
        var item = await CriarEquipamentoAsync(id, modelo: 100);

        var equipar = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RegrasEquipamentos.RotaDe(id) + "/equipar",
            new PedidoEquipar { InstanciaId = item, Posicao = PosicaoEquipamento.Arma1 }
        );
        equipar.EnsureSuccessStatusCode();
        var desequipar = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RegrasEquipamentos.RotaDe(id) + "/desequipar",
            new PedidoDesequipar { Posicao = PosicaoEquipamento.Arma1 }
        );
        desequipar.EnsureSuccessStatusCode();

        var depois = await LerArvoreAsync(cliente, id);
        Assert.Equal(antes.PontosLivres, depois.PontosLivres);
        Assert.Equal(antes.PontosGastos, depois.PontosGastos);
        Assert.Equal(
            antes.Ranks.Select(r => (r.NoId, r.Rank)),
            depois.Ranks.Select(r => (r.NoId, r.Rank))
        );
        Assert.Equal(antes.Atributos.Forca, depois.Atributos.Forca);
    }

    private async Task<long> CriarEquipamentoAsync(long personagemId, int modelo)
    {
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var item = new ItemInventario { PersonagemId = personagemId, ItemId = modelo };
        var dados = new GeradorEquipamentos(
            new ConfigEquipamentos { QuantidadeAleatorios = 0 }
        ).Gerar(modelo, new Random(1));
        Assert.NotNull(dados);
        item.GravarEquipamento(dados);
        db.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }
}
