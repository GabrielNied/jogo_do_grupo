using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Economia;

public sealed class EconomiaModule : IFeatureModule
{
    public string Nome => "Economia";

    public void Inicializar(ServerContext contexto) { }
}
