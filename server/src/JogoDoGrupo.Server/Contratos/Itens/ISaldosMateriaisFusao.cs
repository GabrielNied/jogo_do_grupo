namespace JogoDoGrupo.Server.Contratos.Itens;

/// <summary>
/// Saldos inteiros de materiais de fusão (dungeon e raid) por personagem (MAT-001).
/// Participa da transação do chamador; não confirma a transação (mesmo padrão de ICarteiraCoroas).
/// </summary>
public interface ISaldosMateriaisFusao
{
    Task<(int Dungeon, int Raid)> SaldoAsync(long personagemId, CancellationToken c);
    Task<bool> DebitarAsync(long personagemId, int dungeon, int raid, CancellationToken c);
    Task CreditarDungeonAsync(long personagemId, int quantidade, CancellationToken c);
    Task CreditarRaidAsync(long personagemId, int quantidade, CancellationToken c);
}
