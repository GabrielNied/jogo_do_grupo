using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class MigracaoDeTierTestes
{
    private static Equipamento Legado(
        TipoEquipamento tipo,
        params (StatusItem Status, double Valor)[] atributos
    ) =>
        new()
        {
            ModeloId = 102,
            Tipo = tipo,
            Atributos = atributos
                .Select(a => new AtributoItem { Status = a.Status, Valor = a.Valor })
                .ToArray(),
        };

    [Theory]
    // Referência 30 nos básicos: o Forte legado (24) vira 80, o Médio (12) 40 e o Inicial (6) 20.
    [InlineData(StatusItem.Forca, 6, 20)]
    [InlineData(StatusItem.Forca, 12, 40)]
    [InlineData(StatusItem.Forca, 24, 80)]
    [InlineData(StatusItem.Destreza, 0, 0)]
    // Espírito: referência 15 (3, 6 e 12 viram 20, 40 e 80).
    [InlineData(StatusItem.Espirito, 3, 20)]
    [InlineData(StatusItem.Espirito, 12, 80)]
    // Defesa: referência 190 (a maior defesa legada, 152, vira 80).
    [InlineData(StatusItem.Defesa, 152, 80)]
    [InlineData(StatusItem.Defesa, 30, 16)]
    // Infundido (fracionário) arredonda; acima da referência limita em 100.
    [InlineData(StatusItem.Forca, 6.4, 21)]
    [InlineData(StatusItem.Forca, 60, 100)]
    // Sem referência própria, o valor segue como estava.
    [InlineData(StatusItem.PotenciaFogo, 7, 7)]
    public void Valor_legado_vira_inteiro_de_0_a_100(StatusItem status, double legado, int novo)
    {
        var item = Legado(TipoEquipamento.Peito, (status, legado));

        Assert.True(MigracaoDeTier.Migrar(item));

        Assert.Equal(novo, item.Atributos[0].Valor);
        Assert.Equal(RegrasTier.VersaoAtual, item.VersaoDosAtributos);
    }

    [Fact]
    public void Migrar_grava_o_tier_de_origem_calculado()
    {
        var item = Legado(
            TipoEquipamento.Peito,
            (StatusItem.Vitalidade, 24),
            (StatusItem.Forca, 6.6)
        );

        MigracaoDeTier.Migrar(item);

        // 24 vira 80 e 6,6 vira 22: média 51%, tier B.
        Assert.Equal(new double[] { 80, 22 }, item.Atributos.Select(a => a.Valor));
        Assert.Equal(TierItem.B, item.TierOrigem);
        Assert.Equal(TierItem.B, RegrasTier.TierDe(item));
    }

    [Fact]
    public void Migrar_so_converte_uma_vez()
    {
        var item = Legado(TipoEquipamento.Peito, (StatusItem.Forca, 6));
        Assert.True(MigracaoDeTier.Migrar(item));
        item.Atributos[0].Valor = 90; // mudou depois (ex.: fusão): o tier de origem não acompanha.

        Assert.False(MigracaoDeTier.Migrar(item));

        Assert.Equal(90, item.Atributos[0].Valor);
        Assert.Equal(TierItem.D, item.TierOrigem);
        Assert.Equal(TierItem.S, RegrasTier.TierDe(item));
    }

    [Fact]
    public void Peca_forte_legada_com_valores_do_catalogo_sai_com_tier_coerente()
    {
        // Peito do Vigia A, grau Forte: Vitalidade 6 x 4, Defesa 30 x 4, extras 6 x 4 e 3 x 4.
        var forte = Legado(
            TipoEquipamento.Peito,
            (StatusItem.Vitalidade, 24),
            (StatusItem.Defesa, 120),
            (StatusItem.Forca, 24),
            (StatusItem.Espirito, 12)
        );
        var inicial = Legado(
            TipoEquipamento.Peito,
            (StatusItem.Vitalidade, 6),
            (StatusItem.Defesa, 30),
            (StatusItem.Forca, 6),
            (StatusItem.Espirito, 3)
        );

        MigracaoDeTier.Migrar(forte);
        MigracaoDeTier.Migrar(inicial);

        Assert.Equal(TierItem.A, forte.TierOrigem); // 80, 63, 80, 80
        Assert.Equal(TierItem.D, inicial.TierOrigem); // 20, 16, 20, 20
        Assert.All(
            forte.Atributos.Concat(inicial.Atributos),
            a => Assert.Equal(Math.Round(a.Valor), a.Valor)
        );
    }

    [Fact]
    public void Exemplar_sem_atributos_migra_sem_falhar()
    {
        var item = new Equipamento { Atributos = null! };

        Assert.True(MigracaoDeTier.Migrar(item));

        Assert.Empty(item.Atributos);
        Assert.Equal(TierItem.D, item.TierOrigem);
        Assert.False(MigracaoDeTier.Migrar(null!));
    }

    [Fact]
    public void Atributos_do_modelo_anterior_viram_os_novos_e_os_que_caem_no_mesmo_somam()
    {
        // Destreza e o Vigor-recurso caem na Agilidade; Vitalidade e Vida, no Vigor; Defesa vira
        // defesa física; Energia, Espírito; RecuperacaoVigor, regeneração de stamina.
        var item = Atual(
            0,
            (StatusItem.Destreza, 40),
            (StatusItem.Vigor, 30),
            (StatusItem.Vitalidade, 20),
            (StatusItem.Vida, 25),
            (StatusItem.Defesa, 50),
            (StatusItem.Energia, 10),
            (StatusItem.RecuperacaoVigor, 15),
            (StatusItem.PotenciaFogo, 5)
        );
        item.Tipo = TipoEquipamento.Cinto;
        item.VersaoDosAtributos = 1;
        item.TierOrigem = TierItem.B;

        Assert.True(MigracaoDeTier.Migrar(item));

        Assert.Equal(
            new[]
            {
                StatusItem.RegeneracaoStamina,
                StatusItem.VidaMaxima,
                StatusItem.DefesaFisica,
                StatusItem.RegeneracaoMana,
                StatusItem.PotenciaFogo,
            },
            item.Atributos.Select(a => a.Status)
        );
        // Agilidade e RecuperacaoVigor viram regeneração de stamina (70 + 15); defesa legada fica sem faixa no cinto.
        Assert.Equal(new double[] { 85, 45, 50, 10, 5 }, item.Atributos.Select(a => a.Valor));
        Assert.Equal(RegrasTier.VersaoAtual, item.VersaoDosAtributos);
        Assert.False(MigracaoDeTier.Migrar(item));
    }

    [Fact]
    public void Soma_dos_que_caem_no_mesmo_novo_e_limitada_a_100()
    {
        var item = Atual(0, (StatusItem.Destreza, 70), (StatusItem.Vigor, 60));
        item.VersaoDosAtributos = 1;

        MigracaoDeTier.Migrar(item);

        Assert.Equal(StatusItem.RegeneracaoStamina, Assert.Single(item.Atributos).Status);
        Assert.Equal(100, Assert.Single(item.Atributos).Valor);
    }

    [Fact]
    public void Exemplar_da_versao_1_preserva_o_tier_de_origem_e_recalcula_o_atual()
    {
        var item = Atual(0, (StatusItem.Destreza, 90), (StatusItem.Defesa, 90));
        item.VersaoDosAtributos = 1;
        item.TierOrigem = TierItem.D;

        MigracaoDeTier.Migrar(item);

        Assert.Equal(TierItem.D, item.TierOrigem); // TIER-004
        Assert.Equal(TierItem.S, RegrasTier.TierDe(item));
    }

    [Fact]
    public void Exemplar_da_escala_antiga_ganha_o_tier_de_origem_com_os_atributos_novos()
    {
        var item = Legado(
            TipoEquipamento.Peito,
            (StatusItem.Destreza, 24),
            (StatusItem.Defesa, 152)
        );

        MigracaoDeTier.Migrar(item);

        Assert.Equal(new double[] { 80, 80 }, item.Atributos.Select(a => a.Valor));
        Assert.Equal(TierItem.S, item.TierOrigem);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 3)]
    [InlineData(10, 10)]
    [InlineData(25, 10)]
    public void Infusoes_do_modelo_anterior_viram_nivel_de_fusao_de_no_maximo_10(
        int infusoes,
        int nivel
    )
    {
        var item = Atual(infusoes, (StatusItem.DanoFisico, 20));

        Migrar_e_conferir(item, infusoes > 0);

        Assert.Equal(nivel, item.NivelFusao);
        Assert.Equal(0, item.InfusoesUsadas);
    }

    [Fact]
    public void Valores_fracionarios_da_infusao_sao_arredondados_e_o_tier_de_origem_fica()
    {
        var item = Atual(4, (StatusItem.DanoFisico, 20.4), (StatusItem.Vitalidade, 20.5));
        item.TierOrigem = TierItem.C;

        Assert.True(MigracaoDeTier.Migrar(item));

        Assert.Equal(new double[] { 20, 21 }, item.Atributos.Select(a => a.Valor));
        Assert.Equal(TierItem.C, item.TierOrigem);
        Assert.False(MigracaoDeTier.Migrar(item));
        Assert.Equal(4, item.NivelFusao);
    }

    [Fact]
    public void Item_ja_fundido_nao_perde_o_nivel_ao_ser_lido_de_novo()
    {
        var item = Atual(0, (StatusItem.DanoFisico, 30));
        item.NivelFusao = 7;

        Assert.False(MigracaoDeTier.Migrar(item));

        Assert.Equal(7, item.NivelFusao);
    }

    private static Equipamento Atual(
        int infusoes,
        params (StatusItem Status, double Valor)[] atributos
    )
    {
        var item = Legado(TipoEquipamento.Peito, atributos);
        item.VersaoDosAtributos = RegrasTier.VersaoAtual;
        item.InfusoesUsadas = infusoes;
        return item;
    }

    private static void Migrar_e_conferir(Equipamento item, bool mudou) =>
        Assert.Equal(mudou, MigracaoDeTier.Migrar(item));
}
