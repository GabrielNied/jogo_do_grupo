using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Pets;

namespace JogoDoGrupo.Server.Features.Pets;

public sealed class PetsModule : IFeatureModule
{
    // Criado pelo contêiner quando o app sobe (hosted service); mensagens e eventos só chegam
    // depois que uma sessão se autentica.
    private volatile ServicoDePets? _pets;

    public string Nome => "Pets";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDePets(
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetRequiredService<IVizinhanca>(),
                sp.GetService<ILootDoDono>(),
                sp.GetRequiredService<ILogger<ServicoDePets>>()
            );
            _pets = servico;
            return servico;
        });
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDePets>());
    }

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(PetsMessageIds.Faixa)
            .Ao<ChamarPet>(
                PetsMessageIds.ChamarPet,
                (sessao, pedido) => _pets?.AoChamar(sessao, pedido)
            )
            .Ao<DispensarPet>(
                PetsMessageIds.DispensarPet,
                (sessao, _) => _pets?.AoDispensar(sessao)
            );

    public void Inicializar(ServerContext contexto)
    {
        contexto.Eventos.Assinar<JogadorSaiuDaArea>(evento => _pets?.AoSairDaArea(evento));
        contexto.Eventos.Assinar<JogadorEntrouNaArea>(evento => _pets?.AoEntrarNaArea(evento));
        contexto.AoPasso(
            TimeSpan.FromSeconds(PassoSegundos),
            _ => _pets?.Avancar((float)PassoSegundos),
            "Pets"
        );
    }

    /// <summary>Passo de movimento dos pets: o mesmo ritmo da IA dos mobs (10 por segundo).</summary>
    private const double PassoSegundos = 0.1;
}
