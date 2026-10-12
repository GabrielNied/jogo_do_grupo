using JogoDoGrupo.Server.Core.TempoReal;

namespace JogoDoGrupo.Server.Tests.Core.TempoReal;

public class LimiteDeMensagensTestes
{
    [Fact]
    public void Aceita_a_rajada_inteira_e_recusa_a_seguinte()
    {
        var limite = new LimiteDeMensagens(mensagensPorSegundo: 10, rajada: 5, agoraMs: 0);

        for (var i = 0; i < 5; i++)
        {
            Assert.True(limite.Consumir(0));
        }

        Assert.False(limite.Consumir(0));
    }

    [Fact]
    public void Fichas_voltam_no_ritmo_por_segundo()
    {
        var limite = new LimiteDeMensagens(mensagensPorSegundo: 10, rajada: 1, agoraMs: 0);
        Assert.True(limite.Consumir(0));

        Assert.False(limite.Consumir(99)); // 0,99 ficha
        Assert.True(limite.Consumir(100)); // completou uma
        Assert.False(limite.Consumir(100));
    }

    [Fact]
    public void Ritmo_constante_no_limite_nunca_e_recusado()
    {
        var limite = new LimiteDeMensagens(mensagensPorSegundo: 60, rajada: 120, agoraMs: 0);

        // Um minuto a 60 por segundo, mais que a rajada inteira.
        for (var ms = 0L; ms < 60_000; ms += 1000 / 60 + 1)
        {
            Assert.True(limite.Consumir(ms), $"recusado em {ms} ms");
        }
    }

    [Fact]
    public void Silencio_longo_nao_acumula_mais_que_a_rajada()
    {
        var limite = new LimiteDeMensagens(mensagensPorSegundo: 10, rajada: 3, agoraMs: 0);

        var aceitas = 0;
        while (limite.Consumir(1_000_000))
        {
            aceitas++;
        }

        Assert.Equal(3, aceitas);
    }

    [Fact]
    public void Relogio_que_volta_nao_cria_fichas()
    {
        var limite = new LimiteDeMensagens(mensagensPorSegundo: 10, rajada: 1, agoraMs: 1000);
        Assert.True(limite.Consumir(1000));

        Assert.False(limite.Consumir(0));
        Assert.False(limite.Consumir(1050));
        Assert.True(limite.Consumir(1100));
    }
}
