using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Shared.Tests.Features.Mundo;

public class RegrasDiaENoiteTestes
{
    private const double Dia = RegrasDiaENoite.DuracaoDoDiaSegundos;

    [Fact]
    public void Valores_padrao_de_balanceamento()
    {
        Assert.Equal(24f, RegrasDiaENoite.HorasPorDia);
        Assert.Equal(1200f, RegrasDiaENoite.DuracaoDoDiaSegundos);
        Assert.Equal(8f, RegrasDiaENoite.HoraInicial);
        Assert.Equal(20f, RegrasDiaENoite.InicioDaNoite);
        Assert.Equal(5f, RegrasDiaENoite.FimDaNoite);
    }

    [Fact]
    public void O_servidor_liga_de_dia() =>
        Assert.False(RegrasDiaENoite.EhNoite(RegrasDiaENoite.HoraInicial));

    [Fact]
    public void HoraDoMundo_comeca_na_hora_inicial_com_a_duracao_padrao()
    {
        var msg = new HoraDoMundo();
        Assert.Equal(RegrasDiaENoite.HoraInicial, msg.Hora);
        Assert.Equal(RegrasDiaENoite.DuracaoDoDiaSegundos, msg.DuracaoDoDiaSegundos);
    }

    [Fact]
    public void Id_da_hora_do_mundo_e_o_do_contrato_e_esta_na_faixa()
    {
        Assert.Equal(1011, MundoMessageIds.HoraDoMundo);
        Assert.True(MundoMessageIds.Faixa.Contem(MundoMessageIds.HoraDoMundo));
    }

    [Theory]
    [InlineData(0.0, 0f)]
    [InlineData(12.5, 12.5f)]
    [InlineData(23.99, 23.99f)]
    [InlineData(24.0, 0f)]
    [InlineData(25.0, 1f)]
    [InlineData(49.0, 1f)]
    [InlineData(-1.0, 23f)]
    [InlineData(-25.0, 23f)]
    public void Normalizar_traz_a_hora_para_a_faixa_do_dia(double hora, float esperado) =>
        Assert.Equal(esperado, RegrasDiaENoite.Normalizar(hora), 3);

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Normalizar_de_valor_nao_finito_vira_a_hora_inicial(double hora) =>
        Assert.Equal(RegrasDiaENoite.HoraInicial, RegrasDiaENoite.Normalizar(hora));

    [Fact]
    public void Normalizar_nunca_devolve_24()
    {
        // Logo abaixo de 24 em double pode arredondar para 24 em float.
        var hora = RegrasDiaENoite.Normalizar(23.99999999999);
        Assert.InRange(hora, 0f, 23.9999f);
    }

    [Theory]
    [InlineData(8.0, 0.0, 8f)]
    [InlineData(8.0, Dia / 24, 9f)] // uma hora do mundo = 50 s reais
    [InlineData(8.0, Dia / 2, 20f)]
    [InlineData(20.0, Dia / 2, 8f)] // dá a volta à meia-noite
    [InlineData(23.0, Dia / 12, 1f)]
    [InlineData(8.0, Dia, 8f)] // um dia inteiro volta à mesma hora
    [InlineData(8.0, Dia * 10 + Dia / 4, 14f)] // muitos dias depois
    public void HoraApos_avanca_no_ritmo_do_dia(double hora, double segundos, float esperado) =>
        Assert.Equal(esperado, RegrasDiaENoite.HoraApos(hora, segundos), 2);

    [Fact]
    public void Um_dia_completo_passa_por_todas_as_horas_sem_voltar()
    {
        var anterior = RegrasDiaENoite.HoraApos(0, 0);
        Assert.Equal(0f, anterior);
        for (var passo = 1; passo < 240; passo++)
        {
            var hora = RegrasDiaENoite.HoraApos(0, passo * Dia / 240);
            Assert.True(hora > anterior, $"a hora voltou no passo {passo}");
            anterior = hora;
        }

        Assert.InRange(anterior, 23.8f, 23.9999f);
    }

    [Theory]
    [InlineData(-10.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void HoraApos_com_tempo_invalido_nao_anda(double segundos) =>
        Assert.Equal(8f, RegrasDiaENoite.HoraApos(8, segundos));

    [Fact]
    public void HoraApos_usa_a_duracao_recebida()
    {
        Assert.Equal(20f, RegrasDiaENoite.HoraApos(8, 30, 60), 3); // dia de 1 min
        Assert.Equal(9f, RegrasDiaENoite.HoraApos(8, 100, 2400), 3); // dia de 40 min
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-60.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Duracao_invalida_cai_na_padrao(double duracao)
    {
        Assert.Equal(Dia, RegrasDiaENoite.DuracaoValida(duracao));
        Assert.Equal(9f, RegrasDiaENoite.HoraApos(8, Dia / 24, duracao), 2);
    }

    [Theory]
    [InlineData(20.0, true)] // as lanternas acendem
    [InlineData(23.5, true)]
    [InlineData(0.0, true)]
    [InlineData(4.99, true)]
    [InlineData(5.0, false)] // e apagam de manhã
    [InlineData(12.0, false)]
    [InlineData(19.99, false)]
    [InlineData(44.0, true)] // 20h do dia seguinte
    [InlineData(-2.0, true)] // 22h
    public void EhNoite_vale_das_20h_as_5h(double hora, bool esperado) =>
        Assert.Equal(esperado, RegrasDiaENoite.EhNoite(hora));

    [Theory]
    [InlineData(0.0, 0f)]
    [InlineData(6.0, 0.25f)]
    [InlineData(12.0, 0.5f)]
    [InlineData(18.0, 0.75f)]
    [InlineData(36.0, 0.5f)]
    public void FracaoDoDia_vai_de_zero_a_um(double hora, float esperado) =>
        Assert.Equal(esperado, RegrasDiaENoite.FracaoDoDia(hora), 4);
}
