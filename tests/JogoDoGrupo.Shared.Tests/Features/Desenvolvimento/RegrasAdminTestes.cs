using JogoDoGrupo.Shared.Features.Desenvolvimento;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Shared.Tests.Features.Desenvolvimento;

public class RegrasAdminTestes
{
    private static PedirAcaoDeAdmin ItemValido() =>
        new()
        {
            Acao = AcaoDeAdmin.GerarItem,
            ModeloId = CatalogoEquipamentos.PrimeiroModelo,
            Grau = GrauEquipamento.Forte,
            Origem = OrigemDoLoot.Chefe,
            Quantidade = 1,
        };

    [Fact]
    public void Ids_de_mensagem_ficam_na_faixa()
    {
        Assert.True(
            DesenvolvimentoMessageIds.Faixa.Contem(DesenvolvimentoMessageIds.PedirAcaoDeAdmin)
        );
        Assert.True(
            DesenvolvimentoMessageIds.Faixa.Contem(DesenvolvimentoMessageIds.ResultadoDeAcaoDeAdmin)
        );
    }

    [Fact]
    public void Item_valido_passa()
    {
        Assert.Null(RegrasAdmin.Validar(ItemValido()));
    }

    [Fact]
    public void Pedido_nulo_ou_acao_fora_do_enum_e_recusado()
    {
        Assert.NotNull(RegrasAdmin.Validar(null!));
        Assert.NotNull(RegrasAdmin.Validar(new PedirAcaoDeAdmin { Acao = (AcaoDeAdmin)99 }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(ConfigAdmin.QuantidadeMaximaDeItens + 1)]
    public void Item_recusa_quantidade_fora_da_faixa(int quantidade)
    {
        var pedido = ItemValido();
        pedido.Quantidade = quantidade;
        Assert.NotNull(RegrasAdmin.Validar(pedido));
    }

    [Fact]
    public void Item_aceita_o_maximo_de_quantidade()
    {
        var pedido = ItemValido();
        pedido.Quantidade = ConfigAdmin.QuantidadeMaximaDeItens;
        Assert.Null(RegrasAdmin.Validar(pedido));
    }

    [Fact]
    public void Item_recusa_modelo_grau_e_origem_desconhecidos()
    {
        var modelo = ItemValido();
        modelo.ModeloId = 99999;
        Assert.NotNull(RegrasAdmin.Validar(modelo));

        var grau = ItemValido();
        grau.Grau = (GrauEquipamento)7;
        Assert.NotNull(RegrasAdmin.Validar(grau));

        var origem = ItemValido();
        origem.Origem = (OrigemDoLoot)9;
        Assert.NotNull(RegrasAdmin.Validar(origem));
    }

    [Theory]
    [InlineData(AcaoDeAdmin.AjustarExperiencia, 0, false)]
    [InlineData(AcaoDeAdmin.AjustarExperiencia, 500, true)]
    [InlineData(AcaoDeAdmin.AjustarExperiencia, -500, true)]
    [InlineData(AcaoDeAdmin.AjustarExperiencia, ConfigAdmin.ExperienciaMaxima + 1, false)]
    [InlineData(AcaoDeAdmin.AjustarCoroas, -ConfigAdmin.CoroasMaximas, true)]
    [InlineData(AcaoDeAdmin.AjustarCoroas, ConfigAdmin.CoroasMaximas + 1, false)]
    [InlineData(AcaoDeAdmin.CreditarEssencias, 10, true)]
    [InlineData(AcaoDeAdmin.CreditarEssencias, -1, false)]
    [InlineData(AcaoDeAdmin.CreditarEssencias, ConfigAdmin.EssenciasMaximas + 1, false)]
    public void Valor_respeita_a_faixa_de_cada_acao(AcaoDeAdmin acao, long valor, bool valido)
    {
        var recusa = RegrasAdmin.Validar(new PedirAcaoDeAdmin { Acao = acao, Valor = valor });
        Assert.Equal(valido, recusa == null);
    }

    [Fact]
    public void Teleporte_recusa_numero_nao_finito()
    {
        Assert.Null(
            RegrasAdmin.Validar(
                new PedirAcaoDeAdmin
                {
                    Acao = AcaoDeAdmin.Teleportar,
                    X = 1f,
                    Y = 2f,
                    Z = 3f,
                }
            )
        );
        Assert.NotNull(
            RegrasAdmin.Validar(
                new PedirAcaoDeAdmin { Acao = AcaoDeAdmin.Teleportar, X = float.NaN }
            )
        );
        Assert.NotNull(
            RegrasAdmin.Validar(
                new PedirAcaoDeAdmin { Acao = AcaoDeAdmin.Teleportar, Z = float.PositiveInfinity }
            )
        );
    }

    [Fact]
    public void Spawn_confere_especie_e_quantidade()
    {
        var pedido = new PedirAcaoDeAdmin
        {
            Acao = AcaoDeAdmin.SpawnarInimigo,
            EspecieId = CatalogoCriaturas.LoboDoCampo,
            Quantidade = ConfigAdmin.QuantidadeMaximaDeInimigos,
        };
        Assert.Null(RegrasAdmin.Validar(pedido));

        pedido.Quantidade += 1;
        Assert.NotNull(RegrasAdmin.Validar(pedido));

        pedido.Quantidade = 1;
        pedido.EspecieId = 9999;
        Assert.NotNull(RegrasAdmin.Validar(pedido));
    }

    [Theory]
    [InlineData(AcaoDeAdmin.MatarInimigosDaArea)]
    [InlineData(AcaoDeAdmin.LimparBolsa)]
    [InlineData(AcaoDeAdmin.EquiparUltimoItem)]
    [InlineData(AcaoDeAdmin.QuebrarEquipados)]
    [InlineData(AcaoDeAdmin.ConsertarEquipados)]
    public void Acoes_sem_campos_sempre_passam(AcaoDeAdmin acao)
    {
        Assert.Null(RegrasAdmin.Validar(new PedirAcaoDeAdmin { Acao = acao }));
    }

    [Fact]
    public void Todo_modelo_listado_existe_no_catalogo()
    {
        Assert.NotEmpty(RegrasAdmin.TodosOsModelos);
        foreach (var id in RegrasAdmin.TodosOsModelos)
        {
            Assert.NotNull(CatalogoEquipamentos.Obter(id));
        }
    }

    [Fact]
    public void Todo_tipo_de_equipamento_tem_ao_menos_um_modelo()
    {
        foreach (var tipo in System.Enum.GetValues<TipoEquipamento>())
        {
            var modelos = RegrasAdmin.ModelosDoTipo(tipo);
            Assert.NotEmpty(modelos);
            Assert.All(modelos, id => Assert.Equal(tipo, CatalogoEquipamentos.Obter(id)!.Tipo));
        }
    }
}
