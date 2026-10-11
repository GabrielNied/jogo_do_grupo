using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Shared.Tests.Features.Social;

public class ContratoSocialTestes
{
    private static readonly ushort[] _ids =
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
    };

    private static readonly string[] _codigos =
    {
        CodigosErroSocial.AlvoNaoEncontrado,
        CodigosErroSocial.AlvoJaEmGrupo,
        CodigosErroSocial.ConviteParaSiMesmo,
        CodigosErroSocial.GrupoCheio,
        CodigosErroSocial.NaoELider,
        CodigosErroSocial.ConviteJaPendente,
        CodigosErroSocial.AlvoComConvitesDemais,
        CodigosErroSocial.ConvitesRapidosDemais,
        CodigosErroSocial.ConviteNaoEncontrado,
        CodigosErroSocial.ConviteExpirado,
        CodigosErroSocial.ConvidanteOffline,
        CodigosErroSocial.NaoEstaEmGrupo,
        CodigosErroSocial.AlvoNaoEstaNoGrupo,
        CodigosErroSocial.AlvoInvalido,
        CodigosErroSocial.NaoPodeExpulsarLider,
        CodigosErroSocial.ReconviteBloqueado,
    };

    [Fact]
    public void Ids_estao_dentro_da_faixa_de_social() =>
        Assert.All(
            _ids,
            id => Assert.True(SocialMessageIds.Faixa.Contem(id), $"ID {id} fora da faixa de Social")
        );

    [Fact]
    public void Ids_de_social_sao_distintos() =>
        Assert.Equal(_ids.Length, _ids.Distinct().Count());

    [Fact]
    public void Ids_do_grupo_tem_os_numeros_do_contrato()
    {
        Assert.Equal(13000, SocialMessageIds.ConvidarParaGrupo);
        Assert.Equal(13001, SocialMessageIds.ResponderConvite);
        Assert.Equal(13002, SocialMessageIds.SairDoGrupo);
        Assert.Equal(13003, SocialMessageIds.ExpulsarDoGrupo);
        Assert.Equal(13004, SocialMessageIds.PassarLideranca);
        Assert.Equal(13010, SocialMessageIds.ConviteRecebido);
        Assert.Equal(13011, SocialMessageIds.ListaDoGrupo);
        Assert.Equal(13012, SocialMessageIds.AvisoDoGrupo);
        Assert.Equal(13013, SocialMessageIds.OperacaoDoGrupoRecusada);
    }

    [Fact]
    public void Codigos_de_recusa_sao_distintos_e_sao_os_16_da_doc()
    {
        Assert.Equal(16, _codigos.Length);
        Assert.Equal(_codigos.Length, _codigos.Distinct().Count());
        Assert.All(_codigos, codigo => Assert.False(string.IsNullOrWhiteSpace(codigo)));
    }

    [Fact]
    public void Codigo_de_recusa_tem_o_mesmo_texto_do_nome()
    {
        Assert.Equal("AlvoNaoEncontrado", CodigosErroSocial.AlvoNaoEncontrado);
        Assert.Equal("GrupoCheio", CodigosErroSocial.GrupoCheio);
        Assert.Equal("NaoELider", CodigosErroSocial.NaoELider);
        Assert.Equal("ReconviteBloqueado", CodigosErroSocial.ReconviteBloqueado);
    }

    [Fact]
    public void TipoDeAvisoDoGrupo_tem_valores_estaveis()
    {
        Assert.Equal(0, (int)TipoDeAvisoDoGrupo.ConviteRecusado);
        Assert.Equal(1, (int)TipoDeAvisoDoGrupo.ConviteExpirou);
        Assert.Equal(2, (int)TipoDeAvisoDoGrupo.EntrouNoGrupo);
        Assert.Equal(3, (int)TipoDeAvisoDoGrupo.SaiuDoGrupo);
        Assert.Equal(4, (int)TipoDeAvisoDoGrupo.LiderMudou);
        Assert.Equal(5, (int)TipoDeAvisoDoGrupo.ExpulsoDoGrupo);
        Assert.Equal(6, (int)TipoDeAvisoDoGrupo.GrupoDesfeito);
        Assert.Equal(7, (int)TipoDeAvisoDoGrupo.MembroOffline);
        Assert.Equal(8, (int)TipoDeAvisoDoGrupo.MembroVoltou);
    }

    [Fact]
    public void Pedidos_do_client_campos_padrao()
    {
        Assert.Equal("", new ConvidarParaGrupo().Nome);
        var resposta = new ResponderConvite();
        Assert.Equal(0L, resposta.ConviteId);
        Assert.False(resposta.Aceitar);
        Assert.NotNull(new SairDoGrupo());
        Assert.Equal(0L, new ExpulsarDoGrupo().PersonagemId);
        Assert.Equal(0L, new PassarLideranca().PersonagemId);
    }

    [Fact]
    public void ConviteRecebido_campos_padrao()
    {
        var msg = new ConviteRecebido();
        Assert.Equal(0L, msg.ConviteId);
        Assert.Equal("", msg.NomeDoConvidante);
        Assert.Equal(0, msg.ValidadeSegundos);
    }

    [Fact]
    public void ListaDoGrupo_padrao_e_sem_grupo_com_membros_vazios_e_nao_nulos()
    {
        var msg = new ListaDoGrupo();
        Assert.Equal(0L, msg.GrupoId);
        Assert.Equal(0L, msg.LiderId);
        Assert.NotNull(msg.Membros);
        Assert.Empty(msg.Membros);
    }

    [Fact]
    public void MembroDoGrupo_campos_padrao()
    {
        var membro = new MembroDoGrupo();
        Assert.Equal(0L, membro.PersonagemId);
        Assert.Equal("", membro.Nome);
        Assert.False(membro.Online);
    }

    [Fact]
    public void AvisoDoGrupo_e_recusa_campos_padrao_nao_nulos()
    {
        var aviso = new AvisoDoGrupo();
        Assert.Equal(0L, aviso.PersonagemId);
        Assert.Equal("", aviso.Nome);
        Assert.Equal("", new OperacaoDoGrupoRecusada().Codigo);
    }
}
