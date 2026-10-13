using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class MigracaoDeTierTestes
{
    private static Equipamento Legado(
        TipoEquipamento tipo,
        int indiceInfusivel,
        double baseInfusivel,
        params (StatusItem Status, double Valor)[] atributos
    ) =>
        new()
        {
            ModeloId = 102,
            Tipo = tipo,
            IndiceInfusivel = indiceInfusivel,
            ValorBaseInfusivel = baseInfusivel,
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
        var item = Legado(TipoEquipamento.Peito, 0, legado, (status, legado));

        Assert.True(MigracaoDeTier.Migrar(item));

        Assert.Equal(novo, item.Atributos[0].Valor);
        Assert.Equal(RegrasTier.VersaoAtual, item.VersaoDosAtributos);
    }

    [Fact]
    public void Migrar_grava_o_tier_de_origem_calculado_e_corrige_a_base_da_infusao()
    {
        var item = Legado(
            TipoEquipamento.Peito,
            1,
            6,
            (StatusItem.Vitalidade, 24),
            (StatusItem.Forca, 6.6)
        );

        MigracaoDeTier.Migrar(item);

        // 24 vira 80 e 6,6 vira 22: média 51%, tier B.
        Assert.Equal(new double[] { 80, 22 }, item.Atributos.Select(a => a.Valor));
        Assert.Equal(TierItem.B, item.TierOrigem);
        Assert.Equal(TierItem.B, RegrasTier.TierDe(item));
        Assert.Equal(20.0, item.ValorBaseInfusivel, 6);
    }

    [Fact]
    public void Migrar_so_converte_uma_vez()
    {
        var item = Legado(TipoEquipamento.Peito, 0, 6, (StatusItem.Forca, 6));
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
            0,
            24,
            (StatusItem.Vitalidade, 24),
            (StatusItem.Defesa, 120),
            (StatusItem.Forca, 24),
            (StatusItem.Espirito, 12)
        );
        var inicial = Legado(
            TipoEquipamento.Peito,
            0,
            6,
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
    public void Escala_legada_e_o_inverso_da_migracao_nos_valores_do_catalogo()
    {
        foreach (
            var (status, legado) in new[]
            {
                (StatusItem.Forca, 6.0),
                (StatusItem.Forca, 12.0),
                (StatusItem.Forca, 24.0),
                (StatusItem.Espirito, 3.0),
                (StatusItem.Espirito, 12.0),
            }
        )
        {
            var novo = MigracaoDeTier.ParaEscalaInteira(status, legado);
            Assert.Equal(legado, MigracaoDeTier.ParaEscalaLegada(status, novo), 6);
        }
    }
}
