using JogoDoGrupo.Shared.Features.Instancias;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Instancias;

public sealed class RegrasDificuldadeSemFimTestes
{
    [Fact]
    public void Multiplicadores_de_vida_e_dano_escalam_8_por_cento_por_nivel()
    {
        Assert.Equal(1.0, RegrasDificuldadeSemFim.CalcularMultiplicadorVida(1), 3);
        Assert.Equal(1.08, RegrasDificuldadeSemFim.CalcularMultiplicadorVida(2), 3);
        Assert.Equal(1.32, RegrasDificuldadeSemFim.CalcularMultiplicadorVida(5), 3);
        Assert.Equal(1.72, RegrasDificuldadeSemFim.CalcularMultiplicadorVida(10), 3);

        Assert.Equal(1.0, RegrasDificuldadeSemFim.CalcularMultiplicadorDano(1), 3);
        Assert.Equal(1.08, RegrasDificuldadeSemFim.CalcularMultiplicadorDano(2), 3);
        Assert.Equal(1.32, RegrasDificuldadeSemFim.CalcularMultiplicadorDano(5), 3);
        Assert.Equal(1.72, RegrasDificuldadeSemFim.CalcularMultiplicadorDano(10), 3);

        // Níveis menores que o mínimo são ajustados para nível 1
        Assert.Equal(1.0, RegrasDificuldadeSemFim.CalcularMultiplicadorVida(0), 3);
        Assert.Equal(1.0, RegrasDificuldadeSemFim.CalcularMultiplicadorDano(-1), 3);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(450, true)]
    [InlineData(900, true)]
    [InlineData(901, false)]
    [InlineData(1200, false)]
    [InlineData(-1, false)]
    public void Venceu_no_tempo_respeita_limite_de_15_minutos(int segundos, bool esperado)
    {
        Assert.Equal(esperado, RegrasDificuldadeSemFim.VenceuNoTempo(segundos));
    }

    [Fact]
    public void Venceu_no_tempo_respeita_limite_customizado()
    {
        Assert.True(RegrasDificuldadeSemFim.VenceuNoTempo(600, tempoLimiteSegundos: 600));
        Assert.False(RegrasDificuldadeSemFim.VenceuNoTempo(601, tempoLimiteSegundos: 600));
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(-1, 0, false)]
    [InlineData(1, 0, true)]
    [InlineData(2, 0, false)]
    [InlineData(1, 1, true)]
    [InlineData(2, 1, true)]
    [InlineData(3, 1, false)]
    [InlineData(5, 5, true)]
    [InlineData(6, 5, true)]
    [InlineData(7, 5, false)]
    public void Nivel_permitido_so_autoriza_ate_um_acima_do_recorde(
        int nivelRequisitado,
        int recorde,
        bool esperado
    )
    {
        Assert.Equal(esperado, RegrasDificuldadeSemFim.NivelPermitido(nivelRequisitado, recorde));
    }

    [Fact]
    public void Calcular_novo_maior_nivel_atualiza_apenas_se_venceu_no_tempo()
    {
        // Vencer no tempo eleva o recorde
        Assert.Equal(1, RegrasDificuldadeSemFim.CalcularNovoMaiorNivel(1, 0, noTempo: true));
        Assert.Equal(2, RegrasDificuldadeSemFim.CalcularNovoMaiorNivel(2, 1, noTempo: true));

        // Fora do tempo mantém o recorde anterior
        Assert.Equal(0, RegrasDificuldadeSemFim.CalcularNovoMaiorNivel(1, 0, noTempo: false));
        Assert.Equal(1, RegrasDificuldadeSemFim.CalcularNovoMaiorNivel(2, 1, noTempo: false));

        // Nível inferior ao recorde não reduz o recorde mesmo vencendo
        Assert.Equal(5, RegrasDificuldadeSemFim.CalcularNovoMaiorNivel(2, 5, noTempo: true));
    }
}
