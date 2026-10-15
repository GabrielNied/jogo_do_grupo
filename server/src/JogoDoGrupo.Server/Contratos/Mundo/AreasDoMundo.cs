namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>Identificadores das áreas usados nos eventos e nas consultas do Mundo.</summary>
public static class AreasDoMundo
{
    /// <summary>A cidade e o terreno em volta dela.</summary>
    public const string Cidade = "cidade";

    /// <summary>
    /// Se a posição está na cidade e dentro dos muros (o quadrado de
    /// <c>MapaDoMundo.MetadeDaCidade</c> de cada lado do centro). Fora dos muros é o terreno.
    /// </summary>
    public static bool DentroDosMuros(PosicaoNoMundo posicao) =>
        posicao.Area == Cidade
        && Math.Abs(posicao.X) <= JogoDoGrupo.Shared.Features.Mundo.MapaDoMundo.MetadeDaCidade
        && Math.Abs(posicao.Z) <= JogoDoGrupo.Shared.Features.Mundo.MapaDoMundo.MetadeDaCidade;

    /// <summary>Cria o identificador estável de uma instância.</summary>
    public static string Instancia(string id) => $"instancia:{id}";

    /// <summary>Indica se o identificador tem formato conhecido e não vazio.</summary>
    public static bool EhValida(string? area) =>
        area == Cidade
        || (
            area != null
            && area.StartsWith("instancia:", StringComparison.Ordinal)
            && area.Length > "instancia:".Length
            && !string.IsNullOrWhiteSpace(area["instancia:".Length..])
            && !area.Contains('\n')
            && !area.Contains('\r')
        );
}
