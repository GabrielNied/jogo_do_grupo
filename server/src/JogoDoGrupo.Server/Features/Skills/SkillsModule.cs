using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Skills;

public sealed class SkillsModule : IFeatureModule
{
    public string Nome => "Skills (magias)";

    public void Inicializar(ServerContext contexto) { }
}
