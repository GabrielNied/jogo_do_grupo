using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Shared.Tests.Features.Personagem;

public class ContratoPersonagemTestes
{
    [Theory]
    [InlineData(PersonagemMessageIds.ExperienciaGanha)]
    [InlineData(PersonagemMessageIds.SubiuDeNivel)]
    public void Id_esta_dentro_da_faixa_de_personagem(ushort id) =>
        Assert.True(
            PersonagemMessageIds.Faixa.Contem(id),
            $"ID {id} fora de PersonagemMessageIds.Faixa ({PersonagemMessageIds.Faixa.Inicio}–{PersonagemMessageIds.Faixa.Fim})"
        );

    [Fact]
    public void Ids_sao_os_do_contrato()
    {
        Assert.Equal(3000, PersonagemMessageIds.ExperienciaGanha);
        Assert.Equal(3001, PersonagemMessageIds.SubiuDeNivel);
    }

    [Fact]
    public void ExperienciaGanha_campos_padrao()
    {
        var msg = new ExperienciaGanha();
        Assert.Equal(0L, msg.Quantidade);
        Assert.Equal(0L, msg.XpAtual);
        Assert.Equal(0L, msg.XpDoNivel);
        Assert.Equal(1, msg.Nivel);
        Assert.Equal(0, msg.PontosLivres);
    }

    [Fact]
    public void SubiuDeNivel_campos_padrao()
    {
        var msg = new SubiuDeNivel();
        Assert.Equal(0L, msg.PersonagemId);
        Assert.Equal(1, msg.Nivel);
    }

    [Fact]
    public void ProgressoResponse_comeca_no_nivel_1_sem_pontos()
    {
        var resposta = new ProgressoResponse();
        Assert.Equal(1, resposta.Nivel);
        Assert.Equal(0L, resposta.XpAtual);
        Assert.Equal(0L, resposta.XpDoNivel);
        Assert.Equal(0, resposta.PontosLivres);
        Assert.NotNull(resposta.Distribuidos);
        Assert.Equal(0L, RegrasAtributos.Total(resposta.Distribuidos));
    }

    [Fact]
    public void DistribuirAtributosRequest_comeca_sem_pontos_e_nao_nulo()
    {
        var pedido = new DistribuirAtributosRequest();
        Assert.NotNull(pedido.Pontos);
        Assert.Equal(0L, RegrasAtributos.Total(pedido.Pontos));
    }

    [Fact]
    public void Rotas_de_progresso_e_atributos_trocam_o_id_do_modelo()
    {
        Assert.Equal(
            RotasPersonagem.Progresso.Replace("{personagemId}", "42"),
            RotasPersonagem.ProgressoDe(42)
        );
        Assert.Equal(
            RotasPersonagem.Atributos.Replace("{personagemId}", "42"),
            RotasPersonagem.AtributosDe(42)
        );
        Assert.StartsWith(RotasPersonagem.Personagens + "/", RotasPersonagem.ProgressoDe(42));
    }

    [Fact]
    public void Codigos_de_erro_novos_tem_o_texto_do_contrato()
    {
        Assert.Equal("PersonagemNaoEncontrado", CodigosErroPersonagem.PersonagemNaoEncontrado);
        Assert.Equal("AtributoInvalido", CodigosErroPersonagem.AtributoInvalido);
        Assert.Equal("PontosInsuficientes", CodigosErroPersonagem.PontosInsuficientes);
    }
}
