namespace JogoDoGrupo.Server.Contratos.Itens;

/// <summary>
/// Contrato para consumir itens da bolsa (poções, comidas e consumíveis em geral).
/// Mantém as regras de bolsa em Itens (ADR 0016).
/// </summary>
public interface IConsumoDeItens
{
    /// <summary>
    /// Consome um item consumível do personagem. Se <paramref name="instanciaId"/> for maior que 0,
    /// consome a instância específica se coincidir com o modelo (ou se modeloId for 0).
    /// Se <paramref name="instanciaId"/> for 0 ou menor, consome a primeira ocorrência do <paramref name="modeloId"/>.
    /// Retorna o ID do modelo consumido, ou null se não encontrou ou não pôde consumir.
    /// </summary>
    Task<int?> ConsumirConsumivelAsync(
        long personagemId,
        long instanciaId,
        int modeloId,
        CancellationToken c
    );
}
