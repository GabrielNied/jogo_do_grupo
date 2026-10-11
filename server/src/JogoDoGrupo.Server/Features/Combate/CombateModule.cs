using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Combate;

public sealed class CombateModule : IFeatureModule
{
    public string Nome => "Combate";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao) =>
        servicos.AddSingleton<IBonusDeAtributos, SemBonusDeAtributos>();

    public void MapearRotas(IEndpointRouteBuilder rotas) => CombateRotas.Mapear(rotas);

    public void Inicializar(ServerContext contexto) { }
}
