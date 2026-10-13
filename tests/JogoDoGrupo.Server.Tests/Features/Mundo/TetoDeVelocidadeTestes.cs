using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Server.Tests.Infra;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

/// <summary>
/// O teto de velocidade que o Combate pede no preparo do golpe (40% da corrida, #598). Relógio
/// controlado pelo teste.
/// </summary>
public class TetoDeVelocidadeTestes
{
    private sealed class Conexao;

    private const double Fator = 0.4;
    private const double Segundos = 0.5;

    private readonly Conexao _conexao = new();
    private readonly CidadeDoMundo<Conexao> _cidade;
    private long _agora = 10_000;
    private readonly PosicaoNaCidade _inicio;

    public TetoDeVelocidadeTestes()
    {
        _cidade = new CidadeDoMundo<Conexao>(() => _agora);
        _cidade.Entrar(1, _conexao);
        _inicio = _cidade.Obter(1)!.Posicao;
    }

    private ResultadoMovimento MoverPara(float metrosAFrente) =>
        _cidade.Mover(1, _conexao, _inicio with { Z = _inicio.Z + metrosAFrente }).Resultado;

    private float Avanco() => _cidade.Obter(1)!.Posicao.Z - _inicio.Z;

    // Até onde o teto deixa ir em 500 ms: 40% da corrida mais as folgas.
    private static double Alcance() =>
        ValidadorDeMovimento.AlcanceNoTeto(ValidadorDeMovimento.VelocidadeMaxima * Fator, 500);

    [Fact]
    public void Com_o_teto_andar_a_40_por_cento_da_corrida_e_aceito()
    {
        Assert.True(_cidade.LimitarVelocidade(1, Fator, Segundos));
        _agora += 500;

        var metros = (float)(ValidadorDeMovimento.VelocidadeMaxima * Fator * 0.5);
        Assert.Equal(ResultadoMovimento.Aceito, MoverPara(metros));
        Assert.Equal(metros, Avanco(), 0.001f);
    }

    [Fact]
    public void Com_o_teto_correr_alem_de_40_por_cento_e_recusado_e_conta_suspeita()
    {
        _cidade.LimitarVelocidade(1, Fator, Segundos);
        _agora += 500;

        // Sem o teto, o orçamento cheio aceitaria esse passo sem suspeita.
        Assert.Equal(ResultadoMovimento.Recusado, MoverPara((float)Alcance() + 0.1f));
        Assert.Equal(0f, Avanco());
        Assert.Equal(1, _cidade.TotalDeSuspeitas(1));
    }

    [Fact]
    public void Depois_da_duracao_a_velocidade_normal_volta()
    {
        _cidade.LimitarVelocidade(1, Fator, Segundos);
        _agora += 500 + ValidadorDeMovimento.FolgaDaJanelaMs + 1;

        Assert.Equal(ResultadoMovimento.Aceito, MoverPara((float)Alcance() + 0.1f));
        Assert.Equal(0, _cidade.TotalDeSuspeitas(1));
    }

    [Fact]
    public void Pedido_novo_substitui_o_anterior_a_partir_de_onde_o_personagem_esta()
    {
        _cidade.LimitarVelocidade(1, Fator, Segundos);
        _agora += 500;
        MoverPara(1f);

        Assert.True(_cidade.LimitarVelocidade(1, Fator, Segundos));
        _agora += 500;

        // O alcance conta de onde o segundo teto começou (1 m à frente).
        Assert.Equal(ResultadoMovimento.Aceito, MoverPara(1f + (float)Alcance() - 0.1f));
    }

    [Fact]
    public void Fator_1_tira_o_teto()
    {
        _cidade.LimitarVelocidade(1, Fator, 30);
        Assert.True(_cidade.LimitarVelocidade(1, 1, 30));
        _agora += 500;

        Assert.Equal(ResultadoMovimento.Aceito, MoverPara((float)Alcance() + 0.1f));
    }

    [Theory]
    [InlineData(-0.1, 0.5)]
    [InlineData(1.1, 0.5)]
    [InlineData(double.NaN, 0.5)]
    [InlineData(0.4, 0)]
    [InlineData(0.4, double.PositiveInfinity)]
    public void Numeros_que_nao_servem_sao_recusados(double fator, double segundos) =>
        Assert.False(_cidade.LimitarVelocidade(1, fator, segundos));

    [Fact]
    public void Personagem_fora_da_cidade_e_recusado() =>
        Assert.False(_cidade.LimitarVelocidade(99, Fator, Segundos));

    [Fact]
    public void O_Mundo_atende_o_contrato_pedido_pelo_Combate()
    {
        using var servidor = new ServidorDeTeste();
        var limite = servidor.Services.GetRequiredService<ILimiteDeVelocidadeDoMundo>();

        Assert.False(limite.LimitarVelocidade(424242, Fator, Segundos));
    }
}
