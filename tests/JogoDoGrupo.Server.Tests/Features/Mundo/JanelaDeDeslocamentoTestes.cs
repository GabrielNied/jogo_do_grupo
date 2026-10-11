using JogoDoGrupo.Server.Features.Mundo;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

/// <summary>
/// A janela de deslocamento que o Mundo abre a pedido de outra funcionalidade (a esquiva: 5 m
/// em 0,6 s). Relógio controlado pelo teste.
/// </summary>
/// <remarks>
/// Desde o ADR 0024 o movimento fora da janela vale pelo orçamento de deslocamento, e um excesso
/// pequeno é aceito como suspeita (o jogador não volta no tempo). Para testar a janela no limite
/// estrito, cada teste começa com o orçamento vazio (<see cref="EsvaziarOrcamento"/>).
/// </remarks>
public class JanelaDeDeslocamentoTestes
{
    private sealed class Conexao;

    private const double Metros = 5.0;
    private const double Segundos = 0.6;

    private readonly Conexao _conexao = new();
    private readonly CidadeDoMundo<Conexao> _cidade;
    private long _agora = 10_000;
    private PosicaoNaCidade _inicio;

    public JanelaDeDeslocamentoTestes()
    {
        _cidade = new CidadeDoMundo<Conexao>(() => _agora);
        _cidade.Entrar(1, _conexao);
        EsvaziarOrcamento();
    }

    /// <summary>
    /// Um salto do tamanho exato do orçamento, sem o tempo andar, gasta tudo sem suspeita.
    /// Os testes andam a partir de onde ele termina.
    /// </summary>
    private void EsvaziarOrcamento()
    {
        var aqui = _cidade.Obter(1)!.Posicao;
        var teto = (float)
            ValidadorDeMovimento.TetoDoOrcamento(ValidadorDeMovimento.VelocidadeMaxima);
        Assert.Equal(
            ResultadoMovimento.Aceito,
            _cidade.Mover(1, _conexao, aqui with { X = aqui.X + teto }).Resultado
        );
        _inicio = _cidade.Obter(1)!.Posicao;
    }

    /// <summary>Pede para ir a um ponto à frente do início (em metros, no eixo Z).</summary>
    private ResultadoMovimento MoverPara(float metrosAFrente) =>
        _cidade.Mover(1, _conexao, _inicio with { Z = _inicio.Z + metrosAFrente }).Resultado;

    private float Avanco() => _cidade.Obter(1)!.Posicao.Z - _inicio.Z;

    [Fact]
    public void Sem_janela_5_m_em_600_ms_conta_como_suspeita()
    {
        _agora += 600;
        Assert.Equal(ResultadoMovimento.AceitoComSuspeita, MoverPara(5f));
        Assert.Equal(5f, Avanco());
        Assert.Equal(1, _cidade.TotalDeSuspeitas(1));
    }

    [Fact]
    public void Com_janela_5_m_em_600_ms_e_aceito()
    {
        Assert.True(_cidade.AbrirJanela(1, Metros, Segundos));
        _agora += 600;

        Assert.Equal(ResultadoMovimento.Aceito, MoverPara(5f));
        Assert.Equal(5f, Avanco());
        Assert.Equal(0, _cidade.TotalDeSuspeitas(1));
    }

    [Fact]
    public void Com_janela_5_metros_e_10_centimetros_sai_da_janela_e_conta_como_suspeita()
    {
        _cidade.AbrirJanela(1, Metros, Segundos);
        _agora += 600;

        Assert.Equal(ResultadoMovimento.AceitoComSuspeita, MoverPara(5.1f));
    }

    [Fact]
    public void Janela_nao_deixa_andar_5_m_de_uma_vez_no_primeiro_instante()
    {
        _cidade.AbrirJanela(1, Metros, Segundos);
        _agora += 100;

        Assert.Equal(ResultadoMovimento.AceitoComSuspeita, MoverPara(5f));
    }

    [Fact]
    public void Deslocamento_em_passos_soma_ate_5_m_e_depois_vale_o_orcamento_normal()
    {
        _cidade.AbrirJanela(1, Metros, Segundos);

        foreach (var metros in new[] { 1.6f, 3.3f, 5f })
        {
            _agora += 200;
            Assert.Equal(ResultadoMovimento.Aceito, MoverPara(metros));
        }

        // Passou dos 5 m da janela: 0,4 m em 100 ms é corrida comum, pelo orçamento.
        _agora += 100;
        Assert.Equal(ResultadoMovimento.Aceito, MoverPara(5.4f));
        Assert.Equal(5.4f, Avanco(), 3);
    }

    [Fact]
    public void Limite_e_a_distancia_ate_onde_a_janela_abriu_em_qualquer_direcao()
    {
        _cidade.AbrirJanela(1, Metros, Segundos);
        _agora += 600;

        Assert.Equal(ResultadoMovimento.Aceito, MoverPara(-5f));
    }

    [Fact]
    public void Janela_ainda_vale_na_folga_de_150_ms()
    {
        _cidade.AbrirJanela(1, Metros, Segundos);
        _agora += 600 + ValidadorDeMovimento.FolgaDaJanelaMs;

        Assert.Equal(ResultadoMovimento.Aceito, MoverPara(5f));
    }

    [Fact]
    public void Depois_da_folga_a_janela_fecha()
    {
        _cidade.AbrirJanela(1, Metros, Segundos);
        _agora += 600 + ValidadorDeMovimento.FolgaDaJanelaMs + 1;

        Assert.Equal(ResultadoMovimento.AceitoComSuspeita, MoverPara(5f));
    }

    [Fact]
    public void Fora_da_janela_vale_o_orcamento_de_corrida()
    {
        _cidade.AbrirJanela(1, Metros, Segundos);
        _agora += 600;
        Assert.Equal(ResultadoMovimento.Aceito, MoverPara(5f));

        // A janela não gastou o orçamento: 0,6 s + 0,4 s de corrida = 5,5 m.
        _agora += 400;
        Assert.Equal(ResultadoMovimento.Aceito, MoverPara(5f + 3f));
        Assert.Equal(ResultadoMovimento.AceitoComSuspeita, MoverPara(5f + 3f + 6f));
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
        Assert.Equal(ResultadoMovimento.AceitoComSuspeita, MoverPara(5f));
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
        EsvaziarOrcamento();
        _agora += 600;

        Assert.Equal(ResultadoMovimento.AceitoComSuspeita, MoverPara(5f));
    }
}
