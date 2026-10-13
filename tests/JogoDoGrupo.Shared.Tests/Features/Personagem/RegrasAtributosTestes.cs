using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Shared.Tests.Features.Personagem;

public class RegrasAtributosTestes
{
    private static PontosDeAtributos Pontos(
        int forca = 0,
        int agilidade = 0,
        int intelecto = 0,
        int vigor = 0,
        int espirito = 0
    ) =>
        new PontosDeAtributos
        {
            Forca = forca,
            Agilidade = agilidade,
            Intelecto = intelecto,
            Vigor = vigor,
            Espirito = espirito,
        };

    [Fact]
    public void Total_soma_os_cinco_atributos() =>
        Assert.Equal(15L, RegrasAtributos.Total(Pontos(1, 2, 3, 4, 5)));

    [Fact]
    public void Total_nao_estoura_com_valores_enormes()
    {
        const int Max = int.MaxValue;
        Assert.Equal(5L * Max, RegrasAtributos.Total(Pontos(Max, Max, Max, Max, Max)));
    }

    [Fact]
    public void Gastar_exatamente_os_pontos_livres_vale() =>
        Assert.Null(RegrasAtributos.ValidarDistribuicao(5, Pontos(forca: 3, vigor: 2)));

    [Fact]
    public void Gastar_menos_que_os_pontos_livres_vale() =>
        Assert.Null(RegrasAtributos.ValidarDistribuicao(5, Pontos(espirito: 1)));

    [Fact]
    public void Um_ponto_a_mais_e_recusado() =>
        Assert.Equal(
            CodigosErroPersonagem.PontosInsuficientes,
            RegrasAtributos.ValidarDistribuicao(5, Pontos(forca: 3, vigor: 3))
        );

    [Fact]
    public void Sem_pontos_livres_qualquer_gasto_e_recusado() =>
        Assert.Equal(
            CodigosErroPersonagem.PontosInsuficientes,
            RegrasAtributos.ValidarDistribuicao(0, Pontos(agilidade: 1))
        );

    [Fact]
    public void Soma_que_estouraria_int_e_recusada_por_falta_de_pontos() =>
        Assert.Equal(
            CodigosErroPersonagem.PontosInsuficientes,
            RegrasAtributos.ValidarDistribuicao(
                int.MaxValue,
                Pontos(forca: int.MaxValue, agilidade: int.MaxValue)
            )
        );

    [Theory]
    [InlineData(-1, 0, 0, 0, 0)]
    [InlineData(0, -1, 0, 0, 0)]
    [InlineData(0, 0, -1, 0, 0)]
    [InlineData(0, 0, 0, -1, 0)]
    [InlineData(0, 0, 0, 0, -1)]
    [InlineData(3, -1, 0, 0, 0)] // negativo não "devolve" ponto para gastar em outro
    public void Valor_negativo_e_atributo_invalido(int f, int d, int i, int v, int e) =>
        Assert.Equal(
            CodigosErroPersonagem.AtributoInvalido,
            RegrasAtributos.ValidarDistribuicao(5, Pontos(f, d, i, v, e))
        );

    [Fact]
    public void Pedido_sem_ponto_nenhum_e_atributo_invalido() =>
        Assert.Equal(
            CodigosErroPersonagem.AtributoInvalido,
            RegrasAtributos.ValidarDistribuicao(5, Pontos())
        );

    [Fact]
    public void Pedido_nulo_e_atributo_invalido() =>
        Assert.Equal(
            CodigosErroPersonagem.AtributoInvalido,
            RegrasAtributos.ValidarDistribuicao(5, null)
        );

    [Fact]
    public void Negativo_vence_a_falta_de_pontos() =>
        Assert.Equal(
            CodigosErroPersonagem.AtributoInvalido,
            RegrasAtributos.ValidarDistribuicao(0, Pontos(forca: 10, agilidade: -1))
        );
}
