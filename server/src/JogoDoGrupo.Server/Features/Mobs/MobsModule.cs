using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>Os grupos de inimigos comuns do campo (um por ponto de spawn) e o teto que dividem.</summary>
public sealed record GruposDoCampo(
    LimiteDeInimigosVivos Limite,
    IReadOnlyList<GrupoDeInimigos> Grupos
);

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

        // Um grupo por ponto de spawn (dados em PontosDeSpawnDoCampo), todos sob o mesmo teto
        // de inimigos vivos.
        servicos.AddSingleton(sp =>
        {
            var limite = new LimiteDeInimigosVivos(PontosDeSpawnDoCampo.LimiteDeInimigosVivos);
            var sorteador = sp.GetRequiredService<ISorteadorDeCriaturas>();
            return new GruposDoCampo(
                limite,
                [
                    .. PontosDeSpawnDoCampo.Todos.Select(ponto =>
                        GrupoDeInimigos.DoPonto(ponto, sorteador, limite)
                    ),
                ]
            );
        });
        for (var i = 0; i < PontosDeSpawnDoCampo.Todos.Count; i++)
        {
            var indice = i;
            servicos.AddSingleton<IAlvosDeCombate>(sp =>
                sp.GetRequiredService<GruposDoCampo>().Grupos[indice]
            );
        }

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

        // Os bosses de dungeon: um por área, criados pela instância (IChefesDaDungeon). Enquanto
        // a instância não existe, sobe um de teste fora da cidade.
        servicos.AddSingleton(_ =>
        {
            var chefes = new ChefesDaDungeon();
            if (ConfigChefeDaDungeon.CriarChefeDeTesteAoSubir)
            {
                chefes.Criar(
                    ConfigChefeDaDungeon.AreaDeTeste,
                    ConfigChefeDaDungeon.PontoDeTesteX,
                    ConfigChefeDaDungeon.PontoDeTesteZ
                );
            }

            return chefes;
        });
        servicos.AddSingleton<IChefesDaDungeon>(sp => sp.GetRequiredService<ChefesDaDungeon>());
        servicos.AddSingleton<IAlvosDeCombate>(sp => sp.GetRequiredService<ChefesDaDungeon>());
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDeInimigos(
                [
                    .. sp.GetRequiredService<GruposDoCampo>().Grupos,
                    sp.GetRequiredKeyedService<GrupoDeInimigos>(ChaveDoChefeRaro),
                ],
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetRequiredService<EventosDoServidor>(),
                sp.GetRequiredService<ILogger<ServicoDeInimigos>>(),
                sp.GetRequiredService<ChefesDaDungeon>()
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
