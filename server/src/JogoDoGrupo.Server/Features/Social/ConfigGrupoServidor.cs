namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Valores do grupo que só o servidor usa. Os números das regras (5 membros, 60 s, 120 s, 30 s...)
/// ficam em <c>RegrasGrupo</c>, no shared; aqui só os prazos de manutenção do servidor.
/// </summary>
public static class ConfigGrupoServidor
{
    /// <summary>
    /// Intervalo, em milissegundos, entre as varreduras que expiram convites e removem vagas
    /// guardadas vencidas. Define o atraso máximo do aviso de "expirou" depois dos 60 s.
    /// </summary>
    public const int IntervaloDaVarreduraMs = 1000;

    /// <summary>
    /// Segundos que o servidor lembra de um convite que acabou sem resposta (expirou, o convidante
    /// caiu ou o alvo entrou em outro grupo), só para devolver o código certo se o alvo ainda
    /// tentar aceitá-lo. Passado esse tempo ele responde <c>ConviteNaoEncontrado</c>.
    /// </summary>
    public const int RetencaoDeConviteEncerradoSegundos = 60;

    /// <summary>
    /// Segundos que o servidor lembra do último convite de cada convidante, para limitar a
    /// frequência. Precisa ser maior que o intervalo mínimo entre convites.
    /// </summary>
    public const int RetencaoDoUltimoConviteSegundos = 10;
}
