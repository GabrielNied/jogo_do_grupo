using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Instancias;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Features.Instancias;

public sealed class InstanciasModule : IFeatureModule
{
    private ServicoDeInstancias? _servico;

    public string Nome => "Dungeons e raids";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<PopuladorDeInstancia>();
        servicos.AddSingleton(sp => new ServicoDeInstancias(
            sp.GetRequiredService<JogoDoGrupo.Server.Contratos.Social.IConsultaDeGrupo>(),
            sp.GetRequiredService<JogoDoGrupo.Server.Contratos.Mundo.IPosicoesDoMundo>(),
            sp.GetRequiredService<JogoDoGrupo.Server.Contratos.Mundo.ITransicoesDeAreaDoMundo>(),
            sp.GetRequiredService<ServidorTempoReal>(),
            sp.GetService<TimeProvider>() ?? TimeProvider.System,
            sp.GetRequiredService<PopuladorDeInstancia>(),
            sp.GetRequiredService<JogoDoGrupo.Server.Contratos.Combate.IEstadoDeQueda>(),
            prazoColetaLoot: null,
            scopeFactory: sp.GetService<IServiceScopeFactory>(),
            logger: sp.GetService<ILogger<ServicoDeInstancias>>()
        ));
    }

    public void MapearRotas(IEndpointRouteBuilder rotas) =>
        _servico = rotas.ServiceProvider.GetRequiredService<ServicoDeInstancias>();

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(InstanciasMessageIds.Faixa)
            .Ao<EntrarNaDungeon>(
                InstanciasMessageIds.EntrarNaDungeon,
                (sessao, msg) => _servico?.AoEntrar(sessao, msg)
            )
            .Ao<SairDaDungeon>(
                InstanciasMessageIds.SairDaDungeon,
                (sessao, _) => _servico?.AoPedirSair(sessao)
            );

    public void Inicializar(ServerContext contexto)
    {
        contexto.TempoReal.SessaoEncerrada += sessao => _servico?.AoDesconectar(sessao);
        contexto.AoPasso(
            TimeSpan.FromSeconds(1),
            _ =>
            {
                _servico?.Varrer();
                _servico?.VarrerDerrota();
            },
            "Instancias"
        );
        contexto.Eventos.Assinar<InimigoDerrotado>(derrota =>
            _servico?.AoInimigoDerrotado(derrota)
        );
        contexto.Eventos.Assinar<JogoDoGrupo.Server.Contratos.Combate.PersonagemCaiu>(_ =>
        {
            _servico?.VarrerDerrota();
        });
    }
}
