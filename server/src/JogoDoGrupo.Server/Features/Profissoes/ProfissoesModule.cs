using JogoDoGrupo.Server.Core;

namespace JogoDoGrupo.Server.Features.Profissoes;

public sealed class ProfissoesModule : IFeatureModule
{
    public string Nome => "Profissões";

    public void Inicializar(ServerContext contexto) { }
}
