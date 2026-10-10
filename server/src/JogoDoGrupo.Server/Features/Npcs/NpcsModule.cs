using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Npcs;

public sealed class NpcsModule : IFeatureModule
{
    public string Nome => "NPCs";

    public void Inicializar(ServerContext contexto) { }
}
