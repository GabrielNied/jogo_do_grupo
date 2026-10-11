using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;

namespace JogoDoGrupo.Server.Features.Personagem;

public sealed class PersonagemModule : IFeatureModule
{
    public string Nome => "Personagem";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddScoped<IVerificadorPersonagem, VerificadorPersonagem>();
        servicos.AddSingleton<IDiretorioDePersonagens, DiretorioDePersonagens>();
    }

    public void MapearRotas(IEndpointRouteBuilder rotas) => PersonagensRotas.Mapear(rotas);

    public void Inicializar(ServerContext contexto) { }
}
