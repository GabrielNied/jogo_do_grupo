namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>Identificadores das áreas usados nos eventos e nas consultas do Mundo.</summary>
public static class AreasDoMundo
{
    /// <summary>A cidade e o terreno em volta dela no canal padrão (canal 1).</summary>
    public const string Cidade = "cidade";

    /// <summary>Obtém o identificador da cidade para um canal específico (canal 1 = "cidade", canal 2 = "cidade:2", etc.).</summary>
    public static string CidadeDoCanal(int canal) => canal <= 1 ? Cidade : $"cidade:{canal}";

    /// <summary>Extrai o canal a partir do identificador da área.</summary>
    public static int CanalDaArea(string? area)
    {
        if (string.IsNullOrEmpty(area) || area == Cidade)
        {
            return 1;
        }

        if (
            area.StartsWith("cidade:", StringComparison.Ordinal)
            && int.TryParse(area["cidade:".Length..], out var c)
            && c >= 1
        )
        {
            return c;
        }

        return 1;
    }

    /// <summary>Indica se a área é a cidade em qualquer um dos seus canais.</summary>
    public static bool EhCidade(string? area) =>
        area != null
        && (
            area == Cidade
            || (
                area.StartsWith("cidade:", StringComparison.Ordinal)
                && int.TryParse(area["cidade:".Length..], out var c)
                && c > 1
            )
        );

    /// <summary>
    /// Se a posição está na cidade e dentro dos muros (o quadrado de
    /// <c>MapaDoMundo.MetadeDaCidade</c> de cada lado do centro). Fora dos muros é o terreno.
    /// </summary>
    public static bool DentroDosMuros(PosicaoNoMundo posicao) =>
        EhCidade(posicao.Area)
        && Math.Abs(posicao.X) <= JogoDoGrupo.Shared.Features.Mundo.MapaDoMundo.MetadeDaCidade
        && Math.Abs(posicao.Z) <= JogoDoGrupo.Shared.Features.Mundo.MapaDoMundo.MetadeDaCidade;

    /// <summary>Cria o identificador estável de uma instância.</summary>
    public static string Instancia(string id) => $"instancia:{id}";

    /// <summary>Indica se o identificador tem formato conhecido e não vazio.</summary>
    public static bool EhValida(string? area) =>
        EhCidade(area)
        || (
            area != null
            && area.StartsWith("instancia:", StringComparison.Ordinal)
            && area.Length > "instancia:".Length
            && !string.IsNullOrWhiteSpace(area["instancia:".Length..])
            && !area.Contains('\n')
            && !area.Contains('\r')
        );
}
