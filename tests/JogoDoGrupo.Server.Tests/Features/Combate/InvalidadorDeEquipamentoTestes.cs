using JogoDoGrupo.Server.Features.Combate;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

public sealed class InvalidadorDeEquipamentoTestes
{
    [Fact]
    public void Nao_invalida_personagem_que_nunca_foi_marcado()
    {
        var invalidador = new InvalidadorDeEquipamento();
        Assert.False(invalidador.ConsumirSeInvalido(123));
    }

    [Fact]
    public void Consumir_retorna_true_uma_vez_e_depois_false()
    {
        var invalidador = new InvalidadorDeEquipamento();
        invalidador.Invalidar(42);

        Assert.True(invalidador.ConsumirSeInvalido(42));
        Assert.False(invalidador.ConsumirSeInvalido(42));
    }

    [Fact]
    public void Multiplas_invalidacoes_para_o_mesmo_personagem_sao_idempotentes()
    {
        var invalidador = new InvalidadorDeEquipamento();
        invalidador.Invalidar(99);
        invalidador.Invalidar(99);

        Assert.True(invalidador.ConsumirSeInvalido(99));
        Assert.False(invalidador.ConsumirSeInvalido(99));
    }
}
