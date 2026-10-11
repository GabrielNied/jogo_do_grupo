using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Server.Contratos.Personagem;

/// <summary>
/// Pontos de atributo que o jogador já distribuiu, oferecidos por Personagem a Combate
/// (ADR 0016). Não inclui base, equipamento nem efeito. Pode ser chamada de qualquer thread.
/// </summary>
public interface IPontosDistribuidos
{
    /// <summary>Zeros se o personagem não existe.</summary>
    Task<PontosDeAtributos> ObterAsync(long personagemId, CancellationToken cancelamento);
}
