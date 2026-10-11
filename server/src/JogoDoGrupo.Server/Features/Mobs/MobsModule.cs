using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
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

    /// <summary>Chave do grupo do chefe raro no contêiner.</summary>
    public const string ChaveDoChefeRaro = "chefe-raro";

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

        // O chefe raro é um grupo de um, com chave para não se confundir com o grupo comum.
        servicos.AddKeyedSingleton(
            ChaveDoChefeRaro,
            (sp, _) =>
                new GrupoDeInimigos(
                    CatalogoCriaturas.Obter(CatalogoCriaturas.PresaVelha),
                    sp.GetRequiredService<ISorteadorDeCriaturas>(),
                    1,
                    pontoX: ConfigInimigosDoCampo.PontoDoChefeRaroX,
                    pontoZ: ConfigInimigosDoCampo.PontoDoChefeRaroZ,
                    primeiroId: ConfigInimigosDoCampo.PrimeiroIdDoChefeRaro
                )
        );
        servicos.AddSingleton<IAlvosDeCombate>(sp =>
            sp.GetRequiredKeyedService<GrupoDeInimigos>(ChaveDoChefeRaro)
        );
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDeInimigos(
                [
                    sp.GetRequiredService<GrupoDeInimigos>(),
                    sp.GetRequiredKeyedService<GrupoDeInimigos>(ChaveDoChefeRaro),
                ],
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetRequiredService<EventosDoServidor>(),
                sp.GetRequiredService<ILogger<ServicoDeInimigos>>()
            );
            _inimigos = servico;
            return servico;
        });
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDeInimigos>());
        servicos.AddSingleton<ISpawnDeTeste>(sp => sp.GetRequiredService<ServicoDeInimigos>());
    }

    public void Inicializar(ServerContext contexto) =>
        contexto.Eventos.Assinar<JogadorEntrouNaArea>(evento => _inimigos?.AoEntrarNaArea(evento));
}
