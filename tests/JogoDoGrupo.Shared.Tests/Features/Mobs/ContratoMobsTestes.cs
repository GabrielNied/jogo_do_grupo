using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Shared.Tests.Features.Mobs;

public class ContratoMobsTestes
{
    private static readonly ushort[] _ids =
    {
        MobsMessageIds.InimigosDaArea,
        MobsMessageIds.InimigoMudou,
        MobsMessageIds.InimigoPreparaAtaque,
        MobsMessageIds.InimigoMorreu,
        MobsMessageIds.InimigoApareceu,
    };

    [Theory]
    [InlineData(MobsMessageIds.InimigosDaArea)]
    [InlineData(MobsMessageIds.InimigoMudou)]
    [InlineData(MobsMessageIds.InimigoPreparaAtaque)]
    [InlineData(MobsMessageIds.InimigoMorreu)]
    [InlineData(MobsMessageIds.InimigoApareceu)]
    public void Id_esta_dentro_da_faixa_de_mobs(ushort id) =>
        Assert.True(
            MobsMessageIds.Faixa.Contem(id),
            $"ID {id} fora de MobsMessageIds.Faixa ({MobsMessageIds.Faixa.Inicio}–{MobsMessageIds.Faixa.Fim})"
        );

    [Fact]
    public void Ids_de_mobs_sao_distintos() => Assert.Equal(_ids.Length, _ids.Distinct().Count());

    [Fact]
    public void Ids_sao_os_do_contrato()
    {
        Assert.Equal(11000, MobsMessageIds.InimigosDaArea);
        Assert.Equal(11001, MobsMessageIds.InimigoMudou);
        Assert.Equal(11002, MobsMessageIds.InimigoPreparaAtaque);
        Assert.Equal(11003, MobsMessageIds.InimigoMorreu);
        Assert.Equal(11004, MobsMessageIds.InimigoApareceu);
    }

    [Fact]
    public void EstadoDeInimigo_tem_valores_estaveis()
    {
        Assert.Equal(0, (int)EstadoDeInimigo.Repouso);
        Assert.Equal(1, (int)EstadoDeInimigo.Alerta);
        Assert.Equal(2, (int)EstadoDeInimigo.Perseguicao);
        Assert.Equal(3, (int)EstadoDeInimigo.Ataque);
        Assert.Equal(4, (int)EstadoDeInimigo.Retorno);
        Assert.Equal(5, (int)EstadoDeInimigo.Morto);
    }

    [Fact]
    public void InimigoNoMundo_campos_padrao()
    {
        var inimigo = new InimigoNoMundo();
        Assert.Equal(0L, inimigo.InimigoId);
        Assert.Equal(0, inimigo.CriaturaId);
        Assert.Equal(VarianteDeCriatura.Normal, inimigo.Variante);
        Assert.Equal(0f, inimigo.X);
        Assert.Equal(0f, inimigo.Y);
        Assert.Equal(0f, inimigo.Z);
        Assert.Equal(0f, inimigo.RotacaoY);
        Assert.Equal(0, inimigo.Vida);
        Assert.Equal(0, inimigo.VidaMaxima);
        Assert.Equal(EstadoDeInimigo.Repouso, inimigo.Estado);
        Assert.Equal(RegrasInimigo.SemAlvo, inimigo.AlvoId);
    }

    [Fact]
    public void InimigosDaArea_comeca_vazia_e_nao_nula()
    {
        var msg = new InimigosDaArea();
        Assert.NotNull(msg.Inimigos);
        Assert.Empty(msg.Inimigos);
    }

    [Fact]
    public void InimigoMudou_campos_padrao()
    {
        var msg = new InimigoMudou();
        Assert.Equal(0L, msg.InimigoId);
        Assert.Equal(0f, msg.X);
        Assert.Equal(0f, msg.Y);
        Assert.Equal(0f, msg.Z);
        Assert.Equal(0f, msg.RotacaoY);
        Assert.Equal(EstadoDeInimigo.Repouso, msg.Estado);
        Assert.Equal(RegrasInimigo.SemAlvo, msg.AlvoId);
        Assert.Equal(0, msg.Vida);
        Assert.Equal(0, msg.Sequencia);
    }

    [Fact]
    public void InimigoPreparaAtaque_campos_padrao()
    {
        var msg = new InimigoPreparaAtaque();
        Assert.Equal(0L, msg.InimigoId);
        Assert.Equal(0, msg.PreparoMs);
        Assert.Equal(0f, msg.CentroX);
        Assert.Equal(0f, msg.CentroZ);
        Assert.Equal(0f, msg.Raio);
        Assert.Equal(0f, msg.DirecaoY);
    }

    [Fact]
    public void InimigoMorreu_e_InimigoApareceu_campos_padrao()
    {
        Assert.Equal(0L, new InimigoMorreu().InimigoId);
        Assert.NotNull(new InimigoApareceu().Inimigo);
    }
}
