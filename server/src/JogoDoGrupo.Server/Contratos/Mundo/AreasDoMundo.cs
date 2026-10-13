namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>Identificadores das áreas usados nos eventos e nas consultas do Mundo.</summary>
public static class AreasDoMundo
{
    /// <summary>A cidade e o terreno em volta dela.</summary>
    public const string Cidade = "cidade";

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
