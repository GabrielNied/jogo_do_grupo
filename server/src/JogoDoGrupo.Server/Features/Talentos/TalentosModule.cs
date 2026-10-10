using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Talentos;

public sealed class TalentosModule : IFeatureModule
{
    public string Nome => "Habilidades (talentos)";

    public void Inicializar(ServerContext contexto) { }
}
