using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>
/// O relógio único do mundo (issue #163): começa em <see cref="RegrasDiaENoite.HoraInicial"/>
/// quando o servidor liga e anda sozinho, no ritmo de <see cref="RegrasDiaENoite"/>. Quem entra
/// lê a hora que está valendo; ninguém a reinicia. Fica em memória: reiniciar o servidor volta
/// à hora inicial. Pode ser lido de qualquer thread.
/// </summary>
public sealed class RelogioDoMundo(TimeProvider relogio) : IRelogioDoMundo
{
    private readonly DateTimeOffset _ligadoEm = relogio.GetUtcNow();

    /// <summary>A hora do mundo agora, de 0 até menos de 24.</summary>
    public float Hora() =>
        RegrasDiaENoite.HoraApos(
            RegrasDiaENoite.HoraInicial,
            (relogio.GetUtcNow() - _ligadoEm).TotalSeconds
        );

    /// <summary>Se é noite no momento atual.</summary>
    public bool EhNoite() => RegrasDiaENoite.EhNoite(Hora());

    /// <summary>A hora de agora, pronta para enviar.</summary>
    public HoraDoMundo Mensagem() =>
        new() { Hora = Hora(), DuracaoDoDiaSegundos = RegrasDiaENoite.DuracaoDoDiaSegundos };
}
