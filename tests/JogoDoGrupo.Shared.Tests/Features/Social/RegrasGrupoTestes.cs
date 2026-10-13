using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Shared.Tests.Features.Social;

public class RegrasGrupoTestes
{
    // ── Valores padrão (social.md) ────────────────────────────────────────────

    [Fact]
    public void Valores_padrao_do_grupo()
    {
        Assert.Equal(5, RegrasGrupo.MaximoDeMembros);
        Assert.Equal(2, RegrasGrupo.MinimoDeMembros);
        Assert.Equal(60, RegrasGrupo.ValidadeDoConviteSegundos);
        Assert.Equal(60, RegrasGrupo.VagaGuardadaSegundos);
        Assert.Equal(30, RegrasGrupo.BloqueioDeReconviteSegundos);
        Assert.Equal(1.0, RegrasGrupo.IntervaloEntreConvitesSegundos);
        Assert.Equal(5, RegrasGrupo.MaximoDeConvitesPorAlvo);
        Assert.Equal(4, RegrasGrupo.MaximoDeConvitesPorConvidante);
    }

    // ── Vagas ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1, 0, 4)]
    [InlineData(1, 4, 0)]
    [InlineData(4, 0, 1)]
    [InlineData(4, 1, 0)]
    [InlineData(5, 0, 0)]
    [InlineData(3, 1, 1)]
    public void VagasLivres_desconta_membros_e_convites_pendentes(
        int membros,
        int pendentes,
        int esperado
    ) => Assert.Equal(esperado, RegrasGrupo.VagasLivres(membros, pendentes));

    [Theory]
    [InlineData(6, 0)]
    [InlineData(5, 3)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void VagasLivres_nunca_e_negativo(int membros, int pendentes) =>
        Assert.Equal(0, RegrasGrupo.VagasLivres(membros, pendentes));

    [Fact]
    public void VagasLivres_trata_numero_negativo_como_zero() =>
        Assert.Equal(5, RegrasGrupo.VagasLivres(-3, -1));

    [Fact]
    public void Grupo_de_4_so_tem_um_convite_pendente_por_vez()
    {
        Assert.True(RegrasGrupo.CabeMaisUmConvite(4, 0));
        Assert.False(RegrasGrupo.CabeMaisUmConvite(4, 1));
    }

    [Fact]
    public void Convidante_sem_grupo_tem_ate_4_convites_pendentes()
    {
        Assert.True(RegrasGrupo.CabeMaisUmConvite(1, 3));
        Assert.False(RegrasGrupo.CabeMaisUmConvite(1, 4));
    }

    [Theory]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    public void Quinto_membro_entra_e_o_sexto_nao(int membros, bool esperado) =>
        Assert.Equal(esperado, RegrasGrupo.CabeMaisUmMembro(membros));

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(6, true)]
    public void Alvo_recebe_ate_5_convites_pendentes(int pendentesDoAlvo, bool demais) =>
        Assert.Equal(demais, RegrasGrupo.AlvoComConvitesDemais(pendentesDoAlvo));

    // ── Tempos ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(59.0, false)]
    [InlineData(59.9, false)]
    [InlineData(60.0, true)]
    [InlineData(300.0, true)]
    public void Convite_com_59_s_vale_e_com_60_s_expirou(double segundos, bool expirou) =>
        Assert.Equal(expirou, RegrasGrupo.ConviteExpirou(segundos));

    [Theory]
    [InlineData(0.0, 60)]
    [InlineData(0.5, 60)]
    [InlineData(1.0, 59)]
    [InlineData(59.5, 1)]
    [InlineData(60.0, 0)]
    [InlineData(500.0, 0)]
    [InlineData(-10.0, 60)]
    public void ValidadeRestante_fica_entre_zero_e_a_validade(double segundos, int esperado) =>
        Assert.Equal(esperado, RegrasGrupo.ValidadeRestanteSegundos(segundos));

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(59.0, false)]
    [InlineData(60.0, false)]
    [InlineData(60.1, true)]
    public void Vaga_guardada_vence_so_depois_de_60_s(double segundosOffline, bool venceu) =>
        Assert.Equal(venceu, RegrasGrupo.VagaGuardadaVenceu(segundosOffline));

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(29.9, true)]
    [InlineData(30.0, false)]
    public void Expulso_nao_e_reconvidado_por_30_s(double segundos, bool bloqueado) =>
        Assert.Equal(bloqueado, RegrasGrupo.ReconviteBloqueado(segundos));

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(0.99, true)]
    [InlineData(1.0, false)]
    [InlineData(5.0, false)]
    public void Um_convite_por_segundo_por_convidante(double segundos, bool rapidoDemais) =>
        Assert.Equal(rapidoDemais, RegrasGrupo.ConviteRapidoDemais(segundos));

    // ── Desfazer ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(5, false)]
    public void Grupo_desfaz_ao_ficar_com_uma_pessoa(int membros, bool desfaz) =>
        Assert.Equal(desfaz, RegrasGrupo.DeveDesfazer(membros));

    // ── Herança da liderança ──────────────────────────────────────────────────

    [Fact]
    public void Lideranca_passa_ao_mais_antigo_online() =>
        Assert.Equal(1, RegrasGrupo.PosicaoDoHerdeiro(new[] { true, true, true }, 0));

    [Fact]
    public void Lideranca_pula_quem_esta_offline() =>
        Assert.Equal(2, RegrasGrupo.PosicaoDoHerdeiro(new[] { true, false, true }, 0));

    [Fact]
    public void Sem_ninguem_online_vale_o_mais_antigo_offline() =>
        Assert.Equal(1, RegrasGrupo.PosicaoDoHerdeiro(new[] { true, false, false }, 0));

    [Fact]
    public void Lider_que_nao_e_o_primeiro_da_lista_passa_ao_primeiro_online() =>
        Assert.Equal(0, RegrasGrupo.PosicaoDoHerdeiro(new[] { true, true, true }, 2));

    [Fact]
    public void Lider_offline_nao_herda_de_si_mesmo() =>
        Assert.Equal(1, RegrasGrupo.PosicaoDoHerdeiro(new[] { false, true }, 0));

    [Fact]
    public void Lider_sozinho_nao_tem_herdeiro()
    {
        Assert.Equal(-1, RegrasGrupo.PosicaoDoHerdeiro(new[] { true }, 0));
        Assert.Equal(-1, RegrasGrupo.PosicaoDoHerdeiro(Array.Empty<bool>(), 0));
        Assert.Equal(-1, RegrasGrupo.PosicaoDoHerdeiro(null!, 0));
    }

    [Fact]
    public void Herdeiro_aceita_lista_alem_de_vetor() =>
        Assert.Equal(1, RegrasGrupo.PosicaoDoHerdeiro(new List<bool> { true, true }, 0));
}
