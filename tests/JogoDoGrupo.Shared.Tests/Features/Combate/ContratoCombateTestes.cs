using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class ContratoCombateTestes
{
    private static readonly ushort[] _ids =
    {
        CombateMessageIds.IntencaoDeAtaque,
        CombateMessageIds.AtaqueIniciado,
        CombateMessageIds.AtaqueAcertou,
        CombateMessageIds.AtaqueRecusado,
        CombateMessageIds.RecursosMudaram,
    };

    [Theory]
    [InlineData(CombateMessageIds.IntencaoDeAtaque)]
    [InlineData(CombateMessageIds.AtaqueIniciado)]
    [InlineData(CombateMessageIds.AtaqueAcertou)]
    [InlineData(CombateMessageIds.AtaqueRecusado)]
    [InlineData(CombateMessageIds.RecursosMudaram)]
    public void Id_esta_dentro_da_faixa_do_combate(ushort id) =>
        Assert.True(
            CombateMessageIds.Faixa.Contem(id),
            $"ID {id} fora de CombateMessageIds.Faixa ({CombateMessageIds.Faixa.Inicio}–{CombateMessageIds.Faixa.Fim})"
        );

    [Fact]
    public void Ids_de_combate_sao_distintos() =>
        Assert.Equal(_ids.Length, _ids.Distinct().Count());

    [Fact]
    public void Ids_de_ataque_ficam_fora_dos_reservados_para_a_esquiva()
    {
        // A esquiva (#62) usa 6010 a 6012.
        Assert.All(_ids, id => Assert.True(id < 6010, $"ID {id} invade a faixa da esquiva"));
    }

    [Fact]
    public void TipoDeAtaque_tem_valores_estaveis()
    {
        Assert.Equal(0, (int)TipoDeAtaque.Leve);
        Assert.Equal(1, (int)TipoDeAtaque.Pesado);
    }

    [Fact]
    public void Codigos_de_recusa_tem_o_texto_do_contrato()
    {
        Assert.Equal("StaminaInsuficiente", CodigosErroCombate.StaminaInsuficiente);
        Assert.Equal("AcaoEmAndamento", CodigosErroCombate.AcaoEmAndamento);
        Assert.Equal("VoceEstaCaido", CodigosErroCombate.VoceEstaCaido);
    }

    [Fact]
    public void IntencaoDeAtaque_campos_padrao()
    {
        var msg = new IntencaoDeAtaque();
        Assert.Equal(TipoDeAtaque.Leve, msg.Tipo);
        Assert.Equal(0f, msg.DirecaoY);
        Assert.Equal(0, msg.Sequencia);
    }

    [Fact]
    public void AtaqueIniciado_campos_padrao()
    {
        var msg = new AtaqueIniciado();
        Assert.Equal(0L, msg.AtacanteId);
        Assert.Equal(TipoDeAtaque.Leve, msg.Tipo);
        Assert.Equal(0f, msg.DirecaoY);
        Assert.Equal(0, msg.PreparoMs);
    }

    [Fact]
    public void AtaqueAcertou_acertos_comecam_vazios_e_nao_nulos()
    {
        var msg = new AtaqueAcertou();
        Assert.Equal(0L, msg.AtacanteId);
        Assert.NotNull(msg.Acertos);
        Assert.Empty(msg.Acertos);
    }

    [Fact]
    public void AtaqueAcertou_traz_tipo_sequencia_e_passo_do_golpe()
    {
        var msg = new AtaqueAcertou
        {
            Tipo = TipoDeAtaque.Pesado,
            Sequencia = 8,
            Passo = 2,
        };

        Assert.Equal(TipoDeAtaque.Pesado, msg.Tipo);
        Assert.Equal(8, msg.Sequencia);
        Assert.Equal(2, msg.Passo);
        Assert.Equal(TipoDeAtaque.Leve, new AtaqueAcertou().Tipo);
    }

    [Fact]
    public void Boneco_de_treino_tem_um_alvo_negativo_na_cidade()
    {
        // Id negativo: nunca colide com personagem nem inimigo.
        Assert.True(RegrasBonecoDeTreino.AlvoId < 0);
        Assert.True(RegrasBonecoDeTreino.Vida > 0);
        Assert.True(
            RegrasAtaque.EstaNoVolume(
                RegrasBonecoDeTreino.X,
                RegrasBonecoDeTreino.Z - RegrasAtaque.AlcanceMetros,
                0.0,
                RegrasBonecoDeTreino.X,
                RegrasBonecoDeTreino.Z,
                RegrasBonecoDeTreino.Raio
            )
        );
    }

    [Fact]
    public void AcertoDeAtaque_campos_padrao()
    {
        var acerto = new AcertoDeAtaque();
        Assert.Equal(0L, acerto.AlvoId);
        Assert.Equal(0, acerto.Dano);
        Assert.Equal(0, acerto.VidaRestante);
        Assert.Equal(0, acerto.VidaMaxima);
    }

    [Fact]
    public void AtaqueRecusado_codigo_padrao_nao_nulo()
    {
        var msg = new AtaqueRecusado();
        Assert.Equal(0, msg.Sequencia);
        Assert.Equal("", msg.Codigo);
    }

    [Fact]
    public void RecursosMudaram_campos_padrao()
    {
        var msg = new RecursosMudaram();
        Assert.Equal(0, msg.VidaAtual);
        Assert.Equal(0, msg.VidaMaxima);
        Assert.Equal(0, msg.StaminaAtual);
        Assert.Equal(0, msg.StaminaMaxima);
        Assert.Equal(0, msg.ManaAtual);
        Assert.Equal(0, msg.ManaMaxima);
        Assert.Equal(0, msg.RegeneracaoStamina);
    }
}
