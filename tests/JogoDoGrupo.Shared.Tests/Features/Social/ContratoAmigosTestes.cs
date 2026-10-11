using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Shared.Tests.Features.Social;

public class ContratoAmigosTestes
{
    private static readonly ushort[] _idsDeAmigos =
    {
        SocialMessageIds.PedirAmizade,
        SocialMessageIds.ResponderPedidoDeAmizade,
        SocialMessageIds.CancelarPedidoDeAmizade,
        SocialMessageIds.RemoverAmigo,
        SocialMessageIds.IgnorarJogador,
        SocialMessageIds.DeixarDeIgnorar,
        SocialMessageIds.ListaDeAmigos,
        SocialMessageIds.AmigoMudouDeEstado,
        SocialMessageIds.ListaDeIgnorados,
        SocialMessageIds.OperacaoDeAmizadeRecusada,
    };

    private static readonly ushort[] _idsDoGrupoEDoChat =
    {
        SocialMessageIds.ConvidarParaGrupo,
        SocialMessageIds.ResponderConvite,
        SocialMessageIds.SairDoGrupo,
        SocialMessageIds.ExpulsarDoGrupo,
        SocialMessageIds.PassarLideranca,
        SocialMessageIds.ConviteRecebido,
        SocialMessageIds.ListaDoGrupo,
        SocialMessageIds.AvisoDoGrupo,
        SocialMessageIds.OperacaoDoGrupoRecusada,
        SocialMessageIds.EnviarMensagemDeChat,
        SocialMessageIds.MensagemDeChat,
        SocialMessageIds.MensagemDeChatRecusada,
        SocialMessageIds.HistoricoDaCidade,
    };

    private static readonly string[] _codigosDeAmigos =
    {
        CodigosErroSocial.AmizadeConsigoMesmo,
        CodigosErroSocial.AlvoIgnorado,
        CodigosErroSocial.JaSaoAmigos,
        CodigosErroSocial.PedidoJaPendente,
        CodigosErroSocial.PedidosRapidosDemais,
        CodigosErroSocial.ListaDeAmigosCheia,
        CodigosErroSocial.AlvoComPedidosDemais,
        CodigosErroSocial.PedidoNaoEncontrado,
        CodigosErroSocial.AmigoNaoEncontrado,
        CodigosErroSocial.IgnorarASiMesmo,
        CodigosErroSocial.JaIgnorado,
        CodigosErroSocial.ListaDeIgnoradosCheia,
        CodigosErroSocial.IgnoradoNaoEncontrado,
    };

    [Fact]
    public void Ids_de_amigos_estao_dentro_da_faixa_de_social() =>
        Assert.All(
            _idsDeAmigos,
            id => Assert.True(SocialMessageIds.Faixa.Contem(id), $"ID {id} fora da faixa de Social")
        );

    [Fact]
    public void Ids_de_amigos_ficam_no_bloco_13200_a_13299() =>
        Assert.All(_idsDeAmigos, id => Assert.InRange(id, (ushort)13200, (ushort)13299));

    [Fact]
    public void Ids_de_amigos_nao_repetem_entre_si_nem_com_grupo_e_chat()
    {
        var todos = _idsDeAmigos.Concat(_idsDoGrupoEDoChat).ToArray();
        Assert.Equal(todos.Length, todos.Distinct().Count());
    }

    [Fact]
    public void Ids_de_amigos_tem_os_numeros_do_contrato()
    {
        Assert.Equal(13200, SocialMessageIds.PedirAmizade);
        Assert.Equal(13201, SocialMessageIds.ResponderPedidoDeAmizade);
        Assert.Equal(13202, SocialMessageIds.CancelarPedidoDeAmizade);
        Assert.Equal(13203, SocialMessageIds.RemoverAmigo);
        Assert.Equal(13204, SocialMessageIds.IgnorarJogador);
        Assert.Equal(13205, SocialMessageIds.DeixarDeIgnorar);
        Assert.Equal(13210, SocialMessageIds.ListaDeAmigos);
        Assert.Equal(13211, SocialMessageIds.AmigoMudouDeEstado);
        Assert.Equal(13212, SocialMessageIds.ListaDeIgnorados);
        Assert.Equal(13213, SocialMessageIds.OperacaoDeAmizadeRecusada);
    }

    [Fact]
    public void Codigos_de_amigos_sao_distintos_e_sao_os_13_da_doc()
    {
        Assert.Equal(13, _codigosDeAmigos.Length);
        Assert.Equal(_codigosDeAmigos.Length, _codigosDeAmigos.Distinct().Count());
        Assert.All(_codigosDeAmigos, codigo => Assert.False(string.IsNullOrWhiteSpace(codigo)));
    }

