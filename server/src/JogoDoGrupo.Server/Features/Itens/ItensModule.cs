using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Itens;

public sealed class ItensModule : IFeatureModule
{
    public string Nome => "Itens";

    public void Inicializar(ServerContext contexto) { }
}
