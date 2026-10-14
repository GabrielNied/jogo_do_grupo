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
    }

    [Fact]
    public void Rota_de_progresso_troca_o_id_do_modelo()
    {
        Assert.Equal(
            RotasPersonagem.Progresso.Replace("{personagemId}", "42"),
            RotasPersonagem.ProgressoDe(42)
        );
        Assert.StartsWith(RotasPersonagem.Personagens + "/", RotasPersonagem.ProgressoDe(42));
    }

    [Fact]
    public void Codigos_de_erro_novos_tem_o_texto_do_contrato()
    {
        Assert.Equal("PersonagemNaoEncontrado", CodigosErroPersonagem.PersonagemNaoEncontrado);
        Assert.Equal(
            "ConfirmacaoDeNomeIncorreta",
            CodigosErroPersonagem.ConfirmacaoDeNomeIncorreta
        );
        Assert.Equal("AparenciaInvalida", CodigosErroPersonagem.AparenciaInvalida);
    }

    [Fact]
    public void Rota_de_excluir_troca_o_id_do_modelo()
    {
        Assert.Equal(
            RotasPersonagem.Excluir.Replace("{personagemId}", "42"),
            RotasPersonagem.ExcluirDe(42)
        );
        Assert.StartsWith(RotasPersonagem.Personagens + "/", RotasPersonagem.ExcluirDe(42));
    }

    [Fact]
    public void AparenciaPersonagem_valores_padrao()
    {
        var aparencia = new AparenciaPersonagem();
        Assert.Equal(0, aparencia.TomDePele);
        Assert.Equal(0, aparencia.Cabelo);
        Assert.Equal(0, aparencia.CorDeCabelo);
    }

    [Fact]
    public void ExcluirPersonagemRequest_padrao()
    {
        var req = new ExcluirPersonagemRequest();
        Assert.Equal("", req.ConfirmacaoNome);
    }
}
