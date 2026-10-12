using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Contratos.Itens;

/// <summary>Totais atuais das peças vestidas e do conjunto de armas ativo. Nunca incrementos.</summary>
public interface IBonusEquipamentos
{
    Task<AtributoItem[]> ObterAsync(long personagemId, CancellationToken cancelamento);
}
