using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Skills;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class RegrasEscudoTestes
{
    [Fact]
    public void Absorcao_e_uma_fracao_da_vida_maxima_e_nunca_zero_com_vida()
    {
        Assert.Equal(40, RegrasEscudo.Absorcao(200, 0.20));
        Assert.Equal(1, RegrasEscudo.Absorcao(1, 0.01));
        Assert.Equal(0, RegrasEscudo.Absorcao(0, 0.20));
        Assert.Equal(0, RegrasEscudo.Absorcao(200, 0));
    }

    [Theory]
    [InlineData(40, 25, 0, 15)]
    [InlineData(40, 40, 0, 0)]
    [InlineData(40, 65, 25, 0)]
    [InlineData(0, 30, 30, 0)]
    [InlineData(40, 0, 0, 40)]
    public void Escudo_absorve_antes_da_vida(int escudo, int dano, int passa, int sobra)
    {
        Assert.Equal(passa, RegrasEscudo.Absorver(escudo, dano, out var restante));
        Assert.Equal(sobra, restante);
    }

    [Fact]
    public void Protegido_reduz_o_dano_recebido_e_convive_com_vulneravel()
    {
        Assert.Equal(1.0, RegrasEfeitosDeStatus.MultiplicadorDanoRecebido(false, false), 3);
        Assert.Equal(0.85, RegrasEfeitosDeStatus.MultiplicadorDanoRecebido(false, true), 3);
        Assert.Equal(1.15 * 0.85, RegrasEfeitosDeStatus.MultiplicadorDanoRecebido(true, true), 3);
    }

    [Fact]
    public void Skills_de_apoio_declaram_o_efeito_e_nao_causam_dano()
    {
        var manto = CatalogoDeSkills.Obter("cajado.manto-protetor")!;
        Assert.Equal(TipoDeApoio.Escudo, manto.Apoio);
        Assert.Equal(TipoDeEfeito.Escudo, manto.Propriedades.Tipo);
        Assert.Equal(TipoDeApoio.Fortalecer, CatalogoDeSkills.Obter("foco.fonte-arcana")!.Apoio);
        Assert.Equal(
            TipoDeApoio.Proteger,
            CatalogoDeSkills.Obter("martelo.brado-do-guardiao")!.Apoio
        );
        Assert.Equal(TipoEquipamento.Cajado, CatalogoDeSkills.TipoDaSkill(manto.Id));
        Assert.Equal(0, manto.PreparoMs);
    }
}
