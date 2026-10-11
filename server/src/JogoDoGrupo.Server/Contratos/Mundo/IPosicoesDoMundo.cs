namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>
/// Consulta de posição oferecida pelo Mundo às outras funcionalidades (ADR 0016). Peça pela
/// injeção de dependência; a resposta é sempre a posição que o servidor aceitou, nunca a que
/// o client pediu. Pode ser chamada de qualquer thread.
/// </summary>
public interface IPosicoesDoMundo
{
    /// <summary>A posição do personagem, ou falso se ele não está em nenhuma área.</summary>
    bool TentarObter(long personagemId, out PosicaoNoMundo posicao);

    /// <summary>Os personagens que estão na área agora (vazio se a área não existe).</summary>
    IReadOnlyList<long> PersonagensNaArea(string area);
}
