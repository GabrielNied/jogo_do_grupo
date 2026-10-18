using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

/// <summary>Itens, essências e coroas da tela de teste (F8), com banco e contratos reais.</summary>
public class FerramentasDeItensDeTesteTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    private static readonly int Modelo = CatalogoEquipamentos.PrimeiroModelo;

    private async Task<long> NovoPersonagemAsync()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        return (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;
    }

    private async Task<T> Com<T>(Func<IServiceProvider, Task<T>> acao)
    {
        using var escopo = servidor.Services.CreateScope();
        return await acao(escopo.ServiceProvider);
    }

    private Task<(bool Aceito, string Texto)> Gerar(
        long id,
        int quantidade,
        GrauEquipamento grau = GrauEquipamento.Inicial,
        OrigemDoLoot origem = OrigemDoLoot.Comum,
        int modelo = 0
    ) =>
        Com(s =>
            s.GetRequiredService<IFerramentasDeItensDeTeste>()
                .GerarAsync(
                    id,
                    modelo == 0 ? Modelo : modelo,
                    grau,
                    origem,
                    quantidade,
                    CancellationToken.None
                )
        );

    private async Task<List<ItemInventario>> ItensAsync(long id) =>
        await Com(async s =>
            await s.GetRequiredService<JogoDbContext>()
                .Set<ItemInventario>()
                .AsNoTracking()
                .Where(i => i.PersonagemId == id)
                .OrderBy(i => i.Id)
                .ToListAsync()
        );

    [Fact]
    public async Task Gerar_guarda_cada_item_com_grau_modelo_e_tier_calculado_pelo_gerador()
    {
        var id = await NovoPersonagemAsync();

        var resultado = await Gerar(id, 3, GrauEquipamento.Forte, OrigemDoLoot.Chefe);

        Assert.True(resultado.Aceito, resultado.Texto);
        var itens = await ItensAsync(id);
        Assert.Equal(3, itens.Count);
        Assert.Equal(3, itens.Select(i => (i.Aba, i.Casa)).Distinct().Count());
        foreach (var item in itens)
        {
            Assert.Equal(PosicaoEquipamento.Inventario, item.Posicao);
            var exemplar = item.LerEquipamento()!;
            Assert.Equal(Modelo, exemplar.ModeloId);
            Assert.Equal(GrauEquipamento.Forte, exemplar.Grau);
            Assert.Equal(RegrasTier.TierDe(exemplar), exemplar.TierOrigem);
            Assert.Equal(RegrasTier.VersaoAtual, exemplar.VersaoDosAtributos);
        }
    }

    [Fact]
    public async Task Gerar_recusa_modelo_desconhecido_e_nao_cria_nada()
    {
        var id = await NovoPersonagemAsync();

        var resultado = await Gerar(id, 1, modelo: 99999);

        Assert.False(resultado.Aceito);
        Assert.Empty(await ItensAsync(id));
    }

    [Fact]
    public async Task Gerar_com_bolsa_quase_cheia_entra_so_o_que_cabe_e_diz_quanto_ficou()
    {
        var id = await NovoPersonagemAsync();
        var total = RegrasItens.CapacidadeInicial().Total;
        Assert.True((await Gerar(id, total - 1)).Aceito);

        var resultado = await Gerar(id, 3);

        Assert.True(resultado.Aceito, resultado.Texto);
        Assert.Contains("2 não coube", resultado.Texto);
        Assert.Equal(total, (await ItensAsync(id)).Count);
    }

    [Fact]
    public async Task Gerar_com_bolsa_cheia_recusa_sem_criar_item()
    {
        var id = await NovoPersonagemAsync();
        var total = RegrasItens.CapacidadeInicial().Total;
        Assert.True((await Gerar(id, total)).Aceito);

        var resultado = await Gerar(id, 1);

        Assert.False(resultado.Aceito);
        Assert.Equal(total, (await ItensAsync(id)).Count);
    }

    [Fact]
    public async Task Limpar_bolsa_apaga_tudo_menos_favorito()
    {
        var id = await NovoPersonagemAsync();
        Assert.True((await Gerar(id, 3)).Aceito);
        var favorito = (await ItensAsync(id))[0];
        await Com(async s =>
        {
            var db = s.GetRequiredService<JogoDbContext>();
            var linha = await db.Set<ItemInventario>().SingleAsync(i => i.Id == favorito.Id);
            var exemplar = linha.LerEquipamento()!;
            exemplar.Favorito = true;
            linha.GravarEquipamento(exemplar);
            await db.SaveChangesAsync();
            return 0;
        });

        var resultado = await Com(s =>
            s.GetRequiredService<IFerramentasDeItensDeTeste>()
                .LimparBolsaAsync(id, CancellationToken.None)
        );

        Assert.True(resultado.Aceito, resultado.Texto);
        Assert.Equal(favorito.Id, Assert.Single(await ItensAsync(id)).Id);
    }

    [Fact]
    public async Task Limpar_bolsa_vazia_avisa_que_nao_havia_nada()
    {
        var id = await NovoPersonagemAsync();

        var resultado = await Com(s =>
            s.GetRequiredService<IFerramentasDeItensDeTeste>()
                .LimparBolsaAsync(id, CancellationToken.None)
        );

        Assert.False(resultado.Aceito);
    }

    [Fact]
    public async Task Equipar_ultimo_gerado_veste_a_peca_pela_regra_normal()
    {
        var id = await NovoPersonagemAsync();
        Assert.True((await Gerar(id, 1, GrauEquipamento.Inicial)).Aceito);

        var resultado = await Com(s =>
            s.GetRequiredService<IFerramentasDeItensDeTeste>()
                .EquiparUltimoGeradoAsync(id, CancellationToken.None)
        );

        Assert.True(resultado.Aceito, resultado.Texto);
        var item = Assert.Single(await ItensAsync(id));
        Assert.NotEqual(PosicaoEquipamento.Inventario, item.Posicao);
        Assert.Null(item.Aba);
    }

    [Fact]
    public async Task Equipar_ultimo_gerado_respeita_o_nivel_minimo_do_grau()
    {
        var id = await NovoPersonagemAsync();
        Assert.True((await Gerar(id, 1, GrauEquipamento.Forte)).Aceito);
        var exigido = (await ItensAsync(id))[0].LerEquipamento()!.NivelPersonagemMinimo;
        Assert.True(exigido > 1, "o teste precisa de uma peça que peça mais que o nível 1");

        var resultado = await Com(s =>
            s.GetRequiredService<IFerramentasDeItensDeTeste>()
                .EquiparUltimoGeradoAsync(id, CancellationToken.None)
        );

        Assert.False(resultado.Aceito);
        Assert.Equal(PosicaoEquipamento.Inventario, Assert.Single(await ItensAsync(id)).Posicao);
    }

    [Fact]
    public async Task Equipar_sem_ter_gerado_nada_recusa()
    {
        var id = await NovoPersonagemAsync();

        var resultado = await Com(s =>
            s.GetRequiredService<IFerramentasDeItensDeTeste>()
                .EquiparUltimoGeradoAsync(id, CancellationToken.None)
        );

        Assert.False(resultado.Aceito);
    }

    [Fact]
    public async Task Quebrar_e_consertar_mexem_so_na_durabilidade_do_que_esta_vestido()
    {
        var id = await NovoPersonagemAsync();
        Assert.True((await Gerar(id, 2, GrauEquipamento.Inicial)).Aceito);
        Assert.True(
            (
                await Com(s =>
                    s.GetRequiredService<IFerramentasDeItensDeTeste>()
                        .EquiparUltimoGeradoAsync(id, CancellationToken.None)
                )
            ).Aceito
        );

        var quebrar = await Com(s =>
            s.GetRequiredService<IFerramentasDeItensDeTeste>()
                .AjustarDurabilidadeDosEquipadosAsync(id, true, CancellationToken.None)
        );
        Assert.True(quebrar.Aceito, quebrar.Texto);
        var depois = await ItensAsync(id);
        var vestido = depois.Single(i => i.Posicao != PosicaoEquipamento.Inventario);
        var naBolsa = depois.Single(i => i.Posicao == PosicaoEquipamento.Inventario);
        Assert.True(vestido.LerEquipamento()!.Quebrado);
        Assert.False(naBolsa.LerEquipamento()!.Quebrado);

        var consertar = await Com(s =>
            s.GetRequiredService<IFerramentasDeItensDeTeste>()
                .AjustarDurabilidadeDosEquipadosAsync(id, false, CancellationToken.None)
        );
        Assert.True(consertar.Aceito, consertar.Texto);
        var exemplar = (await ItensAsync(id))
            .Single(i => i.Posicao != PosicaoEquipamento.Inventario)
            .LerEquipamento()!;
        Assert.False(exemplar.Quebrado);
        Assert.Equal(exemplar.DurabilidadeMaxima, exemplar.DurabilidadeAtual);
    }

    [Fact]
    public async Task Durabilidade_sem_peca_vestida_recusa()
    {
        var id = await NovoPersonagemAsync();

        var resultado = await Com(s =>
            s.GetRequiredService<IFerramentasDeItensDeTeste>()
                .AjustarDurabilidadeDosEquipadosAsync(id, true, CancellationToken.None)
        );

        Assert.False(resultado.Aceito);
    }

    [Fact]
    public async Task Essencias_somam_nas_duas_contas_de_material()
    {
        var id = await NovoPersonagemAsync();

        var resultado = await Com(s =>
            s.GetRequiredService<IFerramentasDeItensDeTeste>()
                .CreditarEssenciasAsync(id, 4, CancellationToken.None)
        );
        Assert.True(resultado.Aceito, resultado.Texto);
        await Com(s =>
            s.GetRequiredService<IFerramentasDeItensDeTeste>()
                .CreditarEssenciasAsync(id, 3, CancellationToken.None)
        );

        var saldo = await Com(s =>
            s.GetRequiredService<ISaldosMateriaisFusao>().SaldoAsync(id, CancellationToken.None)
        );
        Assert.Equal((7, 7), saldo);
    }

    [Fact]
    public async Task Coroas_somam_e_tirar_demais_para_no_zero()
    {
        var id = await NovoPersonagemAsync();
        Task<(bool Aceito, string Texto)> Ajustar(long quantidade) =>
            Com(s =>
                s.GetRequiredService<IAjusteDeCoroasDeTeste>()
                    .AjustarAsync(id, quantidade, CancellationToken.None)
            );
        Task<long> Saldo() =>
            Com(s =>
                s.GetRequiredService<ICarteiraCoroas>().SaldoAsync(id, CancellationToken.None)
            );

        Assert.True((await Ajustar(500)).Aceito);
        Assert.Equal(500, await Saldo());

        Assert.True((await Ajustar(-200)).Aceito);
        Assert.Equal(300, await Saldo());

        Assert.True((await Ajustar(-1_000_000)).Aceito);
        Assert.Equal(0, await Saldo());

        Assert.False((await Ajustar(0)).Aceito);
    }
}
