using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Social;

public sealed class SocialModule : IFeatureModule
{
    public string Nome => "Social";

    public void Inicializar(ServerContext contexto) { }
}
