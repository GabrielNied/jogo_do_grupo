using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Mundo;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

public class LagCompensacaoVolumeTestes
{
    private const string Area = ConfigInimigosDoCampo.Area;
    private const long InimigoId = 42;

    private static readonly DefinicaoDeCriatura Javali = CatalogoCriaturas.Obter(
        CatalogoCriaturas.JavaliDoCampo
    )!;

    [Fact]
    public void Alvo_no_historico_dentro_do_rewind_entra_no_cone()
    {
        var historico = new HistoricoDePosicoesDoMundo();
        var relogio = 5_000L;
        var grupo = new GrupoDeInimigos(
            Javali,
            new SorteadorDeCriaturas(),
            1,
            () => relogio,
            pontoX: 0,
            pontoZ: 0,
            primeiroId: InimigoId,
            historico: historico
        );

        historico.Registrar(InimigoId, new PosicaoNoMundo(Area, 0f, 0f, 3f, 0f), relogio);

        var alvos = grupo.AlvosNoVolume(Area, 0, 0, 0, relogio);
        Assert.Single(alvos);
        Assert.Equal(InimigoId, alvos[0].Id);
    }

    [Fact]
    public void Esquiva_com_tempo_visto_abre_invulnerabilidade_no_passado()
    {
        var controle = new ControleDeAcoes();
        var agora = 10_000L;
        var visto = agora - 200;
        Assert.True(controle.TentarOcupar(1, agora, RegrasEsquiva.DuracaoMs, out _, visto));
        Assert.True(controle.EstaInvulneravel(1, visto + 100));
    }

    [Fact]
    public void Referencia_muito_antiga_cai_no_instante_atual()
    {
        var relogio = 10_000L;
        var instante = RegrasCompensacaoDeLag.InstanteParaTeste(relogio - 400, relogio);
        Assert.Equal(relogio, instante);
    }

    [Fact]
    public void Rewind_do_acerto_ancora_no_inicio_da_acao_nao_no_momento_do_golpe()
    {
        const long inicioMs = 10_000L;
        const long tempoVistoMs = 9_800L;
        const long acertoMs = inicioMs + 500;

        Assert.Equal(acertoMs, RegrasCompensacaoDeLag.InstanteParaTeste(tempoVistoMs, acertoMs));
        Assert.Equal(
            tempoVistoMs,
            RegrasCompensacaoDeLag.InstanteParaTeste(tempoVistoMs, inicioMs)
        );
    }
}
