using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Contratos.Itens;

/// <summary>
/// Consulta das peças de equipamento ativas de um personagem e seus atributos resumidos,
/// oferecida por Itens às outras funcionalidades (como Social para inspeção).
/// </summary>
public interface IConsultaInspecaoEquipamento
{
    Task<(
        IReadOnlyList<EntradaInventario> Equipados,
        int ConjuntoAtivo,
        AtributosDosEquipamentos Atributos
    )> ObterAsync(long personagemId, CancellationToken cancelamento);
}
