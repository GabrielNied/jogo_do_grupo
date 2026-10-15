namespace JogoDoGrupo.Server.Contratos.Itens;

/// <summary>
/// Troca o conjunto de armas ativo do personagem (a outra arma entra na mão), oferecida por Itens
/// (dono do equipamento) às outras funcionalidades (ADR 0016). Combate usa na troca de arma (R),
/// depois de reservar os 0,4 s em que golpe, esquiva e guarda são recusados. Grava, avisa a área
/// do visual e publica <see cref="EquipamentoMudou"/>.
/// </summary>
public interface ITrocaDeConjunto
{
    /// <summary>
    /// Vira do conjunto 1 para o 2 (ou do 2 para o 1) e devolve o conjunto que ficou ativo, ou
    /// <c>null</c> se o personagem não existe.
    /// </summary>
    Task<int?> AlternarAsync(long personagemId, CancellationToken cancelamento);
}
