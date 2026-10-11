using JogoDoGrupo.Server.Features.Mundo;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

public class CidadeDoMundoTestes
{
    private sealed class Conexao;

    private long _agora;

    private CidadeDoMundo<Conexao> Nova() => new(() => _agora);

    [Fact]
    public void Entrar_nasce_no_hub_e_ve_os_outros()
    {
        var cidade = Nova();

        var (novoA, euA, outrosA) = cidade.Entrar(1, new Conexao());
        var (novoB, euB, outrosB) = cidade.Entrar(2, new Conexao());

        Assert.True(novoA && novoB);
        Assert.Equal(ValidadorDeMovimento.Nascimento, euA.Posicao);
        Assert.Empty(outrosA);
        Assert.Equal(2, euB.PersonagemId);
        Assert.Equal(1, Assert.Single(outrosB).PersonagemId);
        Assert.Equal(2, cidade.Quantidade);
    }

    [Fact]
    public void Entrar_de_novo_mantem_a_posicao()
    {
        var cidade = Nova();
        var a = new Conexao();
        cidade.Entrar(1, a);
        _agora += 1000;
        cidade.Mover(1, a, new(3f, 0, -12f, 0));

        var (novo, eu, _) = cidade.Entrar(1, a);

        Assert.False(novo);
        Assert.Equal(3f, eu.Posicao.X);
    }

    [Fact]
    public void Mover_aceito_atualiza_e_recusado_mantem()
    {
        var cidade = Nova();
        var a = new Conexao();
        cidade.Entrar(1, a);

        _agora += 1000;
        var (ok, av) = cidade.Mover(1, a, new(4f, 0, -12f, 1f));
        Assert.Equal(ResultadoMovimento.Aceito, ok);
        Assert.Equal(4f, av!.Posicao.X);

        _agora += 100;
        var (recusado, mantido) = cidade.Mover(1, a, new(40f, 0, -12f, 0));
        Assert.Equal(ResultadoMovimento.Recusado, recusado);
        Assert.Equal(4f, mantido!.Posicao.X);
    }

    [Fact]
    public void Mover_fora_da_cidade_ou_com_outra_conexao_e_ignorado()
    {
        var cidade = Nova();
        var (resultado, avatar) = cidade.Mover(1, new Conexao(), new(0, 0, 0, 0));
        Assert.Equal(ResultadoMovimento.ForaDaCidade, resultado);
        Assert.Null(avatar);

        cidade.Entrar(1, new Conexao());
        Assert.Equal(
            ResultadoMovimento.ForaDaCidade,
            cidade.Mover(1, new Conexao(), new(0, 0, -12, 0)).Resultado
        );
    }

    [Fact]
    public void Sair_remove_uma_vez_so()
    {
        var cidade = Nova();
        var a = new Conexao();
        cidade.Entrar(1, a);

        Assert.True(cidade.Sair(1, a));
        Assert.False(cidade.Sair(1, a));
        Assert.Equal(0, cidade.Quantidade);
    }

    [Fact]
    public void Conexao_antiga_encerrando_nao_remove_a_nova_do_mesmo_personagem()
    {
        var cidade = Nova();
        var antiga = new Conexao();
        var nova = new Conexao();
        cidade.Entrar(1, antiga);
        cidade.Entrar(1, nova);

        Assert.False(cidade.Sair(1, antiga));
        Assert.Equal(1, cidade.Quantidade);
        Assert.True(cidade.Sair(1, nova));
    }

    [Fact]
    public async Task Entradas_e_saidas_simultaneas_deixam_o_estado_consistente()
    {
        var cidade = Nova();
        var conexoes = Enumerable.Range(1, 200).Select(_ => new Conexao()).ToArray();

        await Task.WhenAll(conexoes.Select((c, i) => Task.Run(() => cidade.Entrar(i + 1, c))));
        Assert.Equal(200, cidade.Quantidade);

        await Task.WhenAll(conexoes.Select((c, i) => Task.Run(() => cidade.Sair(i + 1, c))));
        Assert.Equal(0, cidade.Quantidade);
    }
}
