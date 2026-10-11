namespace JogoDoGrupo.Server.Contratos.Personagem;

/// <summary>
/// Consulta de nome e id de personagem oferecida por Personagem às outras funcionalidades
/// (ADR 0016). Peça pela injeção de dependência. É só leitura e responde se o personagem
/// <b>existe</b>; se ele está online quem sabe é o Mundo (<c>IPosicoesDoMundo</c>). Pode ser
/// chamada de qualquer thread; cada chamada faz uma leitura curta no banco.
/// </summary>
public interface IDiretorioDePersonagens
{
    /// <summary>
    /// O id do personagem com esse nome, sem diferenciar maiúsculas ("Ana" acha "ana"). Falso se
    /// não existe ou se o texto nem é um nome válido.
    /// </summary>
    bool TentarObterPorNome(string nome, out long personagemId);

    /// <summary>
    /// O nome do personagem como foi digitado na criação (mantém as maiúsculas). Falso, com
    /// nome vazio, se o id não existe.
    /// </summary>
    bool TentarObterNome(long personagemId, out string nome);

    /// <summary>A conta dona do personagem. Falso se o id não existe.</summary>
    bool TentarObterContaDoPersonagem(long personagemId, out long contaId);
}
