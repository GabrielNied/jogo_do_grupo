using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Montarias;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Montarias;

public class MontariasContratosTestes
{
    [Fact]
    public void Faixa_comeca_em_16000()
    {
        Assert.Equal(16000, MontariasMessageIds.Faixa.Inicio);
        Assert.Equal(16999, MontariasMessageIds.Faixa.Fim);
    }

    [Fact]
    public void Velocidade_da_montaria_e_maior_que_corrida_padrao()
    {
        Assert.True(RegrasMontaria.VelocidadeMontariaPadrao > 5.5f);
        Assert.Equal(9.0f, RegrasMontaria.VelocidadeMontariaPadrao);
    }

    [Fact]
    public void Mensagens_estao_na_faixa()
    {
        Assert.True(MontariasMessageIds.Faixa.Contem(MontariasMessageIds.ChamarMontaria));
        Assert.True(MontariasMessageIds.Faixa.Contem(MontariasMessageIds.Desmontar));
        Assert.True(MontariasMessageIds.Faixa.Contem(MontariasMessageIds.EstadoMontaria));
        Assert.True(MontariasMessageIds.Faixa.Contem(MontariasMessageIds.EquiparMontaria));
        Assert.True(MontariasMessageIds.Faixa.Contem(MontariasMessageIds.MontariaEquipada));
        Assert.True(MontariasMessageIds.Faixa.Contem(MontariasMessageIds.UsarItemMontaria));
        Assert.True(MontariasMessageIds.Faixa.Contem(MontariasMessageIds.ColecaoMontarias));
        Assert.True(MontariasMessageIds.Faixa.Contem(MontariasMessageIds.MontariaDesbloqueada));
        Assert.True(MontariasMessageIds.Faixa.Contem(MontariasMessageIds.MontariaRecusada));
        Assert.True(MontariasMessageIds.Faixa.Contem(MontariasMessageIds.ConsultarEstabulo));
        Assert.True(MontariasMessageIds.Faixa.Contem(MontariasMessageIds.RespostaEstabulo));
        Assert.True(MontariasMessageIds.Faixa.Contem(MontariasMessageIds.ComprarMontariaEstabulo));
        Assert.True(MontariasMessageIds.Faixa.Contem(MontariasMessageIds.RespostaCompraEstabulo));
    }

    [Fact]
    public void ConfigEstabulo_TemPrecosEOfertasValidas()
    {
        Assert.True(ConfigEstabulo.EstaAVenda(RegrasMontaria.TipoMulaDeCarga));
        Assert.True(ConfigEstabulo.EstaAVenda(RegrasMontaria.TipoCavaloComum));
        Assert.False(ConfigEstabulo.EstaAVenda(RegrasMontaria.TipoNoturno));

        Assert.Equal(80, ConfigEstabulo.ObterPreco(RegrasMontaria.TipoMulaDeCarga));
        Assert.Equal(150, ConfigEstabulo.ObterPreco(RegrasMontaria.TipoCavaloComum));

        Assert.NotNull(ConfigEstabulo.ObterOferta(RegrasMontaria.TipoCavaloComum));
        Assert.NotNull(ConfigEstabulo.ObterOferta(RegrasMontaria.TipoMulaDeCarga));
    }

    [Fact]
    public void CavaloComum_EstaNoCatalogo_E_TemMetadados()
    {
        Assert.Contains(RegrasMontaria.TipoCavaloComum, RegrasMontaria.TiposDoCatalogo);
        Assert.Equal("Cavalo Comum", RegrasMontaria.ObterNome(RegrasMontaria.TipoCavaloComum));
        Assert.Equal("Comum", RegrasMontaria.ObterRaridade(RegrasMontaria.TipoCavaloComum));
        Assert.Contains("Estábulo", RegrasMontaria.ObterOrigem(RegrasMontaria.TipoCavaloComum));
        Assert.Contains("Estábulo", RegrasMontaria.ObterOrigem(RegrasMontaria.TipoMulaDeCarga));
    }

    [Fact]
    public void Item_redeas_corcel_noturno_desbloqueia_tipo_noturno()
    {
        Assert.Equal(
            RegrasMontaria.TipoNoturno,
            RegrasMontaria.TipoMontariaDoItem(CatalogoItens.RedeasDoCorcelNoturno)
        );
        Assert.Equal(0, RegrasMontaria.TipoMontariaDoItem(CatalogoItens.EspadaDeTeste));
    }

    [Fact]
    public void Castanho_e_desbloqueado_por_padrao_e_noturno_e_bloqueado()
    {
        Assert.True(RegrasMontaria.DesbloqueadaPorPadrao(RegrasMontaria.TipoCastanho));
        Assert.False(RegrasMontaria.DesbloqueadaPorPadrao(RegrasMontaria.TipoNoturno));
        Assert.False(string.IsNullOrEmpty(RegrasMontaria.ObterOrigem(RegrasMontaria.TipoNoturno)));
    }
}
