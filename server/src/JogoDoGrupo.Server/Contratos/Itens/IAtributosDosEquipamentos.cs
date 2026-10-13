using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Contratos.Itens;

/// <summary>
/// O que as peças vestidas e o conjunto de armas ativo dão ao combate, <b>já convertido</b>
/// (ATRIB-003) e como fonte própria: nunca soma em Força, Vigor, Agilidade, Intelecto ou Espírito
/// do personagem (DEC-001). Devolve o <b>total atual</b>, nunca um incremento. Os nove secundários
/// do equipamento chegam pela pipeline, pela <c>IFonteDeModificadores</c> que Itens registra.
/// </summary>
public interface IAtributosDosEquipamentos
{
    Task<AtributosDosEquipamentos> ObterAsync(long personagemId, CancellationToken cancelamento);
}
