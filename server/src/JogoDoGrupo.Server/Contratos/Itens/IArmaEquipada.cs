using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Contratos.Itens;

/// <summary>A arma principal do conjunto ativo: o exemplar e o modelo-base de onde saem as skills.</summary>
public readonly record struct ArmaEquipada(
    long InstanciaId,
    int ModeloId,
    TipoEquipamento Tipo,
    int Conjunto,
    ElementoDaArma Elemento = ElementoDaArma.Nenhum
);

/// <summary>
/// Qual arma o personagem está usando agora (a principal do conjunto ativo), oferecida por Itens
/// (dono do equipamento) às outras funcionalidades (ADR 0016). Skills usa para saber de onde vêm
/// as 3 skills da barra. Devolve o <b>estado atual</b>; sem arma equipada, <c>null</c>.
/// </summary>
public interface IArmaEquipada
{
    Task<ArmaEquipada?> ObterAsync(long personagemId, CancellationToken cancelamento);
}
