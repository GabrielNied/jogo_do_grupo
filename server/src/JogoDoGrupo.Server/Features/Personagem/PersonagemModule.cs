using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Personagem;

public sealed class PersonagemModule : IFeatureModule
{
    public string Nome => "Personagem";

    public void MapearRotas(IEndpointRouteBuilder rotas) => PersonagensRotas.Mapear(rotas);

    public void Inicializar(ServerContext contexto) { }
}
