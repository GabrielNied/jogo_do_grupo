using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

/// <summary>A reserva que a esquiva faz no <see cref="ControleDeAcoes"/>, junto com os ataques.</summary>
public class ControleDeOcupacaoTestes
{
    private const long Ana = 1;
    private const long Beto = 2;
    private const long Esquiva = 600;

    private static PedidoDeAtaque Leve(int sequencia = 1) => new(TipoDeAtaque.Leve, 0f, sequencia);

    [Fact]
    public void Personagem_livre_e_ocupado_e_o_segundo_pedido_no_mesmo_instante_nao_vale()
    {
        var controle = new ControleDeAcoes();

        Assert.True(controle.TentarOcupar(Ana, 1_000, Esquiva, out var primeira));
        Assert.False(controle.TentarOcupar(Ana, 1_000, Esquiva, out var segunda));

        Assert.NotEqual(0, primeira);
        Assert.Equal(0, segunda);
    }

    [Fact]
    public void Ocupacao_vale_ate_o_fim_da_duracao()
    {
        var controle = new ControleDeAcoes();
        controle.TentarOcupar(Ana, 1_000, Esquiva, out _);

        Assert.False(controle.TentarOcupar(Ana, 1_599, Esquiva, out _));
        Assert.True(controle.TentarOcupar(Ana, 1_600, Esquiva, out _));
    }

    [Fact]
    public void Ocupacao_e_por_personagem()
    {
        var controle = new ControleDeAcoes();
        controle.TentarOcupar(Ana, 1_000, Esquiva, out _);

        Assert.True(controle.TentarOcupar(Beto, 1_000, Esquiva, out _));
    }

