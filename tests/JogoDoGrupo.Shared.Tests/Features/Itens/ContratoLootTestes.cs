using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class ContratoLootTestes
{
    private static readonly ushort[] _ids =
    {
        ItensMessageIds.LootApareceu,
        ItensMessageIds.ColetarLoot,
        ItensMessageIds.LootColetado,
        ItensMessageIds.ColetaRecusada,
        ItensMessageIds.LootSumiu,
    };

    [Theory]
    [InlineData(ItensMessageIds.LootApareceu)]
    [InlineData(ItensMessageIds.ColetarLoot)]
    [InlineData(ItensMessageIds.LootColetado)]
    [InlineData(ItensMessageIds.ColetaRecusada)]
    [InlineData(ItensMessageIds.LootSumiu)]
    public void Id_esta_dentro_da_faixa_de_itens(ushort id) =>
        Assert.True(
            ItensMessageIds.Faixa.Contem(id),
            $"ID {id} fora de ItensMessageIds.Faixa ({ItensMessageIds.Faixa.Inicio}–{ItensMessageIds.Faixa.Fim})"
        );

    [Fact]
    public void Ids_de_loot_sao_distintos() => Assert.Equal(_ids.Length, _ids.Distinct().Count());

    [Fact]
    public void Ids_sao_os_do_contrato()
    {
        Assert.Equal(7000, ItensMessageIds.LootApareceu);
        Assert.Equal(7001, ItensMessageIds.ColetarLoot);
        Assert.Equal(7002, ItensMessageIds.LootColetado);
        Assert.Equal(7003, ItensMessageIds.ColetaRecusada);
        Assert.Equal(7004, ItensMessageIds.LootSumiu);
    }

    [Fact]
    public void LootNoChao_campos_padrao()
    {
        var loot = new LootNoChao();
        Assert.Equal(0L, loot.LootId);
        Assert.Equal(0, loot.ItemId);
        Assert.Equal("", loot.Nome);
        Assert.Equal(0f, loot.X);
        Assert.Equal(0f, loot.Z);
        Assert.Equal(0, loot.ExpiraEmSegundos);
    }

    [Fact]
    public void Mensagens_comecam_com_objetos_nao_nulos()
    {
        Assert.NotNull(new LootApareceu().Loot);
        Assert.NotNull(new LootColetado().Entrada);
        Assert.Equal(0L, new LootColetado().LootId);
        Assert.Equal(0L, new LootSumiu().LootId);
    }

    [Fact]
    public void ColetarLoot_sem_id_pede_tudo_por_perto() =>
        Assert.Equal(RegrasLoot.TudoPorPerto, new ColetarLoot().LootId);

    [Fact]
    public void ColetaRecusada_campos_padrao()
    {
        var msg = new ColetaRecusada();
        Assert.Equal(0L, msg.LootId);
        Assert.Equal("", msg.Codigo);
    }

    [Fact]
    public void Codigos_de_erro_do_loot_tem_o_texto_do_contrato()
    {
        Assert.Equal("LootNaoEncontrado", CodigosErroItens.LootNaoEncontrado);
        Assert.Equal("LongeDemais", CodigosErroItens.LongeDemais);
        Assert.Equal("InventarioCheio", CodigosErroItens.InventarioCheio);
    }
}
