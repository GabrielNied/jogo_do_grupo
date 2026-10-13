using JogoDoGrupo.Shared.Core;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Shared.Tests.Features.Mundo;

public class ContratoMundoTestes
{
    // ── IDs dentro da faixa do Mundo (1000–1999) ──────────────────────────────

    [Theory]
    [InlineData(MundoMessageIds.EntrarNaCidade)]
    [InlineData(MundoMessageIds.EstadoDaCidade)]
    [InlineData(MundoMessageIds.IntencaoDeMovimento)]
    [InlineData(MundoMessageIds.PosicaoJogador)]
    [InlineData(MundoMessageIds.JogadorEntrou)]
    [InlineData(MundoMessageIds.JogadorSaiu)]
    [InlineData(MundoMessageIds.EstadoDaArea)]
    public void Id_esta_dentro_da_faixa_do_mundo(ushort id) =>
        Assert.True(
            MundoMessageIds.Faixa.Contem(id),
            $"ID {id} fora de MundoMessageIds.Faixa ({MundoMessageIds.Faixa.Inicio}–{MundoMessageIds.Faixa.Fim})"
        );

    // ── IDs distintos ─────────────────────────────────────────────────────────

    [Fact]
    public void Ids_mundo_sao_distintos()
    {
        var ids = new[]
        {
            MundoMessageIds.EntrarNaCidade,
            MundoMessageIds.EstadoDaCidade,
            MundoMessageIds.IntencaoDeMovimento,
            MundoMessageIds.PosicaoJogador,
            MundoMessageIds.JogadorEntrou,
            MundoMessageIds.JogadorSaiu,
            MundoMessageIds.EstadoDaArea,
        };
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    // ── Valores padrão dos contratos ──────────────────────────────────────────

    [Fact]
    public void JogadorVisivel_campos_padrao()
    {
        var j = new JogadorVisivel();
        Assert.Equal(0L, j.PersonagemId);
        Assert.Equal(0f, j.X);
        Assert.Equal(0f, j.Y);
        Assert.Equal(0f, j.Z);
        Assert.Equal(0f, j.RotacaoY);
    }

    [Fact]
    public void EstadoDaCidade_outros_comecom_vazio_e_nao_nulo()
    {
        var msg = new EstadoDaCidade();
        Assert.NotNull(msg.Eu);
        Assert.NotNull(msg.Outros);
        Assert.Empty(msg.Outros);
    }

    [Fact]
    public void EstadoDaArea_comeca_com_area_e_jogadores_validos()
    {
        var msg = new EstadoDaArea();
        Assert.Equal(string.Empty, msg.Area);
        Assert.NotNull(msg.Eu);
        Assert.NotNull(msg.Outros);
        Assert.Empty(msg.Outros);
    }

    [Fact]
    public void IntencaoDeMovimento_campos_padrao()
    {
        var msg = new IntencaoDeMovimento();
        Assert.Equal(0f, msg.X);
        Assert.Equal(0f, msg.Y);
        Assert.Equal(0f, msg.Z);
        Assert.Equal(0f, msg.RotacaoY);
        Assert.Equal(0, msg.Sequencia);
    }

    [Fact]
    public void PosicaoJogador_campos_padrao()
    {
        var msg = new PosicaoJogador();
        Assert.Equal(0L, msg.PersonagemId);
        Assert.Equal(0f, msg.X);
        Assert.Equal(0f, msg.Y);
        Assert.Equal(0f, msg.Z);
        Assert.Equal(0f, msg.RotacaoY);
        Assert.Equal(0, msg.Sequencia);
    }

    [Fact]
    public void JogadorEntrou_jogador_padrao_nao_nulo()
    {
        var msg = new JogadorEntrou();
        Assert.NotNull(msg.Jogador);
    }

    [Fact]
    public void JogadorSaiu_personagem_id_padrao_zero()
    {
        var msg = new JogadorSaiu();
        Assert.Equal(0L, msg.PersonagemId);
    }
}
