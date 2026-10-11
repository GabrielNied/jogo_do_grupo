using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Itens;

public sealed class ItensModule : IFeatureModule
{
    public string Nome => "Itens";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao) =>
        servicos.AddSingleton<ISorteadorBau>(new SorteadorBau());

    public void MapearRotas(IEndpointRouteBuilder rotas) => ItensRotas.Mapear(rotas);

    public void Inicializar(ServerContext contexto) { }
}
