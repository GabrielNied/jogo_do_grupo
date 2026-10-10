using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Mundo;

public sealed class MundoModule : IFeatureModule
{
    public string Nome => "Mundo";

    public void Inicializar(ServerContext contexto) { }
}
