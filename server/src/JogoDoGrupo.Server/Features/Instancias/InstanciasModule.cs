using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Instancias;

public sealed class InstanciasModule : IFeatureModule
{
    public string Nome => "Dungeons e raids";

    public void Inicializar(ServerContext contexto) { }
}
