using JogoDoGrupo.Server.Features.Itens;

namespace JogoDoGrupo.Server.Tests.Features.Pets;

/// <summary>A escolha do item que o pet vai buscar, sem rede nem banco.</summary>
public class EscolhaDeLootDoPetTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
    private const long Ana = 1;
    private const long Bia = 2;

    [Fact]
    public void Escolhe_o_item_do_dono_mais_perto_do_ponto()
    {
        var chao = new ChaoDeLoot();
        chao.Largar(Ana, [1], "cidade", 6, 0, Agora);
        var perto = chao.Largar(Ana, [2], "cidade", 2, 0, Agora)[0];

        Assert.True(chao.TentarEscolherMaisPerto(Ana, "cidade", 0, 0, 8, Agora, out var loot));
        Assert.Equal(perto.LootId, loot.LootId);
    }

    [Fact]
    public void Item_alem_do_raio_nao_e_escolhido_e_no_raio_exato_e()
    {
        var chao = new ChaoDeLoot();
        chao.Largar(Ana, [1], "cidade", 8, 0, Agora);

        Assert.True(chao.TentarEscolherMaisPerto(Ana, "cidade", 0, 0, 8, Agora, out _));
        Assert.False(chao.TentarEscolherMaisPerto(Ana, "cidade", 0, 0, 7.99, Agora, out _));
    }

    [Fact]
    public void Item_de_outro_dono_ou_de_outra_area_nao_e_escolhido()
    {
        var chao = new ChaoDeLoot();
        chao.Largar(Bia, [1], "cidade", 1, 0, Agora);
        chao.Largar(Ana, [2], "instancia:x", 1, 0, Agora);

        Assert.False(chao.TentarEscolherMaisPerto(Ana, "cidade", 0, 0, 8, Agora, out _));
    }

    [Fact]
    public void Item_ja_separado_para_a_coleta_nao_e_escolhido()
    {
        var chao = new ChaoDeLoot();
        var loot = chao.Largar(Ana, [1], "cidade", 1, 0, Agora)[0];
        Assert.True(chao.TentarSeparar(Ana, loot.LootId, Agora));

        Assert.False(chao.TentarEscolherMaisPerto(Ana, "cidade", 0, 0, 8, Agora, out _));
    }
}
