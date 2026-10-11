namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>
/// Ajustes do dia e noite que só o servidor usa. A duração do dia e as horas da noite ficam em
/// <c>RegrasDiaENoite</c>, no shared.
/// </summary>
public static class ConfigDiaENoiteServidor
{
    /// <summary>
    /// De quanto em quanto tempo, em segundos, o servidor repete a hora a quem está na área,
    /// para corrigir o relógio do client. Entre um envio e outro o client avança a hora sozinho.
    /// </summary>
    public const double IntervaloDeSincroniaSegundos = 30.0;
}
