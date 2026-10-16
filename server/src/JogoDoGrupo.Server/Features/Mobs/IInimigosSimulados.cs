using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Algo que o laço de IA do <see cref="ServicoDeInimigos"/> avança a cada passo: o grupo de
/// inimigos comuns e o boss da dungeon. Quem implementa decide tudo e devolve o que mudou; o
/// serviço envia e publica.
/// </summary>
public interface IInimigosSimulados
{
    /// <summary>Área do mundo em que os inimigos vivem.</summary>
    string Area { get; }

    /// <summary>Avança a IA até agora; <c>jogadores</c> são os personagens da área.</summary>
    PassoDosInimigos Passo(IReadOnlyList<JogadorNoCampo> jogadores);

    /// <summary>Retrato dos inimigos vivos agora (o que vai em <c>InimigosDaArea</c>).</summary>
    InimigoNoMundo[] Vivos();

    /// <summary>
    /// Um personagem curou um aliado: se o curado participa da luta com algum inimigo daqui, o
    /// curador passa a participar dela também (loot e experiência, #909). Sem efeito por padrão.
    /// </summary>
    void RegistrarCura(long curadorId, long curadoId) { }
}
