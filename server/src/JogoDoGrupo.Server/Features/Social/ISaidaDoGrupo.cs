using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Para onde vão as mensagens do grupo. No servidor é o tempo real (<c>SaidaTempoReal</c>); nos
/// testes de regra é uma lista em memória. Quem chama só envia a personagens online, e pode
/// chamar de dentro da trava do grupo: não bloqueie nem chame de volta o <c>GruposDoServidor</c>.
/// </summary>
public interface ISaidaDoGrupo
{
    /// <summary>Envia a lista completa do grupo (vazia = o personagem não tem grupo).</summary>
    void Lista(long personagemId, ListaDoGrupo lista);

    /// <summary>Envia um aviso (não é erro).</summary>
    void Aviso(long personagemId, AvisoDoGrupo aviso);

    /// <summary>Envia o convite a quem foi convidado.</summary>
    void Convite(long personagemId, ConviteRecebido convite);

    /// <summary>Avisa quem pediu que a operação foi recusada (um de <c>CodigosErroSocial</c>).</summary>
    void Recusada(long personagemId, string codigo);
}
