using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Para onde vão as mensagens da guilda. No servidor é o tempo real (<c>SaidaTempoReal</c>);
/// nos testes de regra é uma lista em memória.
/// </summary>
public interface ISaidaDaGuilda
{
    void Dados(long personagemId, DadosDaGuilda dados);
    void Convite(long personagemId, ConviteDeGuildaRecebido convite);
    void Recusada(long personagemId, string codigo);
}
