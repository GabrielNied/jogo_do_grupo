using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Economia;

public sealed class EconomiaModule : IFeatureModule
{
    public string Nome => "Economia";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao) =>
        servicos.AddScoped<JogoDoGrupo.Server.Contratos.Economia.ICarteiraCoroas, CarteiraCoroas>();

    public void Inicializar(ServerContext contexto) { }
}
