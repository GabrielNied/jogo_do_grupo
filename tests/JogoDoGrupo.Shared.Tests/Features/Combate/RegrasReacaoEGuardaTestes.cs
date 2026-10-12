using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class RegrasReacaoEGuardaTestes
{
    [Theory]
    [InlineData(CombateMessageIds.IntencaoDeGuarda)]
    [InlineData(CombateMessageIds.GuardaMudou)]
    [InlineData(CombateMessageIds.GolpeBloqueado)]
    [InlineData(CombateMessageIds.AcaoInterrompida)]
    public void Id_esta_na_faixa_do_combate(ushort id) =>
        Assert.True(CombateMessageIds.Faixa.Contem(id));

    [Fact]
    public void Ids_da_guarda_e_da_reacao_nao_colidem_com_os_outros()
    {
        var novos = new[]
        {
            CombateMessageIds.IntencaoDeGuarda,
            CombateMessageIds.GuardaMudou,
            CombateMessageIds.GolpeBloqueado,
            CombateMessageIds.AcaoInterrompida,
        };
        var outros = new[]
        {
            CombateMessageIds.IntencaoDeAtaque,
            CombateMessageIds.AtaqueIniciado,
            CombateMessageIds.AtaqueAcertou,
            CombateMessageIds.AtaqueRecusado,
            CombateMessageIds.RecursosMudaram,
            CombateMessageIds.IntencaoDeEsquiva,
            CombateMessageIds.EsquivaRecusada,
            CombateMessageIds.EsquivaIniciada,
            CombateMessageIds.ComecarALevantar,
            CombateMessageIds.CancelarLevantar,
            CombateMessageIds.VoltarParaACidade,
            CombateMessageIds.EstadoDeQueda,
            CombateMessageIds.AcaoRecusada,
        };

        Assert.Equal(novos.Length, novos.Distinct().Count());
        Assert.Empty(novos.Intersect(outros));
    }

    [Fact]
    public void Reacao_dura_300_ms_e_a_folga_600_ms_depois()
    {
        Assert.Equal(300, RegrasReacao.AtordoamentoNoAcertoMs);
        Assert.Equal(600, RegrasReacao.FolgaDepoisDoAcertoMs);
        Assert.Equal(1_300, RegrasReacao.FimDoAtordoamentoMs(1_000));
        Assert.Equal(1_900, RegrasReacao.FimDaFolgaMs(1_000));
        Assert.False(RegrasReacao.PodeReagir(1_899, 1_900));
        Assert.True(RegrasReacao.PodeReagir(1_900, 1_900));
    }

    [Fact]
    public void Guarda_anda_a_40_por_cento_e_custa_1_de_vigor_por_ponto_de_dano()
    {
        Assert.Equal(0.4, RegrasGuarda.VelocidadeComGuarda);
        Assert.Equal(42, RegrasGuarda.CustoDoBloqueio(42));
        Assert.Equal(0, RegrasGuarda.CustoDoBloqueio(-5));
        Assert.True(RegrasGuarda.Segura(42, 42));
        Assert.False(RegrasGuarda.Segura(41, 42));
    }
}
