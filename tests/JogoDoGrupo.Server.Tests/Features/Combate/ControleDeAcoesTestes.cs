using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

public class ControleDeAcoesTestes
{
    private const long Ana = 1;
    private const long Beto = 2;

    private static PedidoDeAtaque Leve(int sequencia = 1) => new(TipoDeAtaque.Leve, 0f, sequencia);

    private static PedidoDeAtaque Pesado(int sequencia = 1) =>
        new(TipoDeAtaque.Pesado, 0f, sequencia);

    [Fact]
    public void Tempos_da_acao_vem_de_RegrasAtaque()
    {
        var controle = new ControleDeAcoes();

        controle.Pedir(Ana, Leve(), 1_000, out var leve);
        controle.Pedir(Beto, Pesado(), 1_000, out var pesado);

        Assert.Equal(500, leve!.PreparoMs);
        Assert.Equal(1_000 + 800, leve.FimMs);
        Assert.Equal(1_000, pesado!.PreparoMs);
        Assert.Equal(1_000 + 1_600, pesado.FimMs);
    }

    [Fact]
    public void Sem_acao_em_curso_o_ataque_comeca_na_hora()
    {
        var controle = new ControleDeAcoes();

        var resultado = controle.Pedir(Ana, Leve(7), 1_000, out var acao);

        Assert.Equal(ResultadoDoPedido.Iniciado, resultado);
        Assert.Equal(7, acao!.Pedido.Sequencia);
        Assert.True(controle.EstaAtiva(Ana, acao.Id));
    }

    [Fact]
    public void Dois_pedidos_no_mesmo_instante_so_um_comeca()
    {
        var controle = new ControleDeAcoes();

        var primeiro = controle.Pedir(Ana, Leve(1), 1_000, out _);
        var segundo = controle.Pedir(Ana, Leve(2), 1_000, out var acao);

        Assert.Equal(ResultadoDoPedido.Iniciado, primeiro);
        Assert.Equal(ResultadoDoPedido.Recusado, segundo);
        Assert.Null(acao);
    }

