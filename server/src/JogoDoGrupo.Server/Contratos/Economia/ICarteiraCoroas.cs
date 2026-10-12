namespace JogoDoGrupo.Server.Contratos.Economia;

/// <summary>Participa da transação do contexto do chamador; não confirma a transação.</summary>
public interface ICarteiraCoroas
{
    Task<long> SaldoAsync(long personagemId, CancellationToken c);
    Task<bool> DebitarAsync(long personagemId, long valor, CancellationToken c);
    Task CreditarAsync(long personagemId, long valor, CancellationToken c);
}
