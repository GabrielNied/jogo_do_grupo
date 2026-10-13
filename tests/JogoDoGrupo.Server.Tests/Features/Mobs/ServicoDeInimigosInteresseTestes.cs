using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

public class ServicoDeInimigosInteresseTestes(TempoRealFixture f) : IClassFixture<TempoRealFixture>
{
    [Fact]
    public void Falha_de_um_grupo_nao_impede_o_passo_do_outro()
    {
        var bom = new GrupoSimuladoFalso(deveFalhar: false);
        var ruim = new GrupoSimuladoFalso(deveFalhar: true);
        var servico = new ServicoDeInimigos(
            [ruim, bom],
            f.TempoReal,
            f.Fabrica.Services.GetRequiredService<Contratos.Mundo.IPosicoesDoMundo>(),
            f.Fabrica.Services.GetRequiredService<EventosDoServidor>(),
            NullLogger<ServicoDeInimigos>.Instance,
            f.Fabrica.Services.GetRequiredService<IEstadoDeQueda>()
        );

        servico.Avancar();

        Assert.True(bom.Passou);
    }

    private sealed class GrupoSimuladoFalso(bool deveFalhar) : IInimigosSimulados
    {
        public bool Passou { get; private set; }

        public string Area => ConfigInimigosDoCampo.Area;

        public PassoDosInimigos Passo(IReadOnlyList<JogadorNoCampo> jogadores)
        {
            Passou = true;
            if (deveFalhar)
            {
                throw new InvalidOperationException("falha de teste");
            }

            return new PassoDosInimigos();
        }

        public InimigoNoMundo[] Vivos() => [];
    }
}
