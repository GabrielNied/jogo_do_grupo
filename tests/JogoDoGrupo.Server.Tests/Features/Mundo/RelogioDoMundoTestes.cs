using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

/// <summary>O relógio do mundo sem rede, com o tempo controlado pelo teste.</summary>
public class RelogioDoMundoTestes
{
    private const double Dia = RegrasDiaENoite.DuracaoDoDiaSegundos;
    private const double UmaHora = Dia / 24;

    [Fact]
    public void Comeca_na_hora_inicial_quando_o_servidor_liga()
    {
        var mundo = new RelogioDoMundo(new RelogioFalso());

        Assert.Equal(RegrasDiaENoite.HoraInicial, mundo.Hora());
    }

    [Fact]
    public void A_hora_anda_com_o_tempo_e_nao_com_as_leituras()
    {
        var tempo = new RelogioFalso();
        var mundo = new RelogioDoMundo(tempo);

        Assert.Equal(mundo.Hora(), mundo.Hora()); // Ler não avança.

        tempo.Avancar(UmaHora);
        Assert.Equal(RegrasDiaENoite.HoraInicial + 1, mundo.Hora(), 3);
        tempo.Avancar(UmaHora * 2.5);
        Assert.Equal(RegrasDiaENoite.HoraInicial + 3.5f, mundo.Hora(), 3);
    }

    [Fact]
    public void Um_dia_completo_da_a_volta_e_chega_a_mesma_hora()
    {
        var tempo = new RelogioFalso();
        var mundo = new RelogioDoMundo(tempo);

        tempo.Avancar(Dia / 2);
        Assert.Equal(RegrasDiaENoite.HoraInicial + 12, mundo.Hora(), 3);
        Assert.True(RegrasDiaENoite.EhNoite(mundo.Hora()));

        tempo.Avancar(Dia / 2);
        Assert.Equal(RegrasDiaENoite.HoraInicial, mundo.Hora(), 3);
        Assert.False(RegrasDiaENoite.EhNoite(mundo.Hora()));
    }

    [Fact]
    public void Passa_da_meia_noite_sem_sair_da_faixa()
    {
        var tempo = new RelogioFalso();
        var mundo = new RelogioDoMundo(tempo);

        tempo.Avancar(UmaHora * 17); // 8h + 17h = 1h do dia seguinte

        Assert.Equal(1f, mundo.Hora(), 3);
    }

    [Fact]
    public void Muitos_dias_depois_a_hora_continua_na_faixa()
    {
        var tempo = new RelogioFalso();
        var mundo = new RelogioDoMundo(tempo);

        tempo.Avancar(Dia * 365 + UmaHora);

        Assert.Equal(RegrasDiaENoite.HoraInicial + 1, mundo.Hora(), 1);
    }

    [Fact]
    public void Dois_relogios_ligados_em_momentos_diferentes_nao_se_misturam()
    {
        var tempo = new RelogioFalso();
        var primeiro = new RelogioDoMundo(tempo);
        tempo.Avancar(UmaHora * 3);
        var segundo = new RelogioDoMundo(tempo);

        Assert.Equal(RegrasDiaENoite.HoraInicial + 3, primeiro.Hora(), 3);
        Assert.Equal(RegrasDiaENoite.HoraInicial, segundo.Hora(), 3);
    }

    [Fact]
    public void Mensagem_leva_a_hora_de_agora_e_a_duracao_do_dia()
    {
        var tempo = new RelogioFalso();
        var mundo = new RelogioDoMundo(tempo);
        tempo.Avancar(UmaHora * 4);

        var mensagem = mundo.Mensagem();

        Assert.Equal(mundo.Hora(), mensagem.Hora);
        Assert.Equal(RegrasDiaENoite.DuracaoDoDiaSegundos, mensagem.DuracaoDoDiaSegundos);
    }
}
