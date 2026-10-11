using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

/// <summary>O chão de loot sem rede nem banco: dono, prazo e a coleta em duas etapas.</summary>
public class ChaoDeLootTestes
{
    private const long Ana = 10;
    private const long Beto = 20;
    private const string Area = "cidade";
    private static readonly DateTimeOffset _inicio = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset Depois(double segundos) => _inicio.AddSeconds(segundos);

    private static IReadOnlyList<LootReservado> Largar(
        ChaoDeLoot chao,
        long dono,
        params int[] itens
    ) => chao.Largar(dono, itens, Area, 5.0, 7.0, _inicio);

    [Fact]
    public void Largar_cria_um_item_por_id_com_dono_posicao_e_ids_distintos()
    {
        var chao = new ChaoDeLoot();

        var novos = Largar(chao, Ana, CatalogoItens.EspadaDeTeste, CatalogoItens.EspadaDeTeste);

        Assert.Equal(2, novos.Count);
        Assert.Equal(2, chao.Quantidade);
        Assert.NotEqual(novos[0].LootId, novos[1].LootId);
        Assert.All(
            novos,
            loot =>
            {
                Assert.NotEqual(RegrasLoot.TudoPorPerto, loot.LootId);
                Assert.Equal(Ana, loot.DonoId);
                Assert.Equal(CatalogoItens.EspadaDeTeste, loot.ItemId);
                Assert.Equal(Area, loot.Area);
                Assert.Equal(5.0, loot.X);
                Assert.Equal(7.0, loot.Z);
                Assert.Equal(_inicio, loot.ApareceuEm);
            }
        );
    }

    [Fact]
    public void Largar_sem_itens_nao_poe_nada_no_chao()
    {
        var chao = new ChaoDeLoot();

        Assert.Empty(Largar(chao, Ana));
        Assert.Equal(0, chao.Quantidade);
    }

    [Fact]
    public void Ids_nao_se_repetem_entre_donos_nem_depois_de_uma_coleta()
    {
        var chao = new ChaoDeLoot();
        var daAna = Largar(chao, Ana, CatalogoItens.EspadaDeTeste)[0];
        Assert.True(chao.TentarSeparar(Ana, daAna.LootId, _inicio));
        chao.Confirmar(daAna.LootId);

        var doBeto = Largar(chao, Beto, CatalogoItens.EscudoDeTeste)[0];

        Assert.NotEqual(daAna.LootId, doBeto.LootId);
    }

    [Fact]
    public void DoDono_so_mostra_os_itens_do_proprio_dono_em_ordem()
    {
        var chao = new ChaoDeLoot();
        var daAna = Largar(chao, Ana, CatalogoItens.EspadaDeTeste, CatalogoItens.PocaoDeTeste);
        Largar(chao, Beto, CatalogoItens.EscudoDeTeste);

        var vistos = chao.DoDono(Ana, _inicio);

        Assert.Equal(daAna.Select(l => l.LootId), vistos.Select(l => l.LootId));
        Assert.Single(chao.DoDono(Beto, _inicio));
        Assert.Empty(chao.DoDono(999, _inicio));
    }

    [Fact]
    public void Item_de_outro_dono_responde_como_se_nao_existisse()
    {
        var chao = new ChaoDeLoot();
        var daAna = Largar(chao, Ana, CatalogoItens.EspadaDeTeste)[0];

        Assert.False(chao.TentarObter(Beto, daAna.LootId, _inicio, out _));
        Assert.False(chao.TentarSeparar(Beto, daAna.LootId, _inicio));
        Assert.True(chao.TentarObter(Ana, daAna.LootId, _inicio, out var achado));
        Assert.Equal(daAna, achado);
    }

    [Fact]
    public void Id_que_nao_existe_nao_e_achado()
    {
        var chao = new ChaoDeLoot();
        Largar(chao, Ana, CatalogoItens.EspadaDeTeste);

        Assert.False(chao.TentarObter(Ana, 9999, _inicio, out _));
        Assert.False(chao.TentarSeparar(Ana, 9999, _inicio));
        Assert.False(chao.TentarObter(Ana, RegrasLoot.TudoPorPerto, _inicio, out _));
    }

    [Fact]
    public void Separar_duas_vezes_o_mesmo_item_so_vale_na_primeira()
    {
        var chao = new ChaoDeLoot();
        var loot = Largar(chao, Ana, CatalogoItens.EspadaDeTeste)[0];

        Assert.True(chao.TentarSeparar(Ana, loot.LootId, _inicio));
        Assert.False(chao.TentarSeparar(Ana, loot.LootId, _inicio));
        Assert.False(chao.TentarObter(Ana, loot.LootId, _inicio, out _));
        Assert.Empty(chao.DoDono(Ana, _inicio));
    }

