using System;
using JogoDoGrupo.Shared.Features.Instancias;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Shared.Tests.Features.Instancias;

public sealed class LayoutDaMinaDoReflexoTestes : IDisposable
{
    public LayoutDaMinaDoReflexoTestes()
    {
        DadosDoMapaFeitoAMao.DesativadoEmTestes = true;
    }

    public void Dispose()
    {
        DadosDoMapaFeitoAMao.DesativadoEmTestes = false;
    }

    [Fact]
    public void Entrada_fica_na_serra_nevada_perto_da_cidade_do_passo()
    {
        var x = LayoutDaMinaDoReflexo.EntradaX;
        var z = LayoutDaMinaDoReflexo.EntradaZ;
        Assert.Equal(Bioma.Neve, MapaDoMundo.BiomaEm(x, z));
        Assert.True(EstruturasDoMapa.ChaoSeco(x, z));
        Assert.True(EstruturasDoMapa.EncostaAndavel(x, z));
        var distancia = LayoutDaMinaDoReflexo.Distancia(
            MapaDoMundo.CidadeDaNeveX,
            MapaDoMundo.CidadeDaNeveZ,
            x,
            z
        );
        Assert.InRange(distancia, 60f, 160f);
    }

    [Fact]
    public void Interacao_so_no_raio_da_entrada()
    {
        Assert.True(
            LayoutDaMinaDoReflexo.PertoDaEntrada(
                LayoutDaMinaDoReflexo.EntradaX,
                LayoutDaMinaDoReflexo.EntradaZ
            )
        );
        Assert.False(LayoutDaMinaDoReflexo.PertoDaEntrada(0f, 0f));
    }

    [Fact]
    public void Tres_saloes_comuns_e_o_chefe_por_ultimo()
    {
        Assert.Equal(3, LayoutDaMinaDoReflexo.SaloesComuns().Length);
        var ultimo = LayoutDaMinaDoReflexo.Trechos[^1];
        Assert.Equal(LayoutDaMinaDoReflexo.TipoDeTrecho.SalaoDoChefe, ultimo.Tipo);
        Assert.Equal(LayoutDaMinaDoReflexo.BossCentroX, LayoutDaMinaDoReflexo.CentroX(ultimo), 1);
    }

    [Fact]
    public void Trechos_se_encontram_sem_vazio_e_cabem_no_mundo()
    {
        var trechos = LayoutDaMinaDoReflexo.Trechos;
        for (var i = 1; i < trechos.Length; i++)
        {
            Assert.Equal(trechos[i - 1].Ate, trechos[i].De);
        }

        Assert.True(LayoutDaMinaDoReflexo.OrigemX + trechos[^1].Ate < MapaDoMundo.Metade);
        Assert.True(LayoutDaMinaDoReflexo.OrigemX > -MapaDoMundo.Metade);
    }

    [Fact]
    public void Chao_da_dungeon_e_quase_plano_e_acima_da_agua()
    {
        var menor = float.MaxValue;
        var maior = float.MinValue;
        foreach (var trecho in LayoutDaMinaDoReflexo.Trechos)
        {
            for (var x = trecho.De; x <= trecho.Ate; x += 4f)
            {
                for (var z = -trecho.MetadeDaLargura; z <= trecho.MetadeDaLargura; z += 4f)
                {
                    var h = LayoutDaMinaDoReflexo.AlturaDoChao(
                        LayoutDaMinaDoReflexo.OrigemX + x,
                        LayoutDaMinaDoReflexo.OrigemZ + z
                    );
                    menor = System.Math.Min(menor, h);
                    maior = System.Math.Max(maior, h);
                }
            }
        }

        Assert.True(menor > MapaDoMundo.NivelDaAgua + 1f);
        Assert.True(maior - menor < 4f, "desnível " + (maior - menor));
    }
}
