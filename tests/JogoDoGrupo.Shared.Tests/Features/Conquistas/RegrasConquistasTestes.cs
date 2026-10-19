using JogoDoGrupo.Shared.Features.Conquistas;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Conquistas;

public class RegrasConquistasTestes
{
    [Fact]
    public void Faixa_de_ids_comeca_em_18000()
    {
        Assert.Equal(18000, ConquistasMessageIds.Faixa.Inicio);
        Assert.Equal(18999, ConquistasMessageIds.Faixa.Fim);
    }

    [Fact]
    public void Catalogo_possui_as_seis_conquistas_basicas()
    {
        Assert.Equal(6, CatalogoDeConquistas.Todas.Count);

        Assert.True(
            CatalogoDeConquistas.TentarObter(CatalogoDeConquistas.PrimeirosPassos, out var passos)
        );
        Assert.Equal(10, passos.Pontos);
        Assert.Equal("O Iniciante", passos.TituloRecompensa);

        Assert.True(
            CatalogoDeConquistas.TentarObter(CatalogoDeConquistas.OFimDoPredador, out var predador)
        );
        Assert.Equal(25, predador.Pontos);
        Assert.Equal("Caçador de Lobos", predador.TituloRecompensa);
    }

    [Fact]
    public void PodeEquiparTitulo_permite_vazio_ou_nulo_para_desequipar()
    {
        string[] titulos = ["O Explorador", "O Iniciante"];
        Assert.True(RegrasConquistas.PodeEquiparTitulo("", titulos));
        Assert.True(RegrasConquistas.PodeEquiparTitulo("   ", titulos));
        Assert.True(RegrasConquistas.PodeEquiparTitulo(null!, titulos));
    }

    [Fact]
    public void PodeEquiparTitulo_retorna_false_se_titulo_nao_foi_desbloqueado()
    {
        string[] titulos = ["O Iniciante"];
        Assert.False(RegrasConquistas.PodeEquiparTitulo("Lorde Supremo", titulos));
        Assert.False(RegrasConquistas.PodeEquiparTitulo("O Iniciante", []));
        Assert.False(RegrasConquistas.PodeEquiparTitulo("O Iniciante", null!));
    }

    [Fact]
    public void PodeEquiparTitulo_retorna_true_quando_titulo_pertence_a_lista()
    {
        string[] titulos = ["O Iniciante", "Caçador de Lobos"];
        Assert.True(RegrasConquistas.PodeEquiparTitulo("O Iniciante", titulos));
        Assert.True(RegrasConquistas.PodeEquiparTitulo("caçador de lobos", titulos));
    }

    [Fact]
    public void SomarPontos_ignora_conquistas_pendentes_e_soma_concluidas()
    {
        var progressos = new[]
        {
            new ProgressoConquista
            {
                ConquistaId = CatalogoDeConquistas.PrimeirosPassos,
                ProgressoAtual = 2,
                Meta = 2,
                Concluida = true,
            },
            new ProgressoConquista
            {
                ConquistaId = CatalogoDeConquistas.MatadorDeMobs,
                ProgressoAtual = 5,
                Meta = 10,
                Concluida = false,
            },
            new ProgressoConquista
            {
                ConquistaId = CatalogoDeConquistas.OFimDoPredador,
                ProgressoAtual = 1,
                Meta = 1,
                Concluida = true,
            },
        };

        // PrimeirosPassos (10) + OFimDoPredador (25) = 35
        var pontos = RegrasConquistas.SomarPontos(progressos);
        Assert.Equal(35, pontos);
    }
}
