using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Fonte dos bônus de atributos do personagem que vêm dos efeitos ativos. Quem calcula os bônus
/// devolve o <b>total atual</b>, nunca um acréscimo: o servidor soma uma vez só, então o mesmo
/// bônus não é contado duas vezes. O equipamento <b>não</b> passa por aqui: os atributos dos itens
/// são uma fonte própria e nunca somam nos cinco do personagem (DEC-001, #623); Combate os lê pelo
/// contrato <c>IAtributosDosEquipamentos</c> de Itens e pela pipeline de modificadores.
/// </summary>
public interface IBonusDeAtributos
{
    /// <summary>Soma dos bônus dos efeitos ativos no personagem agora.</summary>
    Task<AtributosCombate> EfeitosAsync(long personagemId, CancellationToken cancelamento);
}

/// <summary>Efeitos ainda não existem: nenhum bônus até a sua implementação.</summary>
internal sealed class SemBonusDeEfeitos : IBonusDeAtributos
{
    public Task<AtributosCombate> EfeitosAsync(long personagemId, CancellationToken c) =>
        Task.FromResult(new AtributosCombate());
}
