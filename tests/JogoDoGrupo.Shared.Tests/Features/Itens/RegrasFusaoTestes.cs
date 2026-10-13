using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class RegrasFusaoTestes
{
    private static readonly FaixaDeAtributo Faixa = new(0, 100);

    private static Equipamento Peca(
        TipoEquipamento tipo,
        params (StatusItem Status, double Valor)[] atributos
    ) =>
        new()
        {
            ModeloId = 102,
            Tipo = tipo,
            VersaoDosAtributos = RegrasTier.VersaoAtual,
            Atributos = atributos
                .Select(a => new AtributoItem { Status = a.Status, Valor = a.Valor })
                .ToArray(),
        };

    // Tabela da doc (FUSAO-014) e as bordas da fórmula atual + sinal(d) x teto(|d| / 2).
    [Theory]
    [InlineData(20, 40, 30)]
    [InlineData(40, 20, 30)]
    [InlineData(20, 25, 23)] // 22,5 vai na direção do material
    [InlineData(25, 20, 22)] // 22,5 vai na direção do material (para baixo)
    [InlineData(20, 21, 21)]
    [InlineData(21, 20, 20)]
    [InlineData(20, 20, 20)]
    [InlineData(0, 100, 50)]
    [InlineData(100, 0, 50)]
    [InlineData(0, 1, 1)]
    [InlineData(99, 100, 100)]
    [InlineData(100, 99, 99)]
    [InlineData(20, 23, 22)]
    public void Resultado_anda_metade_da_diferenca_em_direcao_ao_material(
        int atual,
        int material,
        int esperado
    ) => Assert.Equal(esperado, RegrasFusao.Resultado(atual, material, Faixa));

    [Fact]
    public void Resultado_nunca_passa_do_material_e_sempre_anda_quando_diferem()
    {
        for (var atual = 0; atual <= 100; atual++)
        for (var material = 0; material <= 100; material++)
        {
            var depois = RegrasFusao.Resultado(atual, material, Faixa);
            if (atual == material)
            {
                Assert.Equal(atual, depois);
                continue;
            }

            Assert.NotEqual(atual, depois);
            Assert.InRange(depois, Math.Min(atual, material), Math.Max(atual, material));
        }
    }

    [Fact]
    public void Resultado_respeita_a_faixa_da_categoria()
    {
        var estreita = new FaixaDeAtributo(10, 60);

        Assert.Equal(60, RegrasFusao.Resultado(60, 100, estreita)); // teto: o máximo absoluto
        Assert.Equal(60, RegrasFusao.Resultado(59, 100, estreita));
        Assert.Equal(10, RegrasFusao.Resultado(10, 0, estreita));
    }

    [Fact]
    public void Valor_fracionario_do_legado_e_arredondado_antes_da_conta()
    {
        Assert.Equal(30, RegrasFusao.Resultado(20.4, 40.2, Faixa));
        Assert.Equal(21, RegrasFusao.Resultado(20.5, 20.6, Faixa)); // 21 e 21
    }

    [Fact]
    public void Validar_aceita_mesma_categoria_mesmo_com_grau_regiao_e_raridade_diferentes()
    {
        var principal = Peca(TipoEquipamento.Espada, (StatusItem.Forca, 20));
        var material = Peca(TipoEquipamento.Espada, (StatusItem.Forca, 40));
        material.Grau = GrauEquipamento.Forte;
        material.Regiao = "Serra";
        material.Familia = "Predador";
        material.Raridade = RaridadeItem.Lendario;

        Assert.Null(RegrasFusao.Validar(principal, material));
    }

    [Fact]
    public void Validar_recusa_incompativel_favorito_e_maximo()
    {
        var principal = Peca(TipoEquipamento.Espada, (StatusItem.Forca, 20));
        var material = Peca(TipoEquipamento.Martelo, (StatusItem.Forca, 40));
        Assert.Equal(
            CodigosErroItens.MaterialIncompativel,
            RegrasFusao.Validar(principal, material)
        );

        material.Tipo = TipoEquipamento.Espada;
        material.Favorito = true;
        Assert.Equal(CodigosErroItens.ItemFavorito, RegrasFusao.Validar(principal, material));

        material.Favorito = false;
        principal.NivelFusao = ConfigFusao.NivelMaximo;
        Assert.Equal(CodigosErroItens.NivelMaximo, RegrasFusao.Validar(principal, material));
        principal.NivelFusao = ConfigFusao.NivelMaximo - 1;
        Assert.Null(RegrasFusao.Validar(principal, material));

        principal.NivelFusao = -1;
        Assert.Equal(CodigosErroItens.ItemInvalido, RegrasFusao.Validar(principal, material));
        Assert.Equal(CodigosErroItens.ItemNaoEncontrado, RegrasFusao.Validar(null!, material));
    }

    [Fact]
    public void Elegiveis_sao_os_atributos_dos_dois_itens_com_faixa_na_categoria()
    {
        var principal = Peca(
            TipoEquipamento.Peito,
            (StatusItem.Forca, 20),
            (StatusItem.Vigor, 30),
            (StatusItem.DefesaFisica, 40),
            (StatusItem.PotenciaFogo, 5) // sem faixa na categoria
        );
        var material = Peca(
            TipoEquipamento.Peito,
            (StatusItem.DefesaFisica, 10),
            (StatusItem.Forca, 80),
            (StatusItem.PotenciaFogo, 9)
        );

        Assert.Equal(
            new[] { StatusItem.Forca, StatusItem.DefesaFisica },
            RegrasFusao.Elegiveis(principal, material)
        );
        Assert.Empty(RegrasFusao.Elegiveis(null!, material));
    }

    // A regra é genérica: quem decide o que é elegível é a tabela, não um caso por atributo.
    [Fact]
    public void Elegiveis_obedecem_a_tabela_de_faixas_dada()
    {
        var tabela = new TabelaDeFaixas()
            .Padrao(StatusItem.Forca, 0, 100)
            .Padrao(StatusItem.Destreza, 0, 100)
            .Inelegivel(TipoEquipamento.Anel, StatusItem.Forca);
        var anel = Peca(TipoEquipamento.Anel, (StatusItem.Forca, 20), (StatusItem.Destreza, 20));
        var outro = Peca(TipoEquipamento.Anel, (StatusItem.Forca, 50), (StatusItem.Destreza, 50));

        Assert.Equal(new[] { StatusItem.Destreza }, RegrasFusao.Elegiveis(anel, outro, tabela));
    }

    [Fact]
    public void Custos_seguem_a_tabela_da_doc_e_somam_15500()
    {
        Assert.Equal(
            new long[] { 100, 200, 350, 550, 800, 1200, 1700, 2400, 3400, 4800 },
            Enumerable.Range(0, 10).Select(ConfigFusao.Custo)
        );
        Assert.Equal(15500, ConfigFusao.CustoTotal());
        Assert.Equal(
            new[] { 0, 0, 0, 0, 0, 1, 2, 3, 0, 0 },
            Enumerable.Range(0, 10).Select(ConfigFusao.MateriaisDungeon)
        );
        Assert.Equal(
            new[] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 2 },
            Enumerable.Range(0, 10).Select(ConfigFusao.MateriaisRaid)
        );
        Assert.Throws<ArgumentOutOfRangeException>(() => ConfigFusao.Custo(10));
        Assert.Throws<ArgumentOutOfRangeException>(() => ConfigFusao.Custo(-1));
    }

    [Fact]
    public void Descarte_pede_segunda_confirmacao_com_fusao_ou_tier_A_ou_acima()
    {
        var comum = Peca(TipoEquipamento.Espada, (StatusItem.Forca, 20));
        Assert.False(RegrasFusao.DescarteExigeSegundaConfirmacao(comum));

        comum.NivelFusao = 1;
        Assert.True(RegrasFusao.DescarteExigeSegundaConfirmacao(comum));

        var tierB = Peca(TipoEquipamento.Espada, (StatusItem.Forca, 64));
        var tierA = Peca(TipoEquipamento.Espada, (StatusItem.Forca, 65));
        Assert.Equal(TierItem.B, RegrasTier.TierDe(tierB));
        Assert.False(RegrasFusao.DescarteExigeSegundaConfirmacao(tierB));
        Assert.True(RegrasFusao.DescarteExigeSegundaConfirmacao(tierA));
        Assert.False(RegrasFusao.DescarteExigeSegundaConfirmacao(null!));
    }
}
