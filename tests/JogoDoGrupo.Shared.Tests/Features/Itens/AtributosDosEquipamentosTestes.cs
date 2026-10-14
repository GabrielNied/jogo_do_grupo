using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

/// <summary>
/// Atributos próprios dos equipamentos (#623): a conversão linear por (atributo, categoria), a
/// matriz de elegíveis e o resumo do que as peças ativas dão ao combate, separado do personagem.
/// </summary>
public class AtributosDosEquipamentosTestes
{
    [Theory]
    // Dano da arma: cada arma tem a sua faixa (o 0 e o 100 estão nos dados).
    [InlineData(TipoEquipamento.Espada, StatusItem.DanoDaArma, 10, 30)]
    [InlineData(TipoEquipamento.Martelo, StatusItem.DanoDaArma, 14, 44)]
    [InlineData(TipoEquipamento.Arco, StatusItem.DanoDaArma, 8, 28)]
    [InlineData(TipoEquipamento.Cajado, StatusItem.DanoDaArma, 10, 30)]
    // A mesma Força vale diferente conforme a categoria.
    [InlineData(TipoEquipamento.Espada, StatusItem.DanoFisico, 0, 20)]
    [InlineData(TipoEquipamento.Martelo, StatusItem.DanoFisico, 0, 25)]
    [InlineData(TipoEquipamento.Anel, StatusItem.DanoFisico, 0, 15)]
    [InlineData(TipoEquipamento.Peito, StatusItem.VidaMaxima, 0, 250)]
    [InlineData(TipoEquipamento.Cinto, StatusItem.VidaMaxima, 0, 150)]
    [InlineData(TipoEquipamento.Botas, StatusItem.RegeneracaoStamina, 0, 5)]
    [InlineData(TipoEquipamento.Foco, StatusItem.RegeneracaoMana, 0, 3)]
    [InlineData(TipoEquipamento.Capacete, StatusItem.DefesaFisica, 0, 50)]
    [InlineData(TipoEquipamento.Escudo, StatusItem.DefesaMagica, 0, 40)]
    [InlineData(TipoEquipamento.Anel, StatusItem.ChanceCritica, 0, 0.15)]
    [InlineData(TipoEquipamento.Brinco, StatusItem.DanoCritico, 0, 0.75)]
    [InlineData(TipoEquipamento.Calca, StatusItem.RegeneracaoVida, 0, 2)]
    public void Conversao_linear_vale_no0_em_0_e_no100_em_100(
        TipoEquipamento tipo,
        StatusItem status,
        double no0,
        double no100
    )
    {
        Assert.Equal(no0, ConfigAtributosItem.Converter(tipo, status, 0), 9);
        Assert.Equal(no100, ConfigAtributosItem.Converter(tipo, status, 100), 9);
        Assert.Equal((no0 + no100) / 2, ConfigAtributosItem.Converter(tipo, status, 50), 9);
        // Fora da escala, limita (a fusão já respeita o teto, mas a conta não extrapola).
        Assert.Equal(no100, ConfigAtributosItem.Converter(tipo, status, 250), 9);
        Assert.Equal(no0, ConfigAtributosItem.Converter(tipo, status, -5), 9);
    }

    [Fact]
    public void Atributo_inelegivel_na_categoria_ou_legado_nao_vale_no_combate()
    {
        // Força não existe em escudo nem em cinto; Destreza e Defesa são do modelo anterior.
        Assert.Equal(
            0,
            ConfigAtributosItem.Converter(TipoEquipamento.Escudo, StatusItem.DanoFisico, 80)
        );
        Assert.Equal(
            0,
            ConfigAtributosItem.Converter(TipoEquipamento.Cinto, StatusItem.DanoFisico, 80)
        );
        Assert.Equal(
            0,
            ConfigAtributosItem.Converter(TipoEquipamento.Peito, StatusItem.Destreza, 80)
        );
        Assert.Equal(
            0,
            ConfigAtributosItem.Converter(TipoEquipamento.Peito, StatusItem.Defesa, 80)
        );
        Assert.Null(ConfigTier.Faixa(TipoEquipamento.Escudo, StatusItem.DanoFisico));
    }

    [Fact]
    public void Matriz_de_elegiveis_segue_a_doc_e_a_faixa_do_tier_sai_dela()
    {
        Assert.Equal(
            new[]
            {
                StatusItem.DanoDaArma,
                StatusItem.DanoFisico,
                StatusItem.ChanceCritica,
                StatusItem.PenetracaoFisica,
            },
            ConfigAtributosItem.Elegiveis(TipoEquipamento.Espada)
        );
        Assert.Contains(
            StatusItem.DefesaMagica,
            ConfigAtributosItem.Elegiveis(TipoEquipamento.Adorno)
        );
        Assert.DoesNotContain(
            StatusItem.DanoDaArma,
            ConfigAtributosItem.Elegiveis(TipoEquipamento.Anel)
        );
        foreach (var tipo in Enum.GetValues<TipoEquipamento>())
        {
            var elegiveis = ConfigAtributosItem.Elegiveis(tipo);
            Assert.NotEmpty(elegiveis);
            // Elegível na matriz = tem faixa de 0 a 100; o resto não tem.
            Assert.Equal(elegiveis.OrderBy(s => s), ConfigTier.Elegiveis(tipo).OrderBy(s => s));
            foreach (var status in elegiveis)
                Assert.Equal(100, ConfigTier.Maximo(tipo, status));
        }
    }

