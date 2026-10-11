using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Fonte dos bônus de atributos que vêm de fora de Combate (equipamentos e efeitos). Quem
/// calcula os bônus devolve o <b>total atual</b> de cada fonte, nunca um acréscimo: o servidor
/// soma uma vez só, então o mesmo bônus não é contado duas vezes. Itens e efeitos trocam a
/// implementação por esta interface quando existirem (hoje não há equipamento nem efeito).
/// </summary>
public interface IBonusDeAtributos
{
    /// <summary>Soma dos bônus dos equipamentos que o personagem está usando agora.</summary>
    Task<AtributosCombate> EquipamentosAsync(long personagemId, CancellationToken cancelamento);

    /// <summary>Soma dos bônus dos efeitos ativos no personagem agora.</summary>
    Task<AtributosCombate> EfeitosAsync(long personagemId, CancellationToken cancelamento);
}

/// <summary>Padrão enquanto não existem equipamentos nem efeitos: nenhum bônus.</summary>
internal sealed class SemBonusDeAtributos : IBonusDeAtributos
{
    public Task<AtributosCombate> EquipamentosAsync(long personagemId, CancellationToken c) =>
        Task.FromResult(new AtributosCombate());

    public Task<AtributosCombate> EfeitosAsync(long personagemId, CancellationToken c) =>
        Task.FromResult(new AtributosCombate());
}
