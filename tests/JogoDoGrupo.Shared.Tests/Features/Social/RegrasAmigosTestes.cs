using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Shared.Tests.Features.Social;

public class RegrasAmigosTestes
{
    private const double SemPressa = 60.0;

    /// <summary>Um pedido que passa: muda só o que o teste quer conferir.</summary>
    private static string Pedir(
        bool paraSiMesmo = false,
        RelacaoDeAmizade relacao = RelacaoDeAmizade.Nenhuma,
        bool alvoIgnoradoPorQuemPede = false,
        bool quemPedeIgnoradoPeloAlvo = false,
        int amigos = 0,
        int pedidosEnviados = 0,
        int pedidosRecebidosDoAlvo = 0,
        double segundosDesdeOUltimoPedido = SemPressa
    ) =>
        RegrasAmigos.RecusaDoPedido(
            paraSiMesmo,
            relacao,
            alvoIgnoradoPorQuemPede,
            quemPedeIgnoradoPeloAlvo,
            amigos,
            pedidosEnviados,
            pedidosRecebidosDoAlvo,
            segundosDesdeOUltimoPedido
        );

    // ── Valores padrão (social.md) ────────────────────────────────────────────

    [Fact]
    public void Valores_padrao_de_amigos_e_ignorados()
    {
        Assert.Equal(100, RegrasAmigos.MaximoDeAmigos);
        Assert.Equal(50, RegrasAmigos.MaximoDePedidosRecebidos);
        Assert.Equal(50, RegrasAmigos.MaximoDeIgnorados);
        Assert.Equal(1.0, RegrasAmigos.IntervaloEntrePedidosSegundos);
        Assert.Equal("", RegrasAmigos.SemRecusa);
    }

    // ── Vagas ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0, 100)]
    [InlineData(99, 0, 1)]
    [InlineData(100, 0, 0)]
    [InlineData(60, 39, 1)]
    [InlineData(60, 40, 0)]
    [InlineData(0, 100, 0)]
    public void VagasLivres_desconta_amigos_e_pedidos_enviados(
        int amigos,
        int enviados,
        int esperado
    ) => Assert.Equal(esperado, RegrasAmigos.VagasLivres(amigos, enviados));

    [Theory]
    [InlineData(101, 0)]
    [InlineData(100, 5)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void VagasLivres_nunca_e_negativo(int amigos, int enviados) =>
        Assert.Equal(0, RegrasAmigos.VagasLivres(amigos, enviados));

    [Fact]
    public void VagasLivres_trata_numero_negativo_como_zero() =>
        Assert.Equal(100, RegrasAmigos.VagasLivres(-3, -1));

    [Theory]
    [InlineData(99, 0, true)]
    [InlineData(98, 1, true)]
    [InlineData(100, 0, false)]
    [InlineData(99, 1, false)]
    public void Centesimo_amigo_cabe_e_o_seguinte_nao(int amigos, int enviados, bool cabe) =>
        Assert.Equal(cabe, RegrasAmigos.TemVaga(amigos, enviados));

    [Theory]
    [InlineData(49, false)]
    [InlineData(50, true)]
    [InlineData(51, true)]
    public void Alvo_recebe_ate_50_pedidos_sem_resposta(int recebidos, bool demais) =>
        Assert.Equal(demais, RegrasAmigos.AlvoComPedidosDemais(recebidos));

    [Theory]
    [InlineData(0, true)]
    [InlineData(49, true)]
    [InlineData(50, false)]
    [InlineData(51, false)]
    public void Lista_de_ignorados_aceita_ate_50(int ignorados, bool cabe) =>
        Assert.Equal(cabe, RegrasAmigos.CabeMaisUmIgnorado(ignorados));

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(0.99, true)]
    [InlineData(1.0, false)]
    [InlineData(30.0, false)]
    public void Pedido_antes_de_1_segundo_e_rapido_demais(double segundos, bool rapido) =>
        Assert.Equal(rapido, RegrasAmigos.PedidoRapidoDemais(segundos));

    // ── Pedido de amizade ─────────────────────────────────────────────────────

    [Fact]
    public void Pedido_comum_passa() => Assert.Equal(RegrasAmigos.SemRecusa, Pedir());

    [Fact]
    public void Pedido_para_a_propria_conta_e_recusado() =>
        Assert.Equal(CodigosErroSocial.AmizadeConsigoMesmo, Pedir(paraSiMesmo: true));

    [Fact]
    public void Pedido_para_quem_voce_ignora_e_recusado() =>
        Assert.Equal(CodigosErroSocial.AlvoIgnorado, Pedir(alvoIgnoradoPorQuemPede: true));

    [Fact]
    public void Pedido_para_quem_ja_e_amigo_e_recusado() =>
        Assert.Equal(CodigosErroSocial.JaSaoAmigos, Pedir(relacao: RelacaoDeAmizade.Amigos));

    [Fact]
    public void Pedido_duplicado_e_recusado_sem_criar_outro() =>
        Assert.Equal(
            CodigosErroSocial.PedidoJaPendente,
            Pedir(relacao: RelacaoDeAmizade.PedidoEnviado)
        );

    [Fact]
    public void Pedido_rapido_demais_e_recusado() =>
        Assert.Equal(
            CodigosErroSocial.PedidosRapidosDemais,
            Pedir(segundosDesdeOUltimoPedido: 0.5)
        );

    [Theory]
    [InlineData(100, 0)]
    [InlineData(70, 30)]
    public void Pedido_com_a_lista_cheia_e_recusado(int amigos, int enviados) =>
        Assert.Equal(
            CodigosErroSocial.ListaDeAmigosCheia,
            Pedir(amigos: amigos, pedidosEnviados: enviados)
        );

