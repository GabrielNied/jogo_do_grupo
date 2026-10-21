using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Contratos.Social;

/// <summary>
/// Permite que outros domínios autoritativos (como Economia no banco de guilda) consultem
/// a qual guilda um personagem pertence e qual seu cargo.
/// </summary>
public interface IConsultaDeGuilda
{
    long ObterGuildaId(long personagemId);
    string ObterNomeGuilda(long guildaId);
    CargoDaGuilda? ObterCargo(long personagemId);
}
