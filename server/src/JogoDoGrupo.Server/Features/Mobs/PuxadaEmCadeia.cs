namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Atacar um inimigo puxa também os grupos da mesma espécie a até
/// <see cref="ConfigInimigosDoCampo.RaioDaPuxadaEmCadeiaMetros"/> dele (#915). Barulho de
/// combate sozinho não chama ninguém: só o ataque a um inimigo.
/// </summary>
public static class PuxadaEmCadeia
{
    /// <summary>Liga os grupos entre si; só grupos da mesma área se puxam.</summary>
    public static void Ligar(IEnumerable<GrupoDeInimigos> grupos)
    {
        var todos = grupos.ToArray();
        foreach (var grupo in todos)
        {
            grupo.AoSerAtacado = (origem, atacanteId, x, z) =>
            {
                foreach (var outro in todos)
                {
                    if (outro != origem && outro.Area == origem.Area)
                    {
                        outro.Puxar(
                            atacanteId,
                            origem.CriaturaId,
                            x,
                            z,
                            ConfigInimigosDoCampo.RaioDaPuxadaEmCadeiaMetros
                        );
                    }
                }
            };
        }
    }
}
