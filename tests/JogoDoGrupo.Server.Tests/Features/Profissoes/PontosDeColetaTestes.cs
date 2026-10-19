using JogoDoGrupo.Server.Features.Profissoes;
using JogoDoGrupo.Shared.Features.Profissoes;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Profissoes;

public sealed class PontosDeColetaTestes
{
    private sealed class RelogioFalso : TimeProvider
    {
        private DateTimeOffset _agora = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _agora;

        public void Avancar(TimeSpan tempo) => _agora += tempo;
    }

    [Fact]
    public void O_primeiro_leva_o_ponto_e_o_segundo_nao()
    {
        var pontos = new PontosDeColetaDoMundo(new RelogioFalso());

        Assert.True(pontos.TentarColetar("cidade", ConfigProfissoes.NoArvore1));
        Assert.False(pontos.TentarColetar("cidade", ConfigProfissoes.NoArvore1));
    }

    [Fact]
    public void O_ponto_volta_para_todos_depois_do_respawn()
    {
        var relogio = new RelogioFalso();
        var pontos = new PontosDeColetaDoMundo(relogio);
        pontos.TentarColetar("cidade", ConfigProfissoes.NoArvore1);

        relogio.Avancar(TimeSpan.FromSeconds(ConfigProfissoes.RespawnSegundos - 1));
        Assert.False(pontos.TentarColetar("cidade", ConfigProfissoes.NoArvore1));
        relogio.Avancar(TimeSpan.FromSeconds(1));
        Assert.True(pontos.TentarColetar("cidade", ConfigProfissoes.NoArvore1));
    }

    [Fact]
    public void Cada_canal_tem_os_seus_pontos()
    {
        var pontos = new PontosDeColetaDoMundo(new RelogioFalso());

        Assert.True(pontos.TentarColetar("cidade", ConfigProfissoes.NoArvore1));
        Assert.True(pontos.TentarColetar("cidade:2", ConfigProfissoes.NoArvore1));
    }

    [Fact]
    public void O_estado_lista_o_esgotado_com_os_segundos_que_faltam()
    {
        var relogio = new RelogioFalso();
        var pontos = new PontosDeColetaDoMundo(relogio);
        pontos.TentarColetar("cidade", ConfigProfissoes.NoVeioDeFerro1);
        relogio.Avancar(TimeSpan.FromSeconds(10));

        var estado = pontos.EstadoDaArea("cidade");

        Assert.Equal(new[] { ConfigProfissoes.NoVeioDeFerro1 }, estado.Esgotados);
        Assert.Equal(new[] { ConfigProfissoes.RespawnSegundos - 10 }, estado.SegundosParaVoltar);
        Assert.Empty(pontos.EstadoDaArea("cidade:2").Esgotados);
    }

    [Fact]
    public void Devolver_desfaz_a_coleta()
    {
        var pontos = new PontosDeColetaDoMundo(new RelogioFalso());
        pontos.TentarColetar("cidade", ConfigProfissoes.NoCarcaca1);

        pontos.Devolver("cidade", ConfigProfissoes.NoCarcaca1);

        Assert.True(pontos.TentarColetar("cidade", ConfigProfissoes.NoCarcaca1));
    }

    [Fact]
    public void Os_pontos_e_receitas_do_conteudo_sao_consistentes()
    {
        var ids = ConfigProfissoes.Pontos.Select(p => p.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        foreach (var ponto in ConfigProfissoes.Pontos)
        {
            Assert.NotNull(ConfigProfissoes.NomeDoMaterial(ponto.MaterialId));
            Assert.True(ConfigProfissoes.PodeAprender(ponto.Profissao));
            Assert.True(ConfigProfissoes.PertoDoPonto(ponto, ponto.X, ponto.Z));
            Assert.False(ConfigProfissoes.PertoDoPonto(ponto, ponto.X + 10f, ponto.Z));
        }

        foreach (var receita in ConfigProfissoes.Receitas)
        {
            Assert.True(ConfigProfissoes.PodeAprender(receita.Profissao));
            Assert.Equal(receita.MateriaisIds.Length, receita.MateriaisQuantidades.Length);
            Assert.NotNull(
                JogoDoGrupo.Shared.Features.Itens.CatalogoEquipamentos.Obter(receita.ModeloId)
            );
            Assert.All(
                receita.MateriaisIds,
                m => Assert.NotNull(ConfigProfissoes.NomeDoMaterial(m))
            );
        }
    }
}
