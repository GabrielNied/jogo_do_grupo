namespace JogoDoGrupo.Server.Core.TempoReal;

/// <summary>
/// Diz se um personagem pertence à conta. O Core não conhece a tabela de personagens: a
/// funcionalidade Personagem registra a implementação no <c>ConfigurarServicos</c>
/// (ex.: <c>servicos.AddScoped&lt;IVerificadorPersonagem, VerificadorPersonagem&gt;()</c>).
/// Sem implementação registrada, toda autenticação de tempo real é recusada com
/// <c>AcessoNegado</c>. Roda num escopo de DI próprio, então pode usar o <c>JogoDbContext</c>.
/// </summary>
public interface IVerificadorPersonagem
{
    Task<bool> PertenceAContaAsync(long contaId, long personagemId, CancellationToken cancelamento);
}