    [Fact]
    public void Codigo_de_amigos_tem_o_mesmo_texto_do_nome()
    {
        Assert.Equal("AmizadeConsigoMesmo", CodigosErroSocial.AmizadeConsigoMesmo);
        Assert.Equal("AlvoIgnorado", CodigosErroSocial.AlvoIgnorado);
        Assert.Equal("JaSaoAmigos", CodigosErroSocial.JaSaoAmigos);
        Assert.Equal("PedidoJaPendente", CodigosErroSocial.PedidoJaPendente);
        Assert.Equal("PedidosRapidosDemais", CodigosErroSocial.PedidosRapidosDemais);
        Assert.Equal("ListaDeAmigosCheia", CodigosErroSocial.ListaDeAmigosCheia);
        Assert.Equal("AlvoComPedidosDemais", CodigosErroSocial.AlvoComPedidosDemais);
        Assert.Equal("PedidoNaoEncontrado", CodigosErroSocial.PedidoNaoEncontrado);
        Assert.Equal("AmigoNaoEncontrado", CodigosErroSocial.AmigoNaoEncontrado);
        Assert.Equal("IgnorarASiMesmo", CodigosErroSocial.IgnorarASiMesmo);
        Assert.Equal("JaIgnorado", CodigosErroSocial.JaIgnorado);
        Assert.Equal("ListaDeIgnoradosCheia", CodigosErroSocial.ListaDeIgnoradosCheia);
        Assert.Equal("IgnoradoNaoEncontrado", CodigosErroSocial.IgnoradoNaoEncontrado);
    }

    [Fact]
    public void Codigos_de_amigos_nao_repetem_os_do_grupo_com_nome_parecido()
    {
        Assert.NotEqual(
            CodigosErroSocial.AlvoComConvitesDemais,
            CodigosErroSocial.AlvoComPedidosDemais
        );
        Assert.NotEqual(
            CodigosErroSocial.ConvitesRapidosDemais,
            CodigosErroSocial.PedidosRapidosDemais
        );
        Assert.NotEqual(CodigosErroSocial.ConviteJaPendente, CodigosErroSocial.PedidoJaPendente);
    }

    [Fact]
    public void RelacaoDeAmizade_tem_valores_estaveis()
    {
        Assert.Equal(0, (int)RelacaoDeAmizade.Nenhuma);
        Assert.Equal(1, (int)RelacaoDeAmizade.PedidoEnviado);
        Assert.Equal(2, (int)RelacaoDeAmizade.PedidoRecebido);
        Assert.Equal(3, (int)RelacaoDeAmizade.Amigos);
    }

    [Fact]
    public void Pedidos_do_client_campos_padrao()
    {
        Assert.Equal("", new PedirAmizade().Nome);
        var resposta = new ResponderPedidoDeAmizade();
        Assert.Equal(0L, resposta.PedidoId);
        Assert.False(resposta.Aceitar);
        Assert.Equal(0L, new CancelarPedidoDeAmizade().PedidoId);
        Assert.Equal(0L, new RemoverAmigo().AmigoId);
        Assert.Equal("", new IgnorarJogador().Nome);
        Assert.Equal(0L, new DeixarDeIgnorar().IgnoradoId);
    }

    [Fact]
    public void ListaDeAmigos_padrao_e_vazia_e_sem_nulos()
    {
        var msg = new ListaDeAmigos();
        Assert.NotNull(msg.Amigos);
        Assert.Empty(msg.Amigos);
        Assert.NotNull(msg.PedidosRecebidos);
        Assert.Empty(msg.PedidosRecebidos);
        Assert.NotNull(msg.PedidosEnviados);
        Assert.Empty(msg.PedidosEnviados);
    }

    [Fact]
    public void AmigoDaLista_padrao_e_offline_sem_personagem()
    {
        var amigo = new AmigoDaLista();
        Assert.Equal(0L, amigo.AmigoId);
        Assert.Equal("", amigo.Nome);
        Assert.False(amigo.Online);
        Assert.Equal("", amigo.PersonagemOnline);
    }

    [Fact]
    public void PedidoDeAmizade_e_AmigoMudouDeEstado_campos_padrao()
    {
        var pedido = new PedidoDeAmizade();
        Assert.Equal(0L, pedido.PedidoId);
        Assert.Equal("", pedido.Nome);

        var estado = new AmigoMudouDeEstado();
        Assert.Equal(0L, estado.AmigoId);
        Assert.False(estado.Online);
        Assert.Equal("", estado.PersonagemOnline);
    }

    [Fact]
    public void Ignorados_e_recusa_campos_padrao_nao_nulos()
    {
        var lista = new ListaDeIgnorados();
        Assert.NotNull(lista.Ignorados);
        Assert.Empty(lista.Ignorados);

        var ignorado = new JogadorIgnorado();
        Assert.Equal(0L, ignorado.IgnoradoId);
        Assert.Equal("", ignorado.Nome);

        Assert.Equal("", new OperacaoDeAmizadeRecusada().Codigo);
    }
}