    [Fact]
    public void Quem_foi_ignorado_recebe_a_mesma_resposta_de_nome_inexistente() =>
        Assert.Equal(CodigosErroSocial.AlvoNaoEncontrado, Pedir(quemPedeIgnoradoPeloAlvo: true));

    [Fact]
    public void Pedido_para_alvo_com_50_pedidos_e_recusado()
    {
        Assert.Equal(RegrasAmigos.SemRecusa, Pedir(pedidosRecebidosDoAlvo: 49));
        Assert.Equal(CodigosErroSocial.AlvoComPedidosDemais, Pedir(pedidosRecebidosDoAlvo: 50));
    }

    [Fact]
    public void Ignorado_nao_descobre_que_o_alvo_esta_com_pedidos_demais() =>
        Assert.Equal(
            CodigosErroSocial.AlvoNaoEncontrado,
            Pedir(quemPedeIgnoradoPeloAlvo: true, pedidosRecebidosDoAlvo: 50)
        );

    [Fact]
    public void Erro_de_quem_pede_vem_antes_do_que_revela_algo_do_alvo()
    {
        Assert.Equal(
            CodigosErroSocial.AmizadeConsigoMesmo,
            Pedir(paraSiMesmo: true, alvoIgnoradoPorQuemPede: true, amigos: 100)
        );
        Assert.Equal(
            CodigosErroSocial.AlvoIgnorado,
            Pedir(alvoIgnoradoPorQuemPede: true, relacao: RelacaoDeAmizade.Amigos)
        );
        Assert.Equal(
            CodigosErroSocial.PedidosRapidosDemais,
            Pedir(segundosDesdeOUltimoPedido: 0.0, amigos: 100, quemPedeIgnoradoPeloAlvo: true)
        );
        Assert.Equal(
            CodigosErroSocial.ListaDeAmigosCheia,
            Pedir(amigos: 100, quemPedeIgnoradoPeloAlvo: true)
        );
    }

    // ── Pedido cruzado e aceite ───────────────────────────────────────────────

    [Theory]
    [InlineData(RelacaoDeAmizade.Nenhuma, false)]
    [InlineData(RelacaoDeAmizade.PedidoEnviado, false)]
    [InlineData(RelacaoDeAmizade.PedidoRecebido, true)]
    [InlineData(RelacaoDeAmizade.Amigos, false)]
    public void So_o_pedido_para_quem_ja_pediu_vira_aceite(RelacaoDeAmizade relacao, bool vira) =>
        Assert.Equal(vira, RegrasAmigos.PedidoViraAceite(relacao));

    [Fact]
    public void Pedido_cruzado_passa_mesmo_com_o_alvo_cheio_de_pedidos() =>
        Assert.Equal(
            RegrasAmigos.SemRecusa,
            Pedir(relacao: RelacaoDeAmizade.PedidoRecebido, pedidosRecebidosDoAlvo: 50)
        );

    [Fact]
    public void Pedido_cruzado_ainda_precisa_de_vaga_de_quem_pede() =>
        Assert.Equal(
            CodigosErroSocial.ListaDeAmigosCheia,
            Pedir(relacao: RelacaoDeAmizade.PedidoRecebido, amigos: 100)
        );

    [Theory]
    [InlineData(0, 0, "")]
    [InlineData(99, 0, "")]
    [InlineData(100, 0, CodigosErroSocial.ListaDeAmigosCheia)]
    [InlineData(90, 10, CodigosErroSocial.ListaDeAmigosCheia)]
    public void Aceite_so_e_recusado_com_a_lista_de_quem_aceita_cheia(
        int amigos,
        int enviados,
        string esperado
    ) => Assert.Equal(esperado, RegrasAmigos.RecusaDoAceite(amigos, enviados));

    // ── Ignorar ───────────────────────────────────────────────────────────────

    [Fact]
    public void Ignorar_comum_passa() =>
        Assert.Equal(RegrasAmigos.SemRecusa, RegrasAmigos.RecusaDeIgnorar(false, false, 0));

    [Fact]
    public void Ignorar_a_propria_conta_e_recusado() =>
        Assert.Equal(
            CodigosErroSocial.IgnorarASiMesmo,
            RegrasAmigos.RecusaDeIgnorar(true, false, 0)
        );

    [Fact]
    public void Ignorar_duas_vezes_o_mesmo_jogador_e_recusado() =>
        Assert.Equal(CodigosErroSocial.JaIgnorado, RegrasAmigos.RecusaDeIgnorar(false, true, 1));

    [Fact]
    public void Ignorar_com_a_lista_cheia_e_recusado()
    {
        Assert.Equal(RegrasAmigos.SemRecusa, RegrasAmigos.RecusaDeIgnorar(false, false, 49));
        Assert.Equal(
            CodigosErroSocial.ListaDeIgnoradosCheia,
            RegrasAmigos.RecusaDeIgnorar(false, false, 50)
        );
    }

    [Fact]
    public void Ja_ignorado_vem_antes_de_lista_cheia() =>
        Assert.Equal(CodigosErroSocial.JaIgnorado, RegrasAmigos.RecusaDeIgnorar(false, true, 50));

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void Contato_e_barrado_se_qualquer_um_dos_dois_ignora_o_outro(
        bool origemIgnora,
        bool alvoIgnora,
        bool barrado
    ) => Assert.Equal(barrado, RegrasAmigos.ContatoBarrado(origemIgnora, alvoIgnora));
}
