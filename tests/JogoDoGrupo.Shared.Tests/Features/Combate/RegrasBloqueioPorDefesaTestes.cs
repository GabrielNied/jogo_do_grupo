using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class RegrasBloqueioPorDefesaTestes
{
    [Fact]
    public void Id_defesa_insuficiente_esta_na_faixa_do_combate() =>
        Assert.True(CombateMessageIds.Faixa.Contem(CombateMessageIds.DefesaInsuficiente));

    [Fact]
    public void Constantes_seguem_a_regra_decidida()
    {
        Assert.Equal(1.5, RegrasBloqueioPorDefesa.MultiplicadorDefesaParaAnular);
        Assert.Equal(0.80, RegrasBloqueioPorDefesa.ReducaoMaximaProporcional);
    }

    [Theory]
    [InlineData(150, 100, true)]
    [InlineData(151, 100, true)]
    [InlineData(300, 100, true)]
    [InlineData(149.9, 100, false)]
    [InlineData(75, 100, false)]
    [InlineData(0, 100, false)]
    [InlineData(0, 0, true)]
    [InlineData(50, -10, true)]
    public void Anula_quando_defesa_e_pelo_menos_um_e_meio_vezes_a_potencia(
        double defesa,
        double potencia,
        bool esperado
    )
    {
        Assert.Equal(esperado, RegrasBloqueioPorDefesa.AnulaCompletamente(defesa, potencia));
    }

    [Fact]
    public void Fracao_de_reducao_proporcional_ate_oitenta_por_cento()
    {
        // Defesa zero -> 0% de redução
        Assert.Equal(0.0, RegrasBloqueioPorDefesa.FracaoDeReducao(0, 100));

        // Metade do limiar (75 de 150) -> 50% de 80% = 40% de redução
        Assert.Equal(0.40, RegrasBloqueioPorDefesa.FracaoDeReducao(75, 100), precision: 5);

        // Logo abaixo do limiar (149 de 150) -> próximo de 80%
        var fracaoQuase = RegrasBloqueioPorDefesa.FracaoDeReducao(149, 100);
        Assert.True(fracaoQuase > 0.79 && fracaoQuase < 0.80);

        // No limiar ou acima -> 1.0 (anulação)
        Assert.Equal(1.0, RegrasBloqueioPorDefesa.FracaoDeReducao(150, 100));
        Assert.Equal(1.0, RegrasBloqueioPorDefesa.FracaoDeReducao(200, 100));
    }

    [Theory]
    [InlineData(100, 0, 100, 100)] // Sem defesa: dano total
    [InlineData(100, 75, 100, 60)] // 50% do limiar: 40% de redução -> 60 de dano
    [InlineData(100, 149, 100, 21)] // Quase no limiar: ~79.47% de redução -> 21 de dano
    [InlineData(100, 150, 100, 0)] // Limiar exato (1,5x): anulação -> 0
    [InlineData(100, 250, 100, 0)] // Acima do limiar: anulação -> 0
    [InlineData(1, 140, 100, 1)] // Sem anulação: golpe pequeno não zera abaixo de 1,5x
    [InlineData(0, 50, 100, 0)] // Dano inicial zero continua zero
    [InlineData(-10, 50, 100, 0)] // Dano negativo normaliza para zero
    public void Dano_final_respeita_reducao_proporcional_e_anulacao(
        int danoOriginal,
        double defesa,
        double potencia,
        int esperado
    )
    {
        Assert.Equal(
            esperado,
            RegrasBloqueioPorDefesa.CalcularDanoFinal(danoOriginal, defesa, potencia)
        );
    }
}
