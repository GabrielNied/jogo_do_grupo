using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

/// <summary>
/// Velocidade autoritativa por personagem (modelo do WoW): o servidor muda, o client confirma,
/// e até a confirmação vale a maior entre a antiga e a nova.
/// </summary>
public class VelocidadeNoServidorTestes
{
    private sealed class Conexao;

    private long _agora;

    private CidadeDoMundo<Conexao> Nova() => new(() => _agora);

    // Parte do nascimento e anda dx metros em 1 s.
    private ResultadoMovimento AndarEmUmSegundo(CidadeDoMundo<Conexao> cidade, Conexao c, float dx)
    {
        var de = cidade.Obter(1)!.Posicao;
        _agora += 1000;
        return cidade.Mover(1, c, de with { X = de.X + dx }).Resultado;
    }

    [Fact]
    public void Quem_entra_tem_as_velocidades_padrao()
    {
        var cidade = Nova();
        cidade.Entrar(1, new Conexao());

        var v = cidade.Velocidades(1)!.Value;

        Assert.Equal(RegrasVelocidade.CorrerPadrao, v.Correr);
        Assert.Equal(RegrasVelocidade.AndarPadrao, v.Andar);
        Assert.Equal(0, v.Sequencia);
        Assert.Null(cidade.Velocidades(99));
    }

    [Fact]
    public void Sem_mudanca_correr_alem_do_padrao_e_recusado()
    {
        var cidade = Nova();
        var c = new Conexao();
        cidade.Entrar(1, c);

        Assert.Equal(ResultadoMovimento.Recusado, AndarEmUmSegundo(cidade, c, 9f));
    }

    [Fact]
    public void Velocidade_nova_vale_ja_antes_da_confirmacao()
    {
        var cidade = Nova();
        var c = new Conexao();
        cidade.Entrar(1, c);

        var nova = cidade.MudarVelocidades(1, c, 2f, 10f)!.Value;

        Assert.Equal(1, nova.Sequencia);
        Assert.Equal(10f, cidade.Velocidades(1)!.Value.Correr);
        Assert.Equal(ResultadoMovimento.Aceito, AndarEmUmSegundo(cidade, c, 9f));
    }

    [Fact]
    public void Ao_reduzir_a_antiga_vale_ate_confirmar_e_depois_a_nova()
    {
        var cidade = Nova();
        var c = new Conexao();
        cidade.Entrar(1, c);
        var rapida = cidade.MudarVelocidades(1, c, 2f, 10f)!.Value;
        cidade.ConfirmarVelocidades(1, c, rapida.Sequencia);

        var lenta = cidade.MudarVelocidades(1, c, 2f, 3f)!.Value;

        // O client pode ainda não ter recebido: a rápida continua aceita.
        Assert.Equal(ResultadoMovimento.Aceito, AndarEmUmSegundo(cidade, c, 9f));
        Assert.True(cidade.ConfirmarVelocidades(1, c, lenta.Sequencia));
        Assert.Equal(ResultadoMovimento.Recusado, AndarEmUmSegundo(cidade, c, 9f));
        Assert.Equal(ResultadoMovimento.Aceito, AndarEmUmSegundo(cidade, c, 3f));
    }

    [Fact]
    public void Confirmacao_com_sequencia_errada_ou_sem_mudanca_nao_vale()
    {
        var cidade = Nova();
        var c = new Conexao();
        cidade.Entrar(1, c);

        Assert.False(cidade.ConfirmarVelocidades(1, c, 0));
        var nova = cidade.MudarVelocidades(1, c, 2f, 3f)!.Value;
        Assert.False(cidade.ConfirmarVelocidades(1, c, nova.Sequencia + 1));
        Assert.True(cidade.ConfirmarVelocidades(1, c, nova.Sequencia));
        Assert.False(cidade.ConfirmarVelocidades(1, c, nova.Sequencia));
    }

    [Fact]
    public void Sequencia_cresce_a_cada_mudanca_mesmo_sem_confirmar()
    {
        var cidade = Nova();
        var c = new Conexao();
        cidade.Entrar(1, c);

        cidade.MudarVelocidades(1, c, 2f, 7f);
        var segunda = cidade.MudarVelocidades(1, c, 2f, 8f)!.Value;

        Assert.Equal(2, segunda.Sequencia);
        Assert.Equal(8f, cidade.Velocidades(1)!.Value.Correr);
    }

    [Fact]
    public void Outra_sessao_nao_muda_nem_confirma()
    {
        var cidade = Nova();
        var dono = new Conexao();
        var intruso = new Conexao();
        cidade.Entrar(1, dono);

        Assert.Null(cidade.MudarVelocidades(1, intruso, 2f, 20f));
        var nova = cidade.MudarVelocidades(1, dono, 2f, 20f)!.Value;
        Assert.False(cidade.ConfirmarVelocidades(1, intruso, nova.Sequencia));
    }

    [Fact]
    public void Validador_usa_a_velocidade_recebida()
    {
        var de = ValidadorDeMovimento.Nascimento;
        var para = de with { X = de.X + 9f };

        Assert.False(ValidadorDeMovimento.Permitido(de, para, 1000));
        Assert.True(ValidadorDeMovimento.Permitido(de, para, 1000, 10f));
    }

    [Theory]
    [InlineData(2f, 10f, true)]
    [InlineData(0f, 10f, false)]
    [InlineData(2f, 51f, false)]
    [InlineData(2f, float.NaN, false)]
    public void Pedido_de_teste_so_dentro_da_faixa(float andar, float correr, bool valido) =>
        Assert.Equal(valido, RegrasVelocidade.TesteValido(andar, correr));
}
