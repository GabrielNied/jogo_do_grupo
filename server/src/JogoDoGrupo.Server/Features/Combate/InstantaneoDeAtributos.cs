using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Snapshot imutável dos atributos finais calculados de um personagem para combate.
/// </summary>
internal sealed record InstantaneoDeAtributos(
    AtributosCombate Atributos,
    MaximosDeRecursos Maximos,
    ParcelasDeDano Dano,
    string[] LinhasDoEquipamento,
    SecundarioCalculado[] Secundarios,
    AtributosSecundarios Valores
);
