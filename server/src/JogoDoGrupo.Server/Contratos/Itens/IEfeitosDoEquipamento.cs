using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Contratos.Itens;

/// <summary>
/// Os efeitos que as peças vestidas ligam (poderes lendários e marcos de set), oferecidos por
/// Itens (dono do equipamento) às outras funcionalidades (ADR 0016). Skills usa para aplicar o
/// efeito quando o personagem usa uma skill. Devolve o <b>estado atual</b>, só do conjunto de
/// armas ativo; quem guarda o resultado invalida em <see cref="EquipamentoMudou"/>.
/// </summary>
public interface IEfeitosDoEquipamento
{
    Task<IReadOnlyList<EfeitoAtivo>> ObterAsync(long personagemId, CancellationToken cancelamento);
}
