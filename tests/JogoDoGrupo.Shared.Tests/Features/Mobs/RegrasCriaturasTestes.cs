using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Shared.Tests.Features.Mobs;

public class RegrasCriaturasTestes
{
    [Fact]
    public void Numeros_sao_os_da_proposta()
    {
        Assert.Equal(0.05, RegrasCriaturas.ChanceDeElite);
        Assert.Equal(2.0, RegrasCriaturas.MultiplicadorDeVidaElite);
        Assert.Equal(1.5, RegrasCriaturas.MultiplicadorDeDanoElite);
        Assert.Equal(2.0, RegrasCriaturas.MultiplicadorDeRecompensaElite);
        Assert.Equal(1.3f, RegrasCriaturas.EscalaElite);
    }

    [Fact]
    public void Enums_tem_os_valores_combinados()
    {
        Assert.Equal(0, (int)CategoriaDeCriatura.Comum);
        Assert.Equal(1, (int)CategoriaDeCriatura.Chefe);
        Assert.Equal(2, (int)CategoriaDeCriatura.ChefeRaro);
        Assert.Equal(0, (int)VarianteDeCriatura.Normal);
        Assert.Equal(1, (int)VarianteDeCriatura.Elite);
    }

    [Theory]
    [InlineData(CategoriaDeCriatura.Comum, true)]
    [InlineData(CategoriaDeCriatura.Chefe, false)]
    [InlineData(CategoriaDeCriatura.ChefeRaro, false)]
    public void So_a_comum_pode_ser_elite(CategoriaDeCriatura categoria, bool esperado) =>
        Assert.Equal(esperado, RegrasCriaturas.PodeSerElite(categoria));

    [Theory]
    [InlineData(0.0, VarianteDeCriatura.Elite)]
    [InlineData(0.0499, VarianteDeCriatura.Elite)]
    [InlineData(0.05, VarianteDeCriatura.Normal)]
    [InlineData(0.999, VarianteDeCriatura.Normal)]
    public void Comum_vem_elite_abaixo_da_chance(double sorteio, VarianteDeCriatura esperado)
    {
        var variante = RegrasCriaturas.SortearVariante(CategoriaDeCriatura.Comum, sorteio);

        Assert.Equal(esperado, variante);
    }

    [Theory]
    [InlineData(CategoriaDeCriatura.Chefe)]
    [InlineData(CategoriaDeCriatura.ChefeRaro)]
    public void Chefes_nunca_vem_elites(CategoriaDeCriatura categoria)
    {
        // Zero é o menor sorteio possível: se nem ele fortalece, nenhum fortalece.
        var variante = RegrasCriaturas.SortearVariante(categoria, 0.0);

        Assert.Equal(VarianteDeCriatura.Normal, variante);
    }

    [Theory]
    [InlineData(0.2, 0.0, true)]
    [InlineData(0.2, 0.1999, true)]
    [InlineData(0.2, 0.2, false)]
    [InlineData(1.0, 0.999999, true)]
    [InlineData(0.0, 0.0, false)]
    [InlineData(-1.0, 0.0, false)]
    public void Aparece_so_abaixo_da_chance(double chance, double sorteio, bool esperado) =>
        Assert.Equal(esperado, RegrasCriaturas.Aparece(chance, sorteio));

    [Fact]
    public void Normal_fica_com_os_valores_de_base()
    {
        Assert.Equal(180, RegrasCriaturas.VidaMaxima(180, VarianteDeCriatura.Normal));
        Assert.Equal(40, RegrasCriaturas.Dano(40, VarianteDeCriatura.Normal));
        Assert.Equal(20, RegrasCriaturas.Experiencia(20, VarianteDeCriatura.Normal));
        Assert.Equal(1.0, RegrasCriaturas.MultiplicadorDeRecompensa(VarianteDeCriatura.Normal));
        Assert.Equal(1f, RegrasCriaturas.Escala(VarianteDeCriatura.Normal));
    }

    [Fact]
    public void Elite_tem_mais_vida_dano_tamanho_e_recompensa()
    {
        var elite = VarianteDeCriatura.Elite;

        Assert.Equal(360, RegrasCriaturas.VidaMaxima(180, elite));
        Assert.Equal(60, RegrasCriaturas.Dano(40, elite));
        Assert.Equal(40, RegrasCriaturas.Experiencia(20, elite));
        Assert.Equal(2.0, RegrasCriaturas.MultiplicadorDeRecompensa(elite));
        Assert.Equal(1.3f, RegrasCriaturas.Escala(elite));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(3, 5)]
    [InlineData(0, 0)]
    public void Dano_elite_arredonda_meio_para_cima(int danoBase, int esperado) =>
        Assert.Equal(esperado, RegrasCriaturas.Dano(danoBase, VarianteDeCriatura.Elite));

    [Fact]
    public void Valor_enorme_nao_estoura_o_inteiro()
    {
        var vida = RegrasCriaturas.VidaMaxima(int.MaxValue, VarianteDeCriatura.Elite);

        Assert.Equal(int.MaxValue, vida);
    }
}