    [Fact]
    public void Durante_o_preparo_e_no_comeco_da_recuperacao_o_pedido_e_recusado()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out _); // termina em 1_800

        Assert.Equal(ResultadoDoPedido.Recusado, controle.Pedir(Ana, Leve(2), 1_200, out _));
        // Faltam 0,31 s: fora da janela de 0,3 s.
        Assert.Equal(ResultadoDoPedido.Recusado, controle.Pedir(Ana, Leve(3), 1_490, out _));
    }

    [Fact]
    public void Pedido_com_ate_0_3_s_para_acabar_entra_na_fila()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out var atual); // termina em 1_800

        var resultado = controle.Pedir(Ana, Leve(2), 1_500, out var esperada); // faltam 0,3 s

        Assert.Equal(ResultadoDoPedido.Enfileirado, resultado);
        Assert.Same(atual, esperada);
    }

    [Fact]
    public void So_cabe_um_pedido_na_fila()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out _);
        controle.Pedir(Ana, Leve(2), 1_600, out _);

        Assert.Equal(ResultadoDoPedido.Recusado, controle.Pedir(Ana, Leve(3), 1_700, out _));
    }

    [Fact]
    public void No_fim_da_acao_o_pedido_da_fila_comeca_e_so_uma_vez()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out var primeira);
        controle.Pedir(Ana, Pesado(2), 1_600, out _);

        var proxima = controle.AvancarFila(Ana, primeira!.Id, 1_800);
        var denovo = controle.AvancarFila(Ana, primeira.Id, 1_800);

        Assert.NotNull(proxima);
        Assert.Equal(2, proxima.Pedido.Sequencia);
        Assert.Equal(TipoDeAtaque.Pesado, proxima.Pedido.Tipo);
        Assert.Equal(1_800, proxima.InicioMs);
        Assert.Null(denovo);
        Assert.True(controle.EstaAtiva(Ana, proxima.Id));
        Assert.False(controle.EstaAtiva(Ana, primeira.Id));
    }

    [Fact]
    public void Fim_da_acao_sem_fila_nao_comeca_nada()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out var acao);

        Assert.Null(controle.AvancarFila(Ana, acao!.Id, 1_800));
    }

    [Fact]
    public void Depois_do_fim_da_acao_um_pedido_novo_comeca_normalmente()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out _);

        var resultado = controle.Pedir(Ana, Leve(2), 1_800, out var acao);

        Assert.Equal(ResultadoDoPedido.Iniciado, resultado);
        Assert.Equal(1_800, acao!.InicioMs);
    }

    [Fact]
    public void Pedido_recusado_pelo_servidor_libera_o_personagem()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Pesado(), 1_000, out var acao);

        var proxima = controle.Desistir(Ana, acao!.Id, 1_050);

        Assert.Null(proxima);
        Assert.False(controle.EstaAtiva(Ana, acao.Id));
        Assert.Equal(ResultadoDoPedido.Iniciado, controle.Pedir(Ana, Leve(2), 1_060, out _));
    }

    [Fact]
    public void Desistir_com_fila_comeca_o_pedido_da_fila_agora()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out var acao);
        controle.Pedir(Ana, Leve(2), 1_600, out _);

        var proxima = controle.Desistir(Ana, acao!.Id, 1_650);

        Assert.Equal(2, proxima!.Pedido.Sequencia);
        Assert.Equal(1_650, proxima.InicioMs);
    }

    [Fact]
    public void Desistir_de_acao_que_ja_foi_trocada_nao_mexe_em_nada()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out var velha);
        controle.Pedir(Ana, Leve(2), 1_800, out var nova);

        Assert.Null(controle.Desistir(Ana, velha!.Id, 1_900));
        Assert.True(controle.EstaAtiva(Ana, nova!.Id));
    }

    [Fact]
    public void Esquecer_cancela_a_acao_e_a_fila()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out var acao);
        controle.Pedir(Ana, Leve(2), 1_600, out _);

        controle.Esquecer(Ana);

        Assert.False(controle.EstaAtiva(Ana, acao!.Id));
        Assert.Null(controle.AvancarFila(Ana, acao.Id, 1_800));
        Assert.Equal(ResultadoDoPedido.Iniciado, controle.Pedir(Ana, Leve(3), 1_100, out _));
    }

    [Fact]
    public void Personagens_diferentes_nao_se_atrapalham_e_ids_de_acao_sao_unicos()
    {
        var controle = new ControleDeAcoes();

        controle.Pedir(Ana, Leve(), 1_000, out var a);
        var resultado = controle.Pedir(Beto, Leve(), 1_000, out var b);

        Assert.Equal(ResultadoDoPedido.Iniciado, resultado);
        Assert.NotEqual(a!.Id, b!.Id);
    }

    [Fact]
    public async Task Pedidos_simultaneos_do_mesmo_personagem_so_um_comeca()
    {
        var controle = new ControleDeAcoes();

        var resultados = await Task.WhenAll(
            Enumerable
                .Range(0, 50)
                .Select(i => Task.Run(() => controle.Pedir(Ana, Leve(i), 1_000, out _)))
        );

        Assert.Equal(1, resultados.Count(r => r == ResultadoDoPedido.Iniciado));
        Assert.Equal(49, resultados.Count(r => r == ResultadoDoPedido.Recusado));
    }

    // ── Cadeia de três leves (fatia-jogavel.md, seção 2) ──────────────────────

    private static AcaoDeAtaque Golpe(ControleDeAcoes controle, PedidoDeAtaque pedido, long agoraMs)
    {
        Assert.Equal(ResultadoDoPedido.Iniciado, controle.Pedir(Ana, pedido, agoraMs, out var a));
        return a!;
    }

    [Fact]
    public void Cadeia_sobe_do_1o_ao_3o_leve_e_o_4o_volta_ao_1o()
    {
        var controle = new ControleDeAcoes();

        var primeiro = Golpe(controle, Leve(), 1_000);
        var segundo = Golpe(controle, Leve(), primeiro.FimMs);
        var terceiro = Golpe(controle, Leve(), segundo.FimMs);
        var quarto = Golpe(controle, Leve(), terceiro.FimMs);

        Assert.Equal([0, 1, 2, 0], [primeiro.Passo, segundo.Passo, terceiro.Passo, quarto.Passo]);
        Assert.Equal(1.0, RegrasAtaque.Multiplicador(TipoDeAtaque.Leve, segundo.Passo));
        Assert.Equal(1.2, RegrasAtaque.Multiplicador(TipoDeAtaque.Leve, terceiro.Passo));
        Assert.Equal(1.0, RegrasAtaque.Multiplicador(TipoDeAtaque.Leve, quarto.Passo));
    }

    [Fact]
    public void Pesado_no_meio_da_cadeia_volta_a_cadeia_ao_1o()
    {
        var controle = new ControleDeAcoes();
        var primeiro = Golpe(controle, Leve(), 1_000);
        var segundo = Golpe(controle, Leve(), primeiro.FimMs);

        var pesado = Golpe(controle, Pesado(), segundo.FimMs);
        var depois = Golpe(controle, Leve(), pesado.FimMs);

        Assert.Equal(1, segundo.Passo);
        Assert.Equal(0, pesado.Passo);
        Assert.Equal(0, depois.Passo);
    }

    [Fact]
    public void Cadeia_expira_se_o_golpe_nao_comecar_em_ate_0_8_s_depois_do_fim()
    {
        var controle = new ControleDeAcoes();
        var primeiro = Golpe(controle, Leve(), 1_000);
        var dentro = Golpe(controle, Leve(), primeiro.FimMs + 800);
        var fora = Golpe(controle, Leve(), dentro.FimMs + 801);

        Assert.Equal(1, dentro.Passo);
        Assert.Equal(0, fora.Passo);
    }

    [Fact]
    public void Errar_o_alvo_nao_quebra_a_cadeia_porque_o_passo_sobe_ao_comecar()
    {
        // O controle não sabe se o golpe acertou: o 2º leve é o passo 1 mesmo sem acerto.
        var controle = new ControleDeAcoes();
        var primeiro = Golpe(controle, Leve(), 1_000);

        Assert.Equal(1, Golpe(controle, Leve(), primeiro.FimMs).Passo);
    }

    [Fact]
    public void Pedido_da_fila_continua_a_cadeia()
    {
        var controle = new ControleDeAcoes();
        var primeiro = Golpe(controle, Leve(), 1_000);
        controle.Pedir(Ana, Leve(2), primeiro.FimMs - 100, out _);

        var segundo = controle.AvancarFila(Ana, primeiro.Id, primeiro.FimMs);

        Assert.Equal(1, segundo!.Passo);
    }

    [Fact]
    public void Pesado_recusado_pelo_servidor_nao_quebra_a_cadeia()
    {
        var controle = new ControleDeAcoes();
        var primeiro = Golpe(controle, Leve(), 1_000);
        var pesado = Golpe(controle, Pesado(), primeiro.FimMs);

        controle.Desistir(Ana, pesado.Id, primeiro.FimMs + 20); // sem stamina

        Assert.Equal(1, Golpe(controle, Leve(), primeiro.FimMs + 30).Passo);
    }

    [Fact]
    public void Esquiva_corta_a_recuperacao_e_a_cadeia_conta_do_corte()
    {
        var controle = new ControleDeAcoes();
        var primeiro = Golpe(controle, Leve(), 1_000);
        controle.TentarOcupar(Ana, primeiro.AcertoMs, 600, out _); // recuperação cortada

        // 0,6 s de esquiva + 0,15 s: dentro da janela de 0,8 s contada do corte.
        Assert.Equal(1, Golpe(controle, Leve(), primeiro.AcertoMs + 750).Passo);
    }

    [Fact]
    public void Personagem_atordoado_recusa_ataque_com_codigo_atordoado()
    {
        var relogio = 1_000L;
        var efeitos = new GerenciadorDeEfeitosDeStatus(() => relogio);
        efeitos.Aplicar(Ana, TipoEfeitoDeStatus.Atordoado, duracaoSegundos: 2.0);
        var controle = new ControleDeAcoes(efeitos);

        Assert.True(controle.EstaAtordoado(Ana));

        var resultado = controle.Pedir(Ana, Leve(), relogio, out var acao);
        Assert.Equal(ResultadoDoPedido.Atordoado, resultado);
        Assert.Null(acao);
        Assert.False(controle.TentarOcupar(Ana, relogio, 600, out _));
        Assert.False(controle.TentarGuardar(Ana, relogio));

        // Após expirar o atordoamento, volta a poder agir normalmente.
        relogio += 2_100;
        Assert.False(controle.EstaAtordoado(Ana));
        var resultadoLivre = controle.Pedir(Ana, Leve(), relogio, out var acaoLivre);
        Assert.Equal(ResultadoDoPedido.Iniciado, resultadoLivre);
        Assert.NotNull(acaoLivre);
    }
}
