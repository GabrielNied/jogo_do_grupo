using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class RegrasTierTestes
{
    private static Equipamento Peca(
        TipoEquipamento tipo = TipoEquipamento.Peito,
        params (StatusItem Status, double Valor)[] atributos
    ) =>
        new()
        {
            Tipo = tipo,
            VersaoDosAtributos = RegrasTier.VersaoAtual,
            Atributos = atributos
                .Select(a => new AtributoItem { Status = a.Status, Valor = a.Valor })
                .ToArray(),
        };

    private static Equipamento Unico(double valor) =>
        Peca(TipoEquipamento.Peito, (StatusItem.Forca, valor));

    // Faixa 0 a 100: a qualidade é o próprio valor. Abaixo, no limite e acima de cada limiar.
    [Theory]
    [InlineData(0, TierItem.D)]
    [InlineData(34, TierItem.D)]
    [InlineData(35, TierItem.C)]
    [InlineData(36, TierItem.C)]
    [InlineData(49, TierItem.C)]
    [InlineData(50, TierItem.B)]
    [InlineData(51, TierItem.B)]
    [InlineData(64, TierItem.B)]
    [InlineData(65, TierItem.A)]
    [InlineData(66, TierItem.A)]
    [InlineData(79, TierItem.A)]
    [InlineData(80, TierItem.S)]
    [InlineData(81, TierItem.S)]
    [InlineData(99, TierItem.S)]
    [InlineData(100, TierItem.SMais)]
    public void Tier_pelos_limiares_da_qualidade(double valor, TierItem esperado) =>
        Assert.Equal(esperado, RegrasTier.TierDe(Unico(valor)));

    [Fact]
    public void Qualidade_e_a_media_dos_atributos_elegiveis()
    {
        // 90%, 85% e 65% dão 80%, tier S (exemplo da doc).
        var resultado = RegrasTier.Calcular(
            Peca(
                TipoEquipamento.Peito,
                (StatusItem.Forca, 90),
                (StatusItem.Vitalidade, 85),
                (StatusItem.Defesa, 65)
            )
        );

        Assert.Equal(80.0, resultado.Qualidade, 6);
        Assert.Equal(TierItem.S, resultado.Tier);
        Assert.Equal(3, resultado.Elegiveis);
    }

    [Fact]
    public void SMais_exige_todos_os_elegiveis_no_maximo()
    {
        var tudoNoMaximo = Peca(
            TipoEquipamento.Anel,
            (StatusItem.Forca, 100),
            (StatusItem.Destreza, 100),
            (StatusItem.Defesa, 100)
        );
        Assert.Equal(TierItem.SMais, RegrasTier.TierDe(tudoNoMaximo));

        // Um atributo a 1 do máximo: média 99,67%, mas não é S+.
        tudoNoMaximo.Atributos[2].Valor = 99;
        var resultado = RegrasTier.Calcular(tudoNoMaximo);
        Assert.Equal(TierItem.S, resultado.Tier);
        Assert.True(resultado.Qualidade > 99);
    }

    [Fact]
    public void Atributo_ausente_nao_conta_como_zero_nem_impede_SMais()
    {
        // Um anel só com um atributo, no máximo, é S+: o que o modelo não tem não entra.
        Assert.Equal(TierItem.SMais, RegrasTier.TierDe(Unico(100)));
        var dois = RegrasTier.Calcular(
            Peca(TipoEquipamento.Anel, (StatusItem.Forca, 70), (StatusItem.Defesa, 70))
        );
        var um = RegrasTier.Calcular(Peca(TipoEquipamento.Anel, (StatusItem.Forca, 70)));
        Assert.Equal(um.Qualidade, dois.Qualidade, 6);
    }

    [Fact]
    public void Atributo_sem_faixa_e_ignorado_e_sem_elegiveis_e_tier_D()
    {
        var comLegado = Peca(
            TipoEquipamento.Peito,
            (StatusItem.Forca, 100),
            (StatusItem.PotenciaFogo, 0)
        );
        Assert.Equal(TierItem.SMais, RegrasTier.TierDe(comLegado));

        var semElegiveis = RegrasTier.Calcular(
            Peca(TipoEquipamento.Peito, (StatusItem.PotenciaFogo, 100))
        );
        Assert.Equal(TierItem.D, semElegiveis.Tier);
        Assert.Equal(0, semElegiveis.Elegiveis);
        Assert.Equal(TierItem.D, RegrasTier.TierDe(null!));
    }

    [Fact]
    public void Tier_depende_so_dos_atributos()
    {
        var peca = Unico(70);
        var antes = RegrasTier.TierDe(peca);

        peca.Raridade = RaridadeItem.Lendario;
        peca.Grau = GrauEquipamento.Forte;
        peca.Nivel = 9;
        peca.InfusoesUsadas = 5;
        peca.TierOrigem = TierItem.SMais;
        Assert.Equal(antes, RegrasTier.TierDe(peca));

        peca.Atributos[0].Valor = 30;
        Assert.Equal(TierItem.D, RegrasTier.TierDe(peca));
    }

    // A regra é genérica: faixa e elegíveis vêm da tabela, por StatusItem e categoria.
    [Fact]
    public void Faixa_propria_da_tabela_muda_a_qualidade_sem_mexer_na_regra()
    {
        var tabela = new TabelaDeFaixas()
            .Padrao(StatusItem.Alcance, 20, 60)
            .Categoria(TipoEquipamento.Anel, StatusItem.Alcance, 0, 10)
            .Inelegivel(TipoEquipamento.Brinco, StatusItem.Alcance);

        Assert.Equal(
            TierItem.B, // (40 - 20) / (60 - 20) = 50%
            RegrasTier.Calcular(Peca(TipoEquipamento.Peito, (StatusItem.Alcance, 40)), tabela).Tier
        );
        Assert.Equal(
            TierItem.SMais,
            RegrasTier.Calcular(Peca(TipoEquipamento.Peito, (StatusItem.Alcance, 60)), tabela).Tier
        );
        Assert.Equal(
            TierItem.D, // abaixo do mínimo: qualidade 0
            RegrasTier.Calcular(Peca(TipoEquipamento.Peito, (StatusItem.Alcance, 5)), tabela).Tier
        );
        // Na categoria com faixa própria o máximo é 10.
        Assert.Equal(
            TierItem.SMais,
            RegrasTier.Calcular(Peca(TipoEquipamento.Anel, (StatusItem.Alcance, 10)), tabela).Tier
        );
        // Inelegível na categoria: não entra na conta.
        Assert.Equal(
            0,
            RegrasTier
                .Calcular(Peca(TipoEquipamento.Brinco, (StatusItem.Alcance, 60)), tabela)
                .Elegiveis
        );
        Assert.Equal(new[] { StatusItem.Alcance }, tabela.Elegiveis(TipoEquipamento.Peito));
        Assert.Empty(tabela.Elegiveis(TipoEquipamento.Brinco));
    }

    [Fact]
    public void Config_expoe_o_maximo_e_os_elegiveis_para_a_fusao()
    {
        foreach (var status in ConfigTier.Elegiveis(TipoEquipamento.Peito))
        {
            Assert.Equal(
                ConfigTier.Faixa(TipoEquipamento.Peito, status)!.Value.Maximo,
                ConfigTier.Maximo(TipoEquipamento.Peito, status)
            );
        }

        Assert.Equal(100, ConfigTier.Maximo(TipoEquipamento.Peito, StatusItem.Forca));
        Assert.Null(ConfigTier.Maximo(TipoEquipamento.Peito, StatusItem.PotenciaFogo));
        Assert.Contains(StatusItem.Defesa, ConfigTier.Elegiveis(TipoEquipamento.Anel));
    }

    [Fact]
    public void Letras_dos_tiers()
    {
        Assert.Equal(
            new[] { "D", "C", "B", "A", "S", "S+" },
            Enum.GetValues<TierItem>().Select(RegrasTier.Letra)
        );
    }

    [Theory]
    [InlineData(GrauEquipamento.Inicial, 0, 60)]
    [InlineData(GrauEquipamento.Medio, 20, 80)]
    [InlineData(GrauEquipamento.Forte, 40, 100)]
    public void Valor_sorteado_fica_na_janela_do_grau(GrauEquipamento grau, int minimo, int maximo)
    {
        var aleatorio = new Random(3);
        var faixa = new FaixaDeAtributo(0, 100);
        for (var i = 0; i < 2000; i++)
        {
            Assert.InRange(RegrasTier.SortearValor(aleatorio, faixa, grau, 1), minimo, maximo);
        }
    }

    [Fact]
    public void Mais_rolagens_melhoram_o_valor_sem_sair_da_faixa()
    {
        var faixa = new FaixaDeAtributo(10, 90);
        double Media(int rolagens)
        {
            var aleatorio = new Random(11);
            return Enumerable
                .Range(0, 4000)
                .Average(_ =>
                {
                    var v = RegrasTier.SortearValor(
                        aleatorio,
                        faixa,
                        GrauEquipamento.Forte,
                        rolagens
                    );
                    Assert.InRange(v, faixa.Minimo, faixa.Maximo);
                    return v;
                });
        }

        var uma = Media(1);
        var duas = Media(2);
        var quatro = Media(4);
        Assert.True(uma < duas && duas < quatro, $"{uma} {duas} {quatro}");
    }
}