    [Fact]
    public void Nao_ocupa_no_preparo_e_ocupa_na_recuperacao_cortando_o_resto_da_acao()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out var ataque);

        Assert.False(controle.TentarOcupar(Ana, 1_000, Esquiva, out _));
        Assert.False(controle.TentarOcupar(Ana, ataque!.AcertoMs - 1, Esquiva, out _));

        Assert.True(controle.TentarOcupar(Ana, ataque.AcertoMs, Esquiva, out _));
        // A ação acabou ali: o resto da recuperação foi cortado.
        Assert.Equal(ataque.AcertoMs, controle.AcaoAtual(Ana)!.FimMs);
        Assert.True(ataque.FimMs > ataque.AcertoMs);
    }

    [Fact]
    public void Esquiva_na_recuperacao_limpa_a_fila_e_o_ataque_so_comeca_depois_dela()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out var ataque);
        controle.Pedir(Ana, Leve(2), ataque!.FimMs - 100, out _); // entra na fila

        controle.TentarOcupar(Ana, ataque.AcertoMs + 50, Esquiva, out _);

        Assert.Null(controle.AvancarFila(Ana, ataque.Id, ataque.FimMs));
        Assert.Equal(
            ResultadoDoPedido.Recusado,
            controle.Pedir(Ana, Leve(3), ataque.AcertoMs + 60, out _)
        );
        Assert.Equal(
            ResultadoDoPedido.Iniciado,
            controle.Pedir(Ana, Leve(3), ataque.AcertoMs + 50 + Esquiva, out _)
        );
    }

    [Fact]
    public void Esquiva_recusada_depois_de_cortar_devolve_a_recuperacao()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out var ataque);
        controle.TentarOcupar(Ana, ataque!.AcertoMs, Esquiva, out var ocupacao);

        controle.Desocupar(Ana, ocupacao); // sem stamina

        Assert.Equal(ataque.FimMs, controle.AcaoAtual(Ana)!.FimMs);
        Assert.False(controle.EstaInvulneravel(Ana, ataque.AcertoMs + 10));
    }

    [Fact]
    public void Dano_some_nos_primeiros_0_3_s_da_esquiva_e_volta_depois()
    {
        var controle = new ControleDeAcoes();
        controle.TentarOcupar(Ana, 1_000, Esquiva, out _);

        Assert.True(controle.EstaInvulneravel(Ana, 1_000));
        Assert.True(controle.EstaInvulneravel(Ana, 1_299));
        Assert.False(controle.EstaInvulneravel(Ana, 1_300));
        Assert.False(controle.EstaInvulneravel(Beto, 1_100));
    }

    [Fact]
    public void Ataque_durante_a_ocupacao_e_recusado_sem_entrar_na_fila()
    {
        var controle = new ControleDeAcoes();
        controle.TentarOcupar(Ana, 1_000, Esquiva, out _);

        // Nem nos últimos 0,3 s, em que um ataque em curso aceitaria a fila.
        Assert.Equal(ResultadoDoPedido.Recusado, controle.Pedir(Ana, Leve(), 1_000, out var a));
        Assert.Equal(ResultadoDoPedido.Recusado, controle.Pedir(Ana, Leve(), 1_500, out var b));
        Assert.Null(a);
        Assert.Null(b);
    }

    [Fact]
    public void Ataque_depois_da_ocupacao_comeca_na_hora()
    {
        var controle = new ControleDeAcoes();
        controle.TentarOcupar(Ana, 1_000, Esquiva, out _);

        Assert.Equal(ResultadoDoPedido.Iniciado, controle.Pedir(Ana, Leve(), 1_600, out var acao));
        Assert.Equal(1_600, acao!.InicioMs);
    }

    [Fact]
    public void Desocupar_libera_o_personagem_na_hora()
    {
        var controle = new ControleDeAcoes();
        controle.TentarOcupar(Ana, 1_000, Esquiva, out var ocupacao);

        controle.Desocupar(Ana, ocupacao);

        Assert.Equal(ResultadoDoPedido.Iniciado, controle.Pedir(Ana, Leve(), 1_001, out _));
    }

    [Fact]
    public void Desocupar_com_o_id_de_uma_ocupacao_antiga_nao_libera_a_nova()
    {
        var controle = new ControleDeAcoes();
        controle.TentarOcupar(Ana, 1_000, Esquiva, out var antiga);
        controle.TentarOcupar(Ana, 2_000, Esquiva, out var nova);

        controle.Desocupar(Ana, antiga);

        Assert.NotEqual(antiga, nova);
        Assert.False(controle.TentarOcupar(Ana, 2_100, Esquiva, out _));
    }

    [Fact]
    public void Desocupar_quem_nunca_ocupou_ou_foi_esquecido_nao_faz_nada()
    {
        var controle = new ControleDeAcoes();
        controle.Desocupar(Ana, 123);

        controle.TentarOcupar(Ana, 1_000, Esquiva, out var ocupacao);
        controle.Esquecer(Ana);
        controle.Desocupar(Ana, ocupacao);

        Assert.True(controle.TentarOcupar(Ana, 1_001, Esquiva, out _));
    }

    [Fact]
    public void Duracao_negativa_nao_prende_o_personagem()
    {
        var controle = new ControleDeAcoes();
        Assert.True(controle.TentarOcupar(Ana, 1_000, -50, out _));
        Assert.True(controle.TentarOcupar(Ana, 1_000, Esquiva, out _));
    }

    [Fact]
    public async Task De_varios_pedidos_simultaneos_de_esquiva_e_ataque_so_um_vale()
    {
        for (var rodada = 0; rodada < 50; rodada++)
        {
            var controle = new ControleDeAcoes();
            var pedidos = Enumerable
                .Range(0, 8)
                .Select(i =>
                    Task.Run(() =>
                        i % 2 == 0
                            ? controle.TentarOcupar(Ana, 1_000, Esquiva, out _)
                            : controle.Pedir(Ana, Leve(i), 1_000, out _)
                                == ResultadoDoPedido.Iniciado
                    )
                );

            var valeram = (await Task.WhenAll(pedidos)).Count(valeu => valeu);

            Assert.Equal(1, valeram);
        }
    }
}
