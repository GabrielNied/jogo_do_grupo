using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Shared.Tests.Features.Mundo;

public class RegiaoDeBiomaTestes
{
    [Fact]
    public void Contorno_fechado_tem_interior_exterior_e_borda_gradual()
    {
        var regiao = Quadrado(Bioma.Mata, 20f);

        Assert.Equal(1f, regiao.CoberturaEm(0f, 0f));
        Assert.Equal(0f, regiao.CoberturaEm(30f, 0f));
        Assert.InRange(regiao.CoberturaEm(10f, 0f), 0.49f, 0.51f);
        Assert.True(regiao.CoberturaEm(5f, 0f) > regiao.CoberturaEm(15f, 0f));
    }

    [Fact]
    public void Pintura_mais_recente_substitui_a_anterior_no_centro()
    {
        var baseDoMundo = new PesosDosBiomas(1f, 0f, 0f, 0f, 0f, 0f, 0f);
        var mata = Quadrado(Bioma.Mata, 20f);
        var neve = Quadrado(Bioma.Neve, 20f);

        var anterior = mata.Aplicar(baseDoMundo, mata.CoberturaEm(0f, 0f));
        var final = neve.Aplicar(anterior, neve.CoberturaEm(0f, 0f));

        Assert.Equal(Bioma.Neve, final.Dominante);
        Assert.Equal(1f, final.Neve);
        Assert.Equal(0f, final.Mata);
    }

    [Fact]
    public void Contorno_nao_muda_ao_editar_o_array_de_entrada_ou_a_copia_de_saida()
    {
        var pontos = Pontos();
        var regiao = new RegiaoDeBioma("Mata", Bioma.Mata, pontos, 20f, 1f, 1f);
        pontos[0] = new RegiaoDeBioma.Ponto(100f, 100f);
        var copia = regiao.Pontos;
        copia[1] = new RegiaoDeBioma.Ponto(100f, 100f);

        Assert.Equal(1f, regiao.CoberturaEm(0f, 0f));
    }

    [Fact]
    public void Nao_aceita_regiao_sem_area_ou_fora_do_mundo()
    {
        Assert.Throws<ArgumentException>(() =>
            new RegiaoDeBioma(
                "Mata",
                Bioma.Mata,
                [
                    new RegiaoDeBioma.Ponto(0f, 0f),
                    new RegiaoDeBioma.Ponto(1f, 0f),
                    new RegiaoDeBioma.Ponto(2f, 0f),
                ],
                20f,
                1f,
                1f
            )
        );
        var pontos = Pontos();
        pontos[0] = new RegiaoDeBioma.Ponto(1600f, 0f);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RegiaoDeBioma("Mata", Bioma.Mata, pontos, 20f, 1f, 1f)
        );
    }

    private static RegiaoDeBioma Quadrado(Bioma bioma, float borda) =>
        new("Teste", bioma, Pontos(), borda, 1f, 1f);

    private static RegiaoDeBioma.Ponto[] Pontos() =>
        [new(-10f, -10f), new(10f, -10f), new(10f, 10f), new(-10f, 10f)];
}
