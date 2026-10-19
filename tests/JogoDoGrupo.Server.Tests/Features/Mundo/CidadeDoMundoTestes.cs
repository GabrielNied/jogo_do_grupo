using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Shared.Features.Mundo;

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
        cidade.Mover(1, a, new(3f, ValidadorDeMovimento.Nascimento.Y, -24f, 0));

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

        var y = ValidadorDeMovimento.Nascimento.Y;
        _agora += 1000;
        var (ok, av) = cidade.Mover(1, a, new(4f, y, -24f, 1f));
        Assert.Equal(ResultadoMovimento.Aceito, ok);
        Assert.Equal(4f, av!.Posicao.X);

        _agora += 100;
        var (aceito, andou) = cidade.Mover(1, a, new(40f, y, -24f, 0));
        Assert.Equal(ResultadoMovimento.AceitoComSuspeita, aceito);
        Assert.Equal(40f, andou!.Posicao.X);
        Assert.Null(cidade.PosicaoForcadaPendente(1));
    }

    [Fact]
    public void Andar_pelo_tunel_da_caverna_nao_forca_a_posicao()
    {
        var cidade = Nova();
        var a = new Conexao();
        cidade.Entrar(1, a);
        _agora += 1000;
        cidade.Mover(
            1,
            a,
            new(PrimeiraCaverna.EntradaX, PrimeiraCaverna.EntradaY, PrimeiraCaverna.EntradaZ, 0)
        );

        var comprimento = PrimeiraCaverna.ComprimentoDoPercurso;
        for (var d = 3f; d <= comprimento; d += 3f)
        {
            PrimeiraCaverna.PontoDoPercurso(d, out var x, out var z);
            _agora += 1000;
            var (resultado, avatar) = cidade.Mover(
                1,
                a,
                new(x, PrimeiraCaverna.PisoNoPlano(x, z)!.Value, z, 0)
            );

            Assert.NotEqual(ResultadoMovimento.Recusado, resultado);
            Assert.Equal(z, avatar!.Posicao.Z);
        }

        Assert.Null(cidade.PosicaoForcadaPendente(1));
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
            cidade.Mover(1, new Conexao(), new(0, 0, -24, 0)).Resultado
        );
    }

    [Fact]
    public void Obter_devolve_o_avatar_so_de_quem_esta_na_cidade()
    {
        var cidade = Nova();
        Assert.Null(cidade.Obter(1));

        var a = new Conexao();
        cidade.Entrar(1, a);
        _agora += 1000;
        cidade.Mover(1, a, new(2f, 0, -24f, 0));
        Assert.Equal(2f, cidade.Obter(1)!.Posicao.X);
        Assert.Null(cidade.Obter(2));

        cidade.Sair(1, a);
        Assert.Null(cidade.Obter(1));
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

    [Fact]
    public void Sequencia_de_replica_cresce_a_cada_movimento_e_carimba_tempo()
    {
        var cidade = Nova();
        var a = new Conexao();
        cidade.Entrar(1, a);

        _agora = 1000;
        var (_, av1) = cidade.Mover(1, a, new(1f, 0, -24f, 0));
        Assert.NotNull(av1);
        Assert.Equal(1, av1.SequenciaReplica);
        Assert.Equal(1000, av1.UltimoMs);

        _agora = 1100;
        var (_, av2) = cidade.Mover(1, a, new(2f, 0, -24f, 0));
        Assert.NotNull(av2);
        Assert.Equal(2, av2.SequenciaReplica);
        Assert.Equal(1100, av2.UltimoMs);

        _agora = 1200;
        cidade.ForcarPosicao(1, new(10f, 0, -24f, 0));
        var av3 = cidade.Obter(1);
        Assert.NotNull(av3);
        Assert.Equal(3, av3.SequenciaReplica);
        Assert.Equal(1200, av3.UltimoMs);
    }
}
