using JogoDoGrupo.Server.Features.Mundo;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

/// <summary>
/// A janela de deslocamento que o Mundo abre a pedido de outra funcionalidade (a esquiva: 5 m
/// em 0,6 s). Relógio controlado pelo teste.
/// </summary>
public class JanelaDeDeslocamentoTestes
{
    private sealed class Conexao;

    private const double Metros = 5.0;
    private const double Segundos = 0.6;

    private static readonly float _z0 = ValidadorDeMovimento.Nascimento.Z;

    private readonly Conexao _conexao = new();
    private readonly CidadeDoMundo<Conexao> _cidade;
    private long _agora = 10_000;

    public JanelaDeDeslocamentoTestes()
    {
        _cidade = new CidadeDoMundo<Conexao>(() => _agora);
        _cidade.Entrar(1, _conexao);
    }

    /// <summary>Pede para ir a um ponto à frente do nascimento (em metros, no eixo Z).</summary>
    private ResultadoMovimento MoverPara(float metrosAFrente) =>
        _cidade.Mover(1, _conexao, new PosicaoNaCidade(0f, 0f, _z0 + metrosAFrente, 0f)).Resultado;

    private float Avanco() => _cidade.Obter(1)!.Posicao.Z - _z0;

    [Fact]
    public void Sem_janela_5_m_em_600_ms_e_recusado()
    {
        _agora += 600;
        Assert.Equal(ResultadoMovimento.Recusado, MoverPara(5f));
        Assert.Equal(0f, Avanco());
    }

    [Fact]
    public void Com_janela_5_m_em_600_ms_e_aceito()
    {
        Assert.True(_cidade.AbrirJanela(1, Metros, Segundos));
        _agora += 600;

        Assert.Equal(ResultadoMovimento.Aceito, MoverPara(5f));
        Assert.Equal(5f, Avanco());
    }

    [Fact]
    public void Com_janela_5_metros_e_10_centimetros_e_recusado()
    {
        _cidade.AbrirJanela(1, Metros, Segundos);
        _agora += 600;

        Assert.Equal(ResultadoMovimento.Recusado, MoverPara(5.1f));
        Assert.Equal(0f, Avanco());
    }

    [Fact]
    public void Janela_nao_deixa_andar_5_m_de_uma_vez_no_primeiro_instante()
    {
        _cidade.AbrirJanela(1, Metros, Segundos);
        _agora += 100;

        Assert.Equal(ResultadoMovimento.Recusado, MoverPara(5f));
    }

    [Fact]
    public void Deslocamento_em_passos_soma_ate_5_m_e_o_que_passa_disso_e_recusado()
    {
        _cidade.AbrirJanela(1, Metros, Segundos);

        foreach (var metros in new[] { 1.6f, 3.3f, 5f })
        {
            _agora += 200;
            Assert.Equal(ResultadoMovimento.Aceito, MoverPara(metros));
        }

        _agora += 100;
        Assert.Equal(ResultadoMovimento.Recusado, MoverPara(5.4f));
        Assert.Equal(5f, Avanco());
    }

    [Fact]
    public void Limite_e_a_distancia_ate_onde_a_janela_abriu_em_qualquer_direcao()
    {
        _cidade.AbrirJanela(1, Metros, Segundos);
        _agora += 600;

        Assert.Equal(ResultadoMovimento.Aceito, MoverPara(-5f));
    }

    [Fact]
    public void Janela_ainda_vale_na_folga_de_150_ms_e_depois_fecha()
    {
        _cidade.AbrirJanela(1, Metros, Segundos);
        _agora += 600 + ValidadorDeMovimento.FolgaDaJanelaMs + 1;
        Assert.Equal(ResultadoMovimento.Recusado, MoverPara(5f));

        _agora -= 1;
        Assert.Equal(ResultadoMovimento.Aceito, MoverPara(5f));
    }

    [Fact]
    public void Fora_da_janela_volta_o_limite_de_corrida()
    {
        _cidade.AbrirJanela(1, Metros, Segundos);
        _agora += 600;
        Assert.Equal(ResultadoMovimento.Aceito, MoverPara(5f));

        // 400 ms depois a janela já fechou: correndo dá 5,5 x 0,4 + 0,5 = 2,7 m.
        _agora += 400;
        Assert.Equal(ResultadoMovimento.Recusado, MoverPara(5f + 3f));
        Assert.Equal(ResultadoMovimento.Aceito, MoverPara(5f + 2.6f));
    }

    [Fact]
    public void Janela_nova_parte_de_onde_o_personagem_esta()
    {
        _cidade.AbrirJanela(1, Metros, Segundos);
        _agora += 600;
        MoverPara(5f);

        _agora += 1_000;
        Assert.True(_cidade.AbrirJanela(1, Metros, Segundos));
        _agora += 600;

        Assert.Equal(ResultadoMovimento.Aceito, MoverPara(10f));
    }

    [Fact]
    public void Janela_nao_abre_para_quem_nao_esta_na_cidade()
    {
        Assert.False(_cidade.AbrirJanela(99, Metros, Segundos));
    }

    [Theory]
    [InlineData(0.0, 0.6)]
    [InlineData(-5.0, 0.6)]
    [InlineData(5.0, 0.0)]
    [InlineData(5.0, -1.0)]
    [InlineData(double.NaN, 0.6)]
    [InlineData(5.0, double.PositiveInfinity)]
    public void Janela_com_numeros_que_nao_servem_nao_abre(double metros, double segundos)
    {
        Assert.False(_cidade.AbrirJanela(1, metros, segundos));
        _agora += 600;
        Assert.Equal(ResultadoMovimento.Recusado, MoverPara(5f));
    }

    [Fact]
    public void Janela_nao_libera_posicao_fora_do_mundo()
    {
        var origem = new PosicaoNaCidade(ValidadorDeMovimento.LimiteHorizontal - 1f, 0f, 0f, 0f);
        var fora = origem with { X = ValidadorDeMovimento.LimiteHorizontal + 1f };

        Assert.False(ValidadorDeMovimento.PermitidoNaJanela(origem, origem, fora, 600, 5.0, 8.4));
    }

    [Fact]
    public void Sair_e_entrar_de_novo_nao_carrega_a_janela()
    {
        _cidade.AbrirJanela(1, Metros, Segundos);
        _cidade.Sair(1, _conexao);
        _cidade.Entrar(1, _conexao);
        _agora += 600;

        Assert.Equal(ResultadoMovimento.Recusado, MoverPara(5f));
    }
}
