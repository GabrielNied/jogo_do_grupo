using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mobs;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    private IServiceScopeFactory? _scopeFactory;
    private ServidorTempoReal? _tempoReal;

    /// <summary>Chave do grupo do chefe raro no contêiner.</summary>
    public const string ChaveDoChefeRaro = "chefe-raro";

    /// <summary>Chaves dos grupos dos chefes de mundo (um grupo por chefe) no contêiner.</summary>
    public const string ChaveDoAnciao = "chefe-de-mundo-anciao";

    public const string ChaveDoColosso = "chefe-de-mundo-colosso";

    /// <summary>Chave do chefe fixo do capítulo 1 perto da cidade.</summary>
    public const string ChaveDoChefeDoCapitulo = "chefe-do-capitulo";

    public string Nome => "Mobs e bosses";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddScoped<ServicoDeBestiario>();
        servicos.AddSingleton<ISorteadorDeCriaturas>(new SorteadorDeCriaturas());

        // Um grupo por ponto de spawn (dados em PontosDeSpawnDoCampo), todos sob o mesmo teto
        // de inimigos vivos.
        servicos.AddSingleton(sp =>
        {
            var limite = new LimiteDeInimigosVivos(PontosDeSpawnDoCampo.LimiteDeInimigosVivos);
            var sorteador = sp.GetRequiredService<ISorteadorDeCriaturas>();
            var queda = sp.GetRequiredService<IEstadoDeQueda>();
            var historico = sp.GetRequiredService<IHistoricoDePosicoesDoMundo>();
            var relogioDoMundo = sp.GetService<IRelogioDoMundo>();
            GrupoDeInimigos[] grupos =
            [
                .. PontosDeSpawnDoCampo.Todos.Select(ponto =>
                    GrupoDeInimigos.DoPonto(
                        ponto,
                        sorteador,
                        limite,
                        queda: queda,
                        historico: historico,
                        relogioDoMundo: relogioDoMundo,
                        projeteis: () => sp.GetService<IDisparoDeProjeteis>()
                    )
                ),
            ];
            PuxadaEmCadeia.Ligar(grupos);
            return new GruposDoCampo(limite, grupos);
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
                    primeiroId: ConfigInimigosDoCampo.PrimeiroIdDoChefeRaro,
                    queda: sp.GetRequiredService<IEstadoDeQueda>(),
                    historico: sp.GetRequiredService<IHistoricoDePosicoesDoMundo>()
                )
        );
        servicos.AddSingleton<IAlvosDeCombate>(sp =>
            sp.GetRequiredKeyedService<GrupoDeInimigos>(ChaveDoChefeRaro)
        );

        // Os chefes de mundo: um grupo de um por chefe, lugar e hora sorteados pelo grupo.
        AdicionarChefeDeMundo(servicos, ChaveDoAnciao, CatalogoCriaturas.AnciaoTrovejante, 0);
        AdicionarChefeDeMundo(servicos, ChaveDoColosso, CatalogoCriaturas.ColossoBasaltico, 1);

        // O chefe fixo do capítulo 1 perto da cidade, com respawn curto.
        servicos.AddKeyedSingleton(
            ChaveDoChefeDoCapitulo,
            (sp, _) =>
            {
                var proximoId = ConfigChefeDoCapitulo.PrimeiroId;
                var def = CatalogoCriaturas.Obter(ConfigChefeDoCapitulo.CriaturaId);
                var multDano = (double)def.DanoBase / 120.0;
                return new ChefeDaDungeon(
                    ConfigChefeDoCapitulo.Area,
                    ConfigChefeDoCapitulo.PontoX,
                    ConfigChefeDoCapitulo.PontoZ,
                    () => Interlocked.Increment(ref proximoId),
                    historico: sp.GetService<IHistoricoDePosicoesDoMundo>(),
                    multiplicadorVida: 1.0,
                    multiplicadorDano: multDano,
                    criaturaId: ConfigChefeDoCapitulo.CriaturaId,
                    intervaloRespawnSegundos: ConfigChefeDoCapitulo.IntervaloRespawnSegundos
                );
            }
        );
        servicos.AddSingleton<IAlvosDeCombate>(sp =>
            sp.GetRequiredKeyedService<ChefeDaDungeon>(ChaveDoChefeDoCapitulo)
        );

        // Os bosses de dungeon: um por área, criados pela instância (IChefesDaDungeon). Enquanto
        // a instância não existe, sobe um de teste fora da cidade.
        servicos.AddSingleton(sp => new ChefesDaDungeon(
            historico: sp.GetService<IHistoricoDePosicoesDoMundo>()
        ));
        servicos.AddSingleton<IChefesDaDungeon>(sp => sp.GetRequiredService<ChefesDaDungeon>());
        servicos.AddSingleton<IAlvosDeCombate>(sp => sp.GetRequiredService<ChefesDaDungeon>());
        servicos.AddSingleton<InimigosPorInstancia>();
        servicos.AddSingleton<IInimigosPorInstancia>(sp =>
            sp.GetRequiredService<InimigosPorInstancia>()
        );
        servicos.AddSingleton<IAlvosDeCombate>(sp => sp.GetRequiredService<InimigosPorInstancia>());
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDeInimigos(
                [
                    .. sp.GetRequiredService<GruposDoCampo>().Grupos,
                    sp.GetRequiredKeyedService<GrupoDeInimigos>(ChaveDoChefeRaro),
                    sp.GetRequiredKeyedService<GrupoDeInimigos>(ChaveDoAnciao),
                    sp.GetRequiredKeyedService<GrupoDeInimigos>(ChaveDoColosso),
                    sp.GetRequiredKeyedService<ChefeDaDungeon>(ChaveDoChefeDoCapitulo),
                ],
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetRequiredService<EventosDoServidor>(),
                sp.GetRequiredService<ILogger<ServicoDeInimigos>>(),
                sp.GetRequiredService<IEstadoDeQueda>(),
                sp.GetRequiredService<ChefesDaDungeon>(),
                sp.GetRequiredService<InimigosPorInstancia>(),
                vizinhanca: sp.GetService<IVizinhanca>()
            );
            _inimigos = servico;
            return servico;
        });
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDeInimigos>());
        servicos.AddSingleton<ISpawnDeTeste>(sp => sp.GetRequiredService<ServicoDeInimigos>());
        servicos.AddSingleton<IInimigosDeTeste>(sp => sp.GetRequiredService<ServicoDeInimigos>());
    }

    private static void AdicionarChefeDeMundo(
        IServiceCollection servicos,
        string chave,
        int criaturaId,
        int indice
    )
    {
        servicos.AddKeyedSingleton(
            chave,
            (sp, _) =>
                new GrupoDeInimigos(
                    CatalogoCriaturas.Obter(criaturaId)!,
                    sp.GetRequiredService<ISorteadorDeCriaturas>(),
                    1,
                    primeiroId: ConfigChefeDeMundo.PrimeiroId
                        + (indice * ConfigChefeDeMundo.EspacoDeIdsPorChefe),
                    queda: sp.GetRequiredService<IEstadoDeQueda>(),
                    historico: sp.GetRequiredService<IHistoricoDePosicoesDoMundo>(),
                    lugares: ConfigChefeDeMundo.Lugares
                )
        );
        servicos.AddSingleton<IAlvosDeCombate>(sp =>
            sp.GetRequiredKeyedService<GrupoDeInimigos>(chave)
        );
    }

    public void MapearRotas(IEndpointRouteBuilder rotas)
    {
        _scopeFactory = rotas.ServiceProvider.GetService<IServiceScopeFactory>();
    }

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(MobsMessageIds.Faixa)
            .Ao<ConsultarBestiario>(MobsMessageIds.ConsultarBestiario, AoConsultarBestiario);

    public void Inicializar(ServerContext contexto)
    {
        _tempoReal = contexto.TempoReal;
        contexto.Eventos.Assinar<JogadorEntrouNaArea>(evento => _inimigos?.AoEntrarNaArea(evento));
        contexto.Eventos.Assinar<PersonagemCurouAliado>(evento => _inimigos?.AoCurar(evento));
        contexto.Eventos.Assinar<JogadorSaiuDaArea>(evento => _inimigos?.AoSairDaArea(evento));
        contexto.Eventos.Assinar<InimigoDerrotado>(AoInimigoDerrotado);
        contexto.AoPasso(
            TimeSpan.FromSeconds(RegrasInimigo.PassoSegundos),
            _ => _inimigos?.Avancar(),
            "Mobs"
        );
    }

    private void AoConsultarBestiario(SessaoTempoReal sessao, ConsultarBestiario pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeBestiario>();
                var estado = await servico.ObterBestiarioAsync(sessao.PersonagemId);
                _tempoReal.Enviar(sessao, MobsMessageIds.EstadoDoBestiario, estado);
            }
            catch { }
        });
    }

    private void AoInimigoDerrotado(InimigoDerrotado evento)
    {
        if (_scopeFactory == null || _tempoReal == null || evento.Participantes.Count == 0)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeBestiario>();
                foreach (var participanteId in evento.Participantes)
                {
                    var atualizado = await servico.RegistrarDerrotaAsync(
                        participanteId,
                        evento.CriaturaId
                    );
                    if (
                        atualizado != null
                        && _tempoReal.TentarObterSessao(participanteId, out var sessao)
                        && sessao != null
                    )
                    {
                        _tempoReal.Enviar(
                            sessao,
                            MobsMessageIds.ProgressoDoBestiarioAtualizado,
                            new ProgressoDoBestiarioAtualizado(atualizado)
                        );
                    }
                }
            }
            catch { }
        });
    }
}
