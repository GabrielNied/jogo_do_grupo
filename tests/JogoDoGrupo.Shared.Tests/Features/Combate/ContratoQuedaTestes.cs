using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class ContratoQuedaTestes
{
    private static readonly ushort[] _ids =
    {
        CombateMessageIds.ComecarALevantar,
        CombateMessageIds.CancelarLevantar,
        CombateMessageIds.VoltarParaACidade,
        CombateMessageIds.EstadoDeQueda,
        CombateMessageIds.AcaoRecusada,
    };

    [Theory]
    [InlineData(CombateMessageIds.ComecarALevantar)]
    [InlineData(CombateMessageIds.CancelarLevantar)]
    [InlineData(CombateMessageIds.VoltarParaACidade)]
    [InlineData(CombateMessageIds.EstadoDeQueda)]
    [InlineData(CombateMessageIds.AcaoRecusada)]
    public void Id_esta_dentro_da_faixa_do_combate(ushort id) =>
        Assert.True(CombateMessageIds.Faixa.Contem(id));

    [Fact]
    public void Ids_da_queda_nao_colidem_com_os_outros_ids_de_combate()
    {
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
        };

        Assert.Equal(_ids.Length, _ids.Distinct().Count());
        Assert.Empty(_ids.Intersect(outros));
    }

    [Fact]
    public void Fases_tem_valores_estaveis()
    {
        Assert.Equal(0, (int)FaseDeQueda.Vivo);
        Assert.Equal(1, (int)FaseDeQueda.Caido);
        Assert.Equal(2, (int)FaseDeQueda.Levantando);
        Assert.Equal(3, (int)FaseDeQueda.Retorno);
    }

    [Fact]
    public void Codigos_de_recusa_tem_o_texto_do_contrato()
    {
        Assert.Equal("VoceEstaCaido", CodigosErroCombate.VoceEstaCaido);
        Assert.Equal("AlvoNaoEstaCaido", CodigosErroCombate.AlvoNaoEstaCaido);
        Assert.Equal("LongeDemais", CodigosErroCombate.LongeDemais);
        Assert.Equal("JaEstaSendoLevantado", CodigosErroCombate.JaEstaSendoLevantado);
        Assert.Equal("NaoEstaCaido", CodigosErroCombate.NaoEstaCaido);
        Assert.Equal("AindaNaoPodeVoltar", CodigosErroCombate.AindaNaoPodeVoltar);
    }

    [Fact]
    public void Mensagens_comecam_com_campos_padrao()
    {
        Assert.Equal(0L, new ComecarALevantar().AlvoId);
        Assert.Equal("", new AcaoRecusada().Codigo);

        var estado = new EstadoDeQueda();
        Assert.Equal(0L, estado.PersonagemId);
        Assert.Equal(FaseDeQueda.Vivo, estado.Estado);
        Assert.Equal(0, estado.SegundosRestantes);
        Assert.Equal(0L, estado.AjudanteId);
        Assert.Equal(0, estado.ProgressoMs);
    }
}
