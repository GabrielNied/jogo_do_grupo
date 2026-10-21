using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Contratos.Itens;

/// <summary>
/// Um item tirado da bolsa e guardado por quem o negocia (troca direta, mercado). O conteúdo é
/// uma cópia opaca: quem guarda só devolve para <see cref="ICustodiaDeItens"/>.
/// </summary>
public sealed record ItemEmCustodia(int ItemId, string? EquipamentoJson);

/// <summary>
/// Move itens entre a bolsa e a custódia de quem negocia (Social na troca direta, Economia no
/// mercado). Item e moeda nunca duplicam: o item só existe na bolsa ou na custódia, e as duas
/// pontas rodam na mesma transação do chamador, que decide o commit. Participa da transação do
/// contexto do chamador; não confirma nada. Só a bolsa entra: equipado e favorito não negociam.
/// </summary>
public interface ICustodiaDeItens
{
    /// <summary>Os itens da bolsa que podem ser negociados (sem equipado nem favorito).</summary>
    Task<List<EntradaInventario>> ListarNegociaveisAsync(long personagemId, CancellationToken c);

    /// <summary>
    /// Consulta um item da bolsa sem retirá-lo. Nulo se ele não existe ou não está na bolsa.
    /// </summary>
    Task<ItemEmCustodia?> ObterAsync(long personagemId, long instanciaId, CancellationToken c);

    /// <summary>
    /// Tira o item da bolsa do personagem. Nulo se ele não existe, não é do personagem, está
    /// equipado ou é favorito (nada muda nesse caso).
    /// </summary>
    Task<ItemEmCustodia?> RetirarAsync(long personagemId, long instanciaId, CancellationToken c);

    /// <summary>Põe o item na primeira casa livre da bolsa. Falso, sem mudar nada, se cheia.</summary>
    Task<bool> DevolverAsync(long personagemId, ItemEmCustodia item, CancellationToken c);

    /// <summary>Descreve o item guardado para mostrar ao client (nome, atributos).</summary>
    EntradaInventario Descrever(ItemEmCustodia item, long instanciaId = 0);
}
