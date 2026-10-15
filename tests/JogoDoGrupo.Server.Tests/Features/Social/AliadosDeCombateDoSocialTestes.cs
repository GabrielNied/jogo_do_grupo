using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Features.Social;
using static JogoDoGrupo.Server.Tests.Features.Social.AmbienteDeGrupo;

namespace JogoDoGrupo.Server.Tests.Features.Social;

public sealed class AliadosDeCombateDoSocialTestes
{
    private readonly AmbienteDeGrupo _ambiente = new();

    [Fact]
    public void Membros_do_mesmo_grupo_sao_aliados()
    {
        _ambiente.FormarGrupo(A, B, C);
        var aliados = new AliadosDeCombateDoSocial(_ambiente.Grupos);

        Assert.True(aliados.SaoAliados(A, B));
        Assert.True(aliados.SaoAliados(B, C));
        Assert.True(aliados.SaoAliados(A, C));
        Assert.True(aliados.SaoAliados(B, A));
    }

    [Fact]
    public void Personagens_em_grupos_diferentes_nao_sao_aliados()
    {
        _ambiente.FormarGrupo(A, B);
        _ambiente.FormarGrupo(C, D);

        var aliados = new AliadosDeCombateDoSocial(_ambiente.Grupos);

        Assert.False(aliados.SaoAliados(A, C));
        Assert.False(aliados.SaoAliados(B, D));
    }

    [Fact]
    public void Personagem_solo_nao_e_aliado_de_ninguem()
    {
        _ambiente.FormarGrupo(A, B);
        // C e D continuam solo

        var aliados = new AliadosDeCombateDoSocial(_ambiente.Grupos);

        Assert.False(aliados.SaoAliados(A, C));
        Assert.False(aliados.SaoAliados(C, A));
        Assert.False(aliados.SaoAliados(C, D));
    }

    [Fact]
    public void Personagem_com_ele_mesmo_nao_e_aliado()
    {
        _ambiente.FormarGrupo(A, B);
        var aliados = new AliadosDeCombateDoSocial(_ambiente.Grupos);

        Assert.False(aliados.SaoAliados(A, A));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(0, 0)]
    public void Ids_invalidos_retornam_falso(long a, long b)
    {
        _ambiente.FormarGrupo(A, B);
        var aliados = new AliadosDeCombateDoSocial(_ambiente.Grupos);

        Assert.False(aliados.SaoAliados(a, b));
    }

    [Fact]
    public void Membro_que_sai_deixa_de_ser_aliado_imediatamente()
    {
        _ambiente.FormarGrupo(A, B, C);
        var aliados = new AliadosDeCombateDoSocial(_ambiente.Grupos);

        Assert.True(aliados.SaoAliados(A, B));

        _ambiente.Grupos.Sair(B);

        Assert.False(aliados.SaoAliados(A, B));
        Assert.True(aliados.SaoAliados(A, C));
    }

    [Fact]
    public void Selecao_de_alvos_nao_acerta_aliado_do_mesmo_grupo()
    {
        _ambiente.FormarGrupo(A, B);
        var aliados = new AliadosDeCombateDoSocial(_ambiente.Grupos);

        var alvoAliado = new AlvoDeCombate(B, 0, 0, 0.5, 100, 100, B);
        var alvoInimigo = new AlvoDeCombate(999, 0, 0, 0.5, 100, 100, 999);
        var alvoBoneco = new AlvoDeCombate(0, 0, 0, 0.5, 100, 100);

        Assert.False(SelecaoDeAlvos.PodeAcertar(A, alvoAliado, aliados));
        Assert.True(SelecaoDeAlvos.PodeAcertar(A, alvoInimigo, aliados));
        Assert.True(SelecaoDeAlvos.PodeAcertar(A, alvoBoneco, aliados));
    }
}
