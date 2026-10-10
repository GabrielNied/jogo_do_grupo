using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Combate;

public sealed class CombateModule : IFeatureModule
{
    public string Nome => "Combate";

    public void Inicializar(ServerContext contexto) { }
}