    private static EntradaInventario Peca(
        PosicaoEquipamento posicao,
        TipoEquipamento tipo,
        params (StatusItem Status, double Valor)[] atributos
    ) =>
        new()
        {
            Posicao = posicao,
            Equipamento = new Equipamento
            {
                ModeloId = 1,
                Tipo = tipo,
                Nome = tipo.ToString(),
                Atributos = atributos
                    .Select(a => new AtributoItem { Status = a.Status, Valor = a.Valor })
                    .ToArray(),
            },
        };

    [Fact]
    public void Resumo_separa_o_que_cada_fonte_da_e_nada_vira_atributo_do_personagem()
    {
        var itens = new[]
        {
            Peca(
                PosicaoEquipamento.Arma1,
                TipoEquipamento.Espada,
                (StatusItem.DanoDaArma, 50),
                (StatusItem.DanoFisico, 100),
                (StatusItem.ChanceCritica, 40)
            ),
            Peca(
                PosicaoEquipamento.Peito,
                TipoEquipamento.Peito,
                (StatusItem.VidaMaxima, 80),
                (StatusItem.DanoFisico, 50),
                (StatusItem.DefesaFisica, 50)
            ),
            Peca(
                PosicaoEquipamento.Cinto,
                TipoEquipamento.Cinto,
                (StatusItem.RegeneracaoMana, 100)
            ),
        };

        var resumo = RegrasAtributosDeItem.Resumir(itens, 1);

        Assert.True(resumo.TemArma);
        Assert.False(resumo.ArmaMagica);
        Assert.Equal(20, resumo.DanoDaArma, 9); // espada: 10 a 30, 50 vale 20
        Assert.Equal(20 + 7.5, resumo.BonusDanoFisico, 9); // espada 100 = 20; peito 50 = 7,5
        Assert.Equal(200, resumo.VidaMaxima); // peito: vida 0 a 250, 80 vale 200
        Assert.Equal(30, resumo.DefesaFisica, 9); // peito: 0 a 60, 50 vale 30
        // Chance crítica é secundário: vai pela pipeline, não por aqui.
        Assert.DoesNotContain(resumo.Linhas, l => l.Status == StatusItem.ChanceCritica);
        Assert.Contains(resumo.Linhas, l => l.Status == StatusItem.DanoDaArma);
    }

    [Fact]
    public void Cajado_da_dano_magico_e_so_a_arma_do_conjunto_ativo_conta()
    {
        var itens = new[]
        {
            Peca(
                PosicaoEquipamento.Arma1,
                TipoEquipamento.Cajado,
                (StatusItem.DanoDaArma, 100),
                (StatusItem.DanoMagico, 50)
            ),
            Peca(PosicaoEquipamento.Arma2, TipoEquipamento.Espada, (StatusItem.DanoDaArma, 100)),
            Peca(PosicaoEquipamento.Inventario, TipoEquipamento.Anel, (StatusItem.DanoFisico, 100)),
        };

        var conjunto1 = RegrasAtributosDeItem.Resumir(itens, 1);
        var conjunto2 = RegrasAtributosDeItem.Resumir(itens, 2);

        Assert.True(conjunto1.ArmaMagica);
        Assert.Equal(30, conjunto1.DanoDaArma, 9);
        Assert.Equal(10, conjunto1.BonusDanoMagico, 9);
        Assert.Equal(0, conjunto1.BonusDanoFisico); // o anel está na bolsa
        Assert.False(conjunto2.ArmaMagica);
        Assert.Equal(30, conjunto2.DanoDaArma, 9);
        Assert.Equal(0, conjunto2.BonusDanoMagico);
    }

    [Fact]
    public void Dano_da_arma_so_vale_na_mao_principal_e_sem_arma_nao_ha_dano_do_item()
    {
        var itens = new[]
        {
            // Dano da arma num escudo (dado inválido): o escudo nem tem esse atributo na matriz.
            Peca(
                PosicaoEquipamento.Secundaria1,
                TipoEquipamento.Escudo,
                (StatusItem.DanoDaArma, 100)
            ),
            Peca(
                PosicaoEquipamento.Capacete,
                TipoEquipamento.Capacete,
                (StatusItem.DanoFisico, 100)
            ),
        };

        var resumo = RegrasAtributosDeItem.Resumir(itens, 1);

        Assert.False(resumo.TemArma);
        Assert.Equal(0, resumo.DanoDaArma);
        Assert.Equal(15, resumo.BonusDanoFisico, 9);
    }

    [Fact]
    public void Textos_dizem_o_efeito_convertido()
    {
        Assert.Equal(
            "+14,4 de dano físico",
            RegrasAtributosDeItem.Efeito(StatusItem.DanoFisico, 14.4)
        );
        Assert.Equal(
            "+120 de vida máxima",
            RegrasAtributosDeItem.Efeito(StatusItem.VidaMaxima, 120)
        );
        Assert.Equal("+3/s", RegrasAtributosDeItem.Efeito(StatusItem.RegeneracaoMana, 3));
        Assert.Equal(
            "24,4 de dano mágico",
            RegrasAtributosDeItem.Efeito(StatusItem.DanoDaArma, 24.4, true)
        );
    }
}
