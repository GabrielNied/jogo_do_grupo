using JogoDoGrupo.Server.Features.Mundo;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

/// <summary>
/// O client é dono da própria posição (ADR 0024): o servidor valida com um orçamento de
/// deslocamento, aceita excesso pequeno como suspeita e só força a posição no caso grave, com
/// confirmação. Relógio controlado pelo teste.
/// </summary>
public class MovimentoSemVoltarNoTempoTestes
{
    private sealed class Conexao;

    private static readonly PosicaoNaCidade Nascimento = ValidadorDeMovimento.Nascimento;

    private readonly Conexao _conexao = new();
    private readonly CidadeDoMundo<Conexao> _cidade;
    private long _agora = 10_000;

    public MovimentoSemVoltarNoTempoTestes()
    {
        _cidade = new CidadeDoMundo<Conexao>(() => _agora);
        _cidade.Entrar(1, _conexao);
    }

    private ResultadoMovimento IrPara(float x) =>
        _cidade.Mover(1, _conexao, Nascimento with { X = x }).Resultado;

    private float X() => _cidade.Obter(1)!.Posicao.X;

    [Fact]
    public void Pacotes_em_rajada_nao_viram_recusa()
    {
        // Um segundo correndo, mas os 10 pacotes chegam juntos (jitter de rede).
        _agora += 1000;
        for (var passo = 1; passo <= 10; passo++)
        {
            Assert.Equal(ResultadoMovimento.Aceito, IrPara(passo * 0.55f));
        }

        Assert.Equal(5.5f, X(), 3);
        Assert.Equal(0, _cidade.TotalDeSuspeitas(1));
    }

    [Fact]
    public void Correr_sem_parar_nunca_gasta_o_orcamento()
    {
        for (var passo = 1; passo <= 100; passo++)
        {
            _agora += 100;
            Assert.Equal(ResultadoMovimento.Aceito, IrPara(passo * 0.55f));
        }

        Assert.Equal(0, _cidade.TotalDeSuspeitas(1));
    }

    [Fact]
    public void Excesso_pequeno_e_aceito_como_suspeita_e_a_posicao_do_client_vale()
    {
        var teto = (float)
            ValidadorDeMovimento.TetoDoOrcamento(ValidadorDeMovimento.VelocidadeMaxima);

        Assert.Equal(ResultadoMovimento.AceitoComSuspeita, IrPara(teto + 3f));

        Assert.Equal(teto + 3f, X());
        Assert.Equal(1, _cidade.TotalDeSuspeitas(1));
        Assert.Null(_cidade.PosicaoForcadaPendente(1));
    }

    [Fact]
    public void Salto_impossivel_forca_a_posicao_e_ignora_o_movimento_ate_confirmar()
    {
        _agora += 1000;
        IrPara(2f);

        Assert.Equal(ResultadoMovimento.Recusado, IrPara(80f));
        Assert.Equal(2f, X());
        var sequencia = _cidade.PosicaoForcadaPendente(1);
        Assert.Equal(1, sequencia);

        // Movimento ainda de antes do teleporte: ignorado, sem nova posição forçada.
        _agora += 100;
        Assert.Equal(ResultadoMovimento.AguardandoConfirmacao, IrPara(80.5f));
        Assert.Equal(1, _cidade.PosicaoForcadaPendente(1));

        Assert.False(_cidade.ConfirmarPosicaoForcada(1, _conexao, 99));
        Assert.True(_cidade.ConfirmarPosicaoForcada(1, _conexao, 1));
        Assert.Null(_cidade.PosicaoForcadaPendente(1));
        Assert.Equal(ResultadoMovimento.Aceito, IrPara(2.5f));
    }

    [Fact]
    public void Posicao_fora_do_mundo_e_grave_mesmo_perto()
    {
        var fora = new PosicaoNaCidade(ValidadorDeMovimento.LimiteHorizontal + 1f, 0f, 0f, 0f);

        Assert.Equal(ResultadoMovimento.Recusado, _cidade.Mover(1, _conexao, fora).Resultado);
    }

