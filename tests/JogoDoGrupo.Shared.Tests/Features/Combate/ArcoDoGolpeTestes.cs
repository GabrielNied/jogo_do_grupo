using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class ArcoDoGolpeTestes
{
    private const double Corpo = 0.5;
    private const double Altura = ArcosDosGolpes.AlturaPadraoDoCorpoMetros;

    private static bool Acerta(ArcoDoGolpe arco, double alvoX, double alvoZ, double direcao = 0) =>
        arco.Alcanca(0, 0, direcao, alvoX, alvoZ, Corpo, Altura);

    [Fact]
    public void Alvo_a_frente_dentro_do_alcance_e_acertado_em_todo_golpe()
    {
        foreach (var passo in new[] { 0, 1, 2 })
        {
            Assert.True(Acerta(ArcosDosGolpes.Obter(TipoDeAtaque.Leve, passo), 0, 1.5));
        }
        Assert.True(Acerta(ArcosDosGolpes.Pesado, 0, 1.5));
    }

    [Fact]
    public void Alvo_alem_do_alcance_mais_o_corpo_nao_e_acertado()
    {
        var arco = ArcosDosGolpes.Leve1;
        Assert.True(Acerta(arco, 0, arco.AlcanceMetros + Corpo - 0.01));
        Assert.False(Acerta(arco, 0, arco.AlcanceMetros + Corpo + 0.01));
    }

    [Fact]
    public void Alvo_ao_lado_dentro_do_cone_antigo_mas_fora_do_arco_nao_e_acertado()
    {
        // O terceiro leve é estreito (35 graus de cada lado); 40 graus ficava no cone de 45 graus.
        var arco = ArcosDosGolpes.Leve3;
        double x = Math.Sin(40 * Math.PI / 180) * 2.0;
        double z = Math.Cos(40 * Math.PI / 180) * 2.0;
        Assert.True(RegrasAtaque.EstaNoVolume(0, 0, 0, x, z));
        Assert.True(Acerta(arco, 0, 2.0));
        Assert.False(arco.Alcanca(0, 0, 0, x, z, 0.1, Altura));
    }

    [Fact]
    public void Arco_largo_pega_o_que_o_cone_antigo_nao_pegava()
    {
        // 60 graus ao lado: fora do cone de 90 graus, dentro do corte de 140 graus.
        double x = Math.Sin(60 * Math.PI / 180) * 1.8;
        double z = Math.Cos(60 * Math.PI / 180) * 1.8;
        Assert.False(RegrasAtaque.EstaNoVolume(0, 0, 0, x, z));
        Assert.True(Acerta(ArcosDosGolpes.Leve1, x, z));
    }

    [Fact]
    public void Dois_alvos_no_arco_levam_o_golpe()
    {
        var arco = ArcosDosGolpes.Leve1;
        Assert.True(Acerta(arco, 0, 1.0));
        Assert.True(Acerta(arco, 0, 2.0)); // atrás do primeiro, na mesma linha
        Assert.True(Acerta(arco, 1.0, 1.0));
    }

    [Fact]
    public void Alvo_atras_do_atacante_nao_e_acertado_e_a_direcao_gira_o_arco()
    {
        var arco = ArcosDosGolpes.Leve1;
        Assert.False(Acerta(arco, 0, -1.5));
        Assert.True(Acerta(arco, 0, -1.5, direcao: 180));
        Assert.True(Acerta(arco, 1.5, 0, direcao: 90));
    }

    [Fact]
    public void Corpo_largo_encosta_na_borda_do_leque()
    {
        var arco = ArcosDosGolpes.Leve1; // borda em 70 graus
        double x = Math.Sin(80 * Math.PI / 180) * 1.0;
        double z = Math.Cos(80 * Math.PI / 180) * 1.0;
        Assert.False(arco.Alcanca(0, 0, 0, x, z, 0.05, Altura));
        Assert.True(arco.Alcanca(0, 0, 0, x, z, 0.5, Altura));
    }

    [Fact]
    public void Faixa_de_altura_da_lamina_precisa_cruzar_o_corpo()
    {
        var arco = ArcosDosGolpes.Leve1; // lâmina de 0,3 m a 1,7 m
        Assert.True(arco.Alcanca(0, 0, 0, 0, 1, Corpo, 0.5));
        Assert.False(arco.Alcanca(0, 0, 0, 0, 1, Corpo, 0.2));
    }

    [Fact]
    public void Alvo_sobre_o_atacante_e_sempre_acertado()
    {
        Assert.True(Acerta(ArcosDosGolpes.Leve3, 0, 0));
    }

    [Fact]
    public void Dados_de_cada_golpe_sao_coerentes()
    {
        var todos = new[]
        {
            ArcosDosGolpes.Obter(TipoDeAtaque.Leve, 0),
            ArcosDosGolpes.Obter(TipoDeAtaque.Leve, 1),
            ArcosDosGolpes.Obter(TipoDeAtaque.Leve, 2),
            ArcosDosGolpes.Obter(TipoDeAtaque.Pesado, 0),
        };
        Assert.All(
            todos,
            a =>
            {
                Assert.True(a.AlcanceMetros > 0);
                Assert.True(a.AlturaMinimaMetros < a.AlturaMaximaMetros);
                Assert.True(a.InicioMs < 0 && a.FimMs > 0);
                Assert.NotEqual(a.AnguloInicialGraus, a.AnguloFinalGraus);
            }
        );
        // O leve 2 volta o corte do leve 1; o pesado é o mais largo.
        Assert.Equal(
            ArcosDosGolpes.Leve1.AnguloInicialGraus,
            ArcosDosGolpes.Leve2.AnguloFinalGraus
        );
        Assert.True(
            todos[3].AnguloMaximoGraus - todos[3].AnguloMinimoGraus
                > todos[0].AnguloMaximoGraus - todos[0].AnguloMinimoGraus
        );
        Assert.Equal(ArcosDosGolpes.Pesado, ArcosDosGolpes.Obter(TipoDeAtaque.Pesado, 2));
    }
}
