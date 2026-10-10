using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Pvp;

public sealed class PvpModule : IFeatureModule
{
    public string Nome => "PvP";

    public void Inicializar(ServerContext contexto) { }
}