    [Fact]
    public void Suspeitas_seguidas_viram_posicao_forcada()
    {
        var teto = (float)
            ValidadorDeMovimento.TetoDoOrcamento(ValidadorDeMovimento.VelocidadeMaxima);
        IrPara(teto); // gasta o orçamento sem suspeita

        // Cada passo vai 3 m além do que o tempo permite: suspeito.
        var x = teto;
        ResultadoMovimento resultado;
        var passos = 0;
        do
        {
            _agora += 100;
            x += 0.55f + 3f;
            resultado = IrPara(x);
            passos++;
        } while (resultado == ResultadoMovimento.AceitoComSuspeita && passos < 50);

        Assert.Equal(ResultadoMovimento.Recusado, resultado);
        Assert.Equal((int)ValidadorDeMovimento.LimiteDeSuspeitas + 1, passos);
    }

    [Fact]
    public void Suspeita_isolada_esvazia_com_o_tempo()
    {
        var teto = (float)
            ValidadorDeMovimento.TetoDoOrcamento(ValidadorDeMovimento.VelocidadeMaxima);
        for (var vez = 0; vez < 20; vez++)
        {
            // Um salto 3 m além do orçamento a cada 4 s: o contador esvazia mais rápido do que enche.
            _agora += 4000;
            Assert.Equal(ResultadoMovimento.AceitoComSuspeita, IrPara(X() + teto + 3f));
        }

        Assert.Null(_cidade.PosicaoForcadaPendente(1));
        Assert.Equal(20, _cidade.TotalDeSuspeitas(1));
    }

    [Fact]
    public void Teleporte_do_servidor_muda_a_posicao_e_espera_a_confirmacao()
    {
        var destino = Nascimento with { X = 30f };

        var sequencia = _cidade.ForcarPosicao(1, destino);

        Assert.Equal(1, sequencia);
        Assert.Equal(30f, X());
        Assert.Equal(ResultadoMovimento.AguardandoConfirmacao, IrPara(1f));
        Assert.True(_cidade.ConfirmarPosicaoForcada(1, _conexao, 1));
        _agora += 100;
        Assert.Equal(ResultadoMovimento.Aceito, IrPara(30.5f));
        Assert.Null(_cidade.ForcarPosicao(99, destino));
    }

    [Fact]
    public void Reconectar_da_folga_para_o_que_o_client_andou_sem_conexao()
    {
        _cidade.Entrar(1, _conexao); // mesma presença: é reconexão

        // 40 m sem o tempo andar: mais que o teto normal, dentro da folga da reconexão.
        Assert.Equal(ResultadoMovimento.Aceito, IrPara(40f));
    }

    [Fact]
    public void Outra_sessao_nao_confirma_a_posicao_forcada()
    {
        IrPara(80f);

        Assert.False(_cidade.ConfirmarPosicaoForcada(1, new Conexao(), 1));
        Assert.Equal(1, _cidade.PosicaoForcadaPendente(1));
    }

    [Theory]
    [InlineData(0.0, JulgamentoDoPasso.Normal)]
    [InlineData(5.0, JulgamentoDoPasso.Normal)]
    [InlineData(10.0, JulgamentoDoPasso.Suspeito)]
    [InlineData(11.0, JulgamentoDoPasso.Suspeito)]
    [InlineData(11.5, JulgamentoDoPasso.Grave)]
    public void Julgamento_de_um_passo_com_5_m_de_orcamento(
        double metros,
        JulgamentoDoPasso esperado
    )
    {
        var para = Nascimento with { X = (float)metros };

        Assert.Equal(esperado, ValidadorDeMovimento.Julgar(Nascimento, para, 5.0));
    }

    [Fact]
    public void Orcamento_recarrega_ate_o_teto_e_nao_corta_o_da_reconexao()
    {
        var teto = ValidadorDeMovimento.TetoDoOrcamento(5.5f);

        Assert.Equal(5.5, ValidadorDeMovimento.Recarregar(0, 1000, 5.5f), 3);
        Assert.Equal(teto, ValidadorDeMovimento.Recarregar(0, 60_000, 5.5f), 3);
        Assert.Equal(0, ValidadorDeMovimento.Recarregar(0, -500, 5.5f));
        Assert.Equal(50, ValidadorDeMovimento.Recarregar(50, 1000, 5.5f));
    }
}
