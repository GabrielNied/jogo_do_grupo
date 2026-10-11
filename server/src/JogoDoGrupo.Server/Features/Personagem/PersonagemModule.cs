using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;

namespace JogoDoGrupo.Server.Features.Personagem;

public sealed class PersonagemModule : IFeatureModule
{
    // O contêiner cria o serviço quando o app sobe; a morte só chega depois, no laço de IA.
    private volatile ExperienciaDoPersonagem? _experiencia;

    public string Nome => "Personagem";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddScoped<IVerificadorPersonagem, VerificadorPersonagem>();
        servicos.AddSingleton<IDiretorioDePersonagens, DiretorioDePersonagens>();
        servicos.AddSingleton(sp =>
        {
            var servico = new ExperienciaDoPersonagem(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetRequiredService<ILogger<ExperienciaDoPersonagem>>()
            );
            _experiencia = servico;
            return servico;
        });
        servicos.AddSingleton<IPontosDistribuidos>(sp =>
            sp.GetRequiredService<ExperienciaDoPersonagem>()
        );
        servicos.AddSingleton<IAjusteDeExperienciaDeTeste>(sp =>
            sp.GetRequiredService<ExperienciaDoPersonagem>()
        );
        servicos.AddHostedService<IniciarExperiencia>();
    }

    public void MapearRotas(IEndpointRouteBuilder rotas) => PersonagensRotas.Mapear(rotas);

    public void Inicializar(ServerContext contexto) =>
        contexto.Eventos.Assinar<InimigoDerrotado>(derrota => _experiencia?.Agendar(derrota));
}

/// <summary>
/// Garante que a experiência existe antes de o servidor aceitar sessão. O construtor resolve
/// o serviço e preenche o campo que o evento de morte lê.
/// </summary>
internal sealed class IniciarExperiencia : IHostedService
{
    public IniciarExperiencia(ExperienciaDoPersonagem experiencia) => _ = experiencia;

    public Task StartAsync(CancellationToken cancelamento) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancelamento) => Task.CompletedTask;
}
