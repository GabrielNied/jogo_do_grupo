namespace JogoDoGrupo.Server.Contratos.Economia;

/// <summary>
/// Soma ou tira coroas de um personagem, numa transação própria. Só a tela de teste de
/// Desenvolvimento chama; o ganho e o gasto normais continuam na Economia.
/// </summary>
public interface IAjusteDeCoroasDeTeste
{
    /// <summary>
    /// Positivo credita; negativo debita até o saldo (nunca deixa negativo). Devolve o texto
    /// com o saldo novo.
    /// </summary>
    Task<(bool Aceito, string Texto)> AjustarAsync(
        long personagemId,
        long quantidade,
        CancellationToken c
    );
}