    [Fact]
    public void Separar_ao_mesmo_tempo_da_o_item_a_um_pedido_so()
    {
        var chao = new ChaoDeLoot();
        var loot = Largar(chao, Ana, CatalogoItens.EspadaDeTeste)[0];
        var conseguiram = 0;

        Parallel.For(
            0,
            64,
            _ =>
            {
                if (chao.TentarSeparar(Ana, loot.LootId, _inicio))
                {
                    Interlocked.Increment(ref conseguiram);
                }
            }
        );

        Assert.Equal(1, conseguiram);
    }

    [Fact]
    public void Confirmar_tira_o_item_do_chao_de_vez()
    {
        var chao = new ChaoDeLoot();
        var loot = Largar(chao, Ana, CatalogoItens.EspadaDeTeste)[0];
        chao.TentarSeparar(Ana, loot.LootId, _inicio);

        chao.Confirmar(loot.LootId);

        Assert.Equal(0, chao.Quantidade);
        Assert.False(chao.TentarSeparar(Ana, loot.LootId, _inicio));
        chao.Devolver(loot.LootId); // Devolver o que já saiu não o traz de volta.
        Assert.Equal(0, chao.Quantidade);
    }

    [Fact]
    public void Confirmar_sem_separar_antes_nao_remove()
    {
        var chao = new ChaoDeLoot();
        var loot = Largar(chao, Ana, CatalogoItens.EspadaDeTeste)[0];

        chao.Confirmar(loot.LootId);

        Assert.True(chao.TentarObter(Ana, loot.LootId, _inicio, out _));
    }

    [Fact]
    public void Devolver_deixa_o_item_livre_de_novo()
    {
        var chao = new ChaoDeLoot();
        var loot = Largar(chao, Ana, CatalogoItens.EspadaDeTeste)[0];
        chao.TentarSeparar(Ana, loot.LootId, _inicio);

        chao.Devolver(loot.LootId);

        Assert.True(chao.TentarObter(Ana, loot.LootId, _inicio, out _));
        Assert.True(chao.TentarSeparar(Ana, loot.LootId, _inicio));
    }

    [Fact]
    public void Item_vale_ate_o_ultimo_instante_do_prazo_e_some_ao_completar()
    {
        var chao = new ChaoDeLoot();
        var loot = Largar(chao, Ana, CatalogoItens.EspadaDeTeste)[0];
        var quase = Depois(RegrasLoot.ReservaSegundos - 0.001);
        var noPrazo = Depois(RegrasLoot.ReservaSegundos);

        Assert.True(chao.TentarObter(Ana, loot.LootId, quase, out _));
        Assert.Empty(chao.RemoverExpirados(quase));

        Assert.False(chao.TentarObter(Ana, loot.LootId, noPrazo, out _));
        Assert.False(chao.TentarSeparar(Ana, loot.LootId, noPrazo));
        Assert.Empty(chao.DoDono(Ana, noPrazo));
        Assert.Equal(loot, Assert.Single(chao.RemoverExpirados(noPrazo)));
        Assert.Equal(0, chao.Quantidade);
        Assert.Empty(chao.RemoverExpirados(noPrazo)); // Uma vez só.
    }

    [Fact]
    public void RemoverExpirados_so_leva_os_vencidos()
    {
        var chao = new ChaoDeLoot();
        var velho = Largar(chao, Ana, CatalogoItens.EspadaDeTeste)[0];
        var novo = chao.Largar(Beto, [CatalogoItens.EscudoDeTeste], Area, 0, 0, Depois(100))[0];

        var expirados = chao.RemoverExpirados(Depois(RegrasLoot.ReservaSegundos + 1));

        Assert.Equal(velho, Assert.Single(expirados));
        Assert.True(chao.TentarObter(Beto, novo.LootId, Depois(200), out _));
    }

    [Fact]
    public void Item_em_coleta_nao_expira_ate_ser_devolvido()
    {
        var chao = new ChaoDeLoot();
        var loot = Largar(chao, Ana, CatalogoItens.EspadaDeTeste)[0];
        var vencido = Depois(RegrasLoot.ReservaSegundos + 5);
        chao.TentarSeparar(Ana, loot.LootId, _inicio);

        Assert.Empty(chao.RemoverExpirados(vencido));

        chao.Devolver(loot.LootId);
        Assert.Equal(loot, Assert.Single(chao.RemoverExpirados(vencido)));
    }

    [Fact]
    public void ParaMensagem_leva_o_nome_do_catalogo_e_o_tempo_que_falta()
    {
        var chao = new ChaoDeLoot();
        var loot = Largar(chao, Ana, CatalogoItens.PocaoDeTeste)[0];

        var nova = loot.ParaMensagem(_inicio);
        var depois = loot.ParaMensagem(Depois(60.5));

        Assert.Equal(loot.LootId, nova.LootId);
        Assert.Equal(CatalogoItens.PocaoDeTeste, nova.ItemId);
        Assert.Equal("Poção de teste", nova.Nome);
        Assert.Equal(5f, nova.X);
        Assert.Equal(7f, nova.Z);
        Assert.Equal(RegrasLoot.ReservaSegundos, nova.ExpiraEmSegundos);
        Assert.Equal(RegrasLoot.ReservaSegundos - 60, depois.ExpiraEmSegundos);
    }
}
