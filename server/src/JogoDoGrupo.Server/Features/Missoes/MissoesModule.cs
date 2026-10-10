using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Missoes;

public sealed class MissoesModule : IFeatureModule
{
    public string Nome => "Missões";

    public void Inicializar(ServerContext contexto) { }
}
