using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Itens;

public sealed class ItensModule : IFeatureModule
{
    // Criado pelo contêiner quando o app sobe (hosted service); os manipuladores de rede e de
    // eventos só o usam depois que uma sessão se autentica.
    private volatile ServicoDeLoot? _loot;

    public string Nome => "Itens";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        var configEquipamentos = new ConfigEquipamentos();
        configuracao.GetSection("Itens:Equipamentos").Bind(configEquipamentos);
        servicos.AddSingleton(configEquipamentos);
        servicos.AddSingleton<GeradorEquipamentos>();
        servicos.AddSingleton<AssinadorFusao>();
        servicos.AddSingleton<EquipamentosParaCombate>();
        servicos.AddSingleton<JogoDoGrupo.Server.Contratos.Itens.IAtributosDosEquipamentos>(sp =>
            sp.GetRequiredService<EquipamentosParaCombate>()
        );
        servicos.AddSingleton<JogoDoGrupo.Server.Contratos.Combate.IFonteDeModificadores>(sp =>
            sp.GetRequiredService<EquipamentosParaCombate>()
        );
        servicos.AddHostedService<MigradorDeEquipamentos>();
        servicos.AddSingleton<ISorteadorBau>(new SorteadorBau());
        servicos.AddSingleton<ISorteadorDeLoot>(new SorteadorDeLoot());
        servicos.AddSingleton<ChaoDeLoot>();
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDeLoot(
                sp.GetRequiredService<ChaoDeLoot>(),
                sp.GetRequiredService<ISorteadorDeLoot>(),
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<ILogger<ServicoDeLoot>>()
            );
            _loot = servico;
            return servico;
        });
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDeLoot>());
    }

    public void MapearRotas(IEndpointRouteBuilder rotas) => ItensRotas.Mapear(rotas);

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(ItensMessageIds.Faixa)
            .Ao<ColetarLoot>(
                ItensMessageIds.ColetarLoot,
                (sessao, pedido) => _loot?.AoColetar(sessao, pedido)
            );

    public void Inicializar(ServerContext contexto)
    {
        contexto.Eventos.Assinar<InimigoDerrotado>(evento => _loot?.AoInimigoDerrotado(evento));
        contexto.Eventos.Assinar<JogadorEntrouNaArea>(evento => _loot?.AoEntrarNaArea(evento));
    }
}
