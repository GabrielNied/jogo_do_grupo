using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Mobs;

public sealed class MobsModule : IFeatureModule
{
    public string Nome => "Mobs e bosses";

    public void Inicializar(ServerContext contexto) { }
}
