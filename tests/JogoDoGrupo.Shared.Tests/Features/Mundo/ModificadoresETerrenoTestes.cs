using System;
using JogoDoGrupo.Shared.Features.Mundo;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Mundo;

public class ModificadoresETerrenoTestes : IDisposable
{
    public ModificadoresETerrenoTestes()
    {
        ModificadoresDoMapa.LimparDinamicos();
    }

    public void Dispose()
    {
        ModificadoresDoMapa.LimparDinamicos();
    }

    [Fact]
    public void Transicoes_da_cidade_possuem_raios_e_suavidade_corretos()
    {
        Assert.Equal(60f, MapaDoMundo.RaioTransicao1);
        Assert.Equal(120f, MapaDoMundo.RaioTransicao2);

        // Dentro dos muros (até 40 m) é rigorosamente plano em zero
        Assert.Equal(0f, MapaDoMundo.AlturaEm(0f, 0f));
        Assert.Equal(0f, MapaDoMundo.AlturaEm(35f, 35f));

        // Transição 1 (40 a 60 m): a altura começa a subir suavemente
        var h45 = MapaDoMundo.AlturaEm(0f, 45f);
        var h55 = MapaDoMundo.AlturaEm(0f, 55f);
        Assert.True(h45 >= 0f, "Altura em 45m deve ser >= 0.");
        Assert.True(h55 >= h45, "Altura em 55m deve ser maior ou igual a 45m.");

        // Transição 2 (60 a 120 m): continua a conexão suave com o bioma
        var h70 = MapaDoMundo.AlturaEm(0f, 70f);
        var h100 = MapaDoMundo.AlturaEm(0f, 100f);
        Assert.True(h70 >= 0f);
        Assert.True(h100 >= 0f);
    }

    [Fact]
    public void Modificador_planar_achata_area_com_transicao_suave()
    {
        // Ponto de teste no campo onde a altura natural é diferente de 15f
        var x = 200f;
        var z = 200f;
        var alturaOriginal = MapaDoMundo.AlturaEm(x, z);
        var alturaForaOriginal = MapaDoMundo.AlturaEm(x + 50f, z);

        var mod = new ModificadorDeTerreno(
            id: "teste_praca",
            tipo: TipoModificadorTerreno.Planar,
            centroX: x,
            centroZ: z,
            raioPlano: 20f,
            raioSuavizacao: 10f,
            alturaAlvo: 15f
        );

        ModificadoresDoMapa.AdicionarDinamico(mod);

        // No centro e dentro do raio plano (20m), a altura deve ser exatamente a altura alvo
        Assert.Equal(15f, MapaDoMundo.AlturaEm(x, z), 0.05f);
        Assert.Equal(15f, MapaDoMundo.AlturaEm(x + 10f, z), 0.05f);

        // Fora do raio total (30m), o modificador não tem efeito
        Assert.Equal(alturaForaOriginal, MapaDoMundo.AlturaEm(x + 50f, z), 0.05f);

        // Na faixa de transição (entre 20m e 30m), a altura é intermediária
        var alturaTrans = MapaDoMundo.AlturaEm(x + 25f, z);
        Assert.True(
            (
                alturaTrans >= Math.Min(15f, alturaOriginal)
                && alturaTrans <= Math.Max(15f, alturaOriginal)
            )
                || Math.Abs(alturaTrans - 15f) < Math.Abs(alturaOriginal - 15f)
        );

        ModificadoresDoMapa.RemoverDinamico("teste_praca");
        Assert.Equal(alturaOriginal, MapaDoMundo.AlturaEm(x, z), 0.05f);
    }

    [Fact]
    public void Modificador_cavar_rebaixa_apenas_se_for_mais_alto()
    {
        var mod = new ModificadorDeTerreno(
            id: "trincheira",
            tipo: TipoModificadorTerreno.Cavar,
            centroX: 0f,
            centroZ: 0f,
            raioPlano: 10f,
            raioSuavizacao: 5f,
            alturaAlvo: -2f
        );

        // Chão em 10f cavado para -2f vira -2f
        var modificado = mod.Aplicar(0f, 0f, 10f);
        Assert.Equal(-2f, modificado, 0.001f);

        // Se o chão já estivesse em -5f, cavar para -2f não eleva
        var inalterado = mod.Aplicar(0f, 0f, -5f);
        Assert.Equal(-5f, inalterado, 0.001f);
    }

    [Fact]
    public void Modificador_elevar_sobe_apenas_se_for_mais_baixo()
    {
        var mod = new ModificadorDeTerreno(
            id: "aterro",
            tipo: TipoModificadorTerreno.Elevar,
            centroX: 0f,
            centroZ: 0f,
            raioPlano: 10f,
            raioSuavizacao: 5f,
            alturaAlvo: 8f
        );

        // Chão em 2f elevado para 8f vira 8f
        var modificado = mod.Aplicar(0f, 0f, 2f);
        Assert.Equal(8f, modificado, 0.001f);

        // Se o chão já estivesse em 12f, elevar para 8f não rebaixa
        var inalterado = mod.Aplicar(0f, 0f, 12f);
        Assert.Equal(12f, inalterado, 0.001f);
    }

    [Theory]
    [InlineData(45f, 0f, Bioma.Campo, false, TipoDeSuperficie.ParedaoRochoso)]
    [InlineData(50f, 10f, Bioma.Neve, false, TipoDeSuperficie.ParedaoRochoso)]
    [InlineData(20f, 0f, Bioma.Campo, true, TipoDeSuperficie.MargemDeRio)]
    [InlineData(0f, 0f, Bioma.Pantano, false, TipoDeSuperficie.Praia)]
    [InlineData(5f, 2f, Bioma.Pantano, false, TipoDeSuperficie.ChaoPadrao)]
    [InlineData(5f, 0f, Bioma.Campo, false, TipoDeSuperficie.ChaoPadrao)]
    public void Classificacao_de_superficie_atende_regras_geométricas(
        float inclinacao,
        float altura,
        Bioma bioma,
        bool proximoRio,
        TipoDeSuperficie esperado
    )
    {
        var tipo = ClassificacaoTerreno.Classificar(altura, inclinacao, bioma, proximoRio);
        Assert.Equal(esperado, tipo);
    }
}
