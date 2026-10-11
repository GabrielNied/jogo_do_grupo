using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

public sealed class MobsModule : IFeatureModule
{
    // Criado pelo contêiner quando o app sobe (hosted service); o evento de entrada na área só
    // chega depois que uma sessão se autentica.
    private volatile ServicoDeInimigos? _inimigos;

    public string Nome => "Mobs e bosses";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<ISorteadorDeCriaturas>(new SorteadorDeCriaturas());
        servicos.AddSingleton(sp => new GrupoDeInimigos(
            CatalogoCriaturas.Obter(RegrasInimigo.CriaturaDaFatia),
            sp.GetRequiredService<ISorteadorDeCriaturas>(),
            RegrasInimigo.TamanhoDoGrupo(Random.Shared.NextDouble())
        ));
        servicos.AddSingleton<IAlvosDeCombate>(sp => sp.GetRequiredService<GrupoDeInimigos>());
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDeInimigos(
                sp.GetRequiredService<GrupoDeInimigos>(),
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetRequiredService<EventosDoServidor>(),
                sp.GetRequiredService<ILogger<ServicoDeInimigos>>()
            );
            _inimigos = servico;
            return servico;
        });
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDeInimigos>());
    }

    public void Inicializar(ServerContext contexto) =>
        contexto.Eventos.Assinar<JogadorEntrouNaArea>(evento => _inimigos?.AoEntrarNaArea(evento));
}
