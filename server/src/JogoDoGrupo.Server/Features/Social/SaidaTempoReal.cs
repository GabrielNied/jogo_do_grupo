using System.Collections.Concurrent;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Leva as mensagens do grupo pelo tempo real (UDP), todas confiáveis e em ordem. Guarda a sessão
/// de cada personagem online para achar o destino pelo id do personagem.
/// </summary>
public sealed class SaidaTempoReal(ServidorTempoReal tempoReal) : ISaidaDoGrupo
{
    private readonly ConcurrentDictionary<long, SessaoTempoReal> _sessoes = new();

    /// <summary>A sessão passa a ser o destino do personagem (substitui a anterior, se houver).</summary>
    public void Registrar(SessaoTempoReal sessao) => _sessoes[sessao.PersonagemId] = sessao;

    /// <summary>
    /// Esquece a sessão. Falso se ela já tinha sido substituída por outra do mesmo personagem.
    /// </summary>
    public bool Remover(SessaoTempoReal sessao) =>
        _sessoes.TryRemove(new KeyValuePair<long, SessaoTempoReal>(sessao.PersonagemId, sessao));

    public void Lista(long personagemId, ListaDoGrupo lista) =>
        Enviar(personagemId, SocialMessageIds.ListaDoGrupo, lista);

    public void Aviso(long personagemId, AvisoDoGrupo aviso) =>
        Enviar(personagemId, SocialMessageIds.AvisoDoGrupo, aviso);

    public void Convite(long personagemId, ConviteRecebido convite) =>
        Enviar(personagemId, SocialMessageIds.ConviteRecebido, convite);

    public void Recusada(long personagemId, string codigo) =>
        Enviar(
            personagemId,
            SocialMessageIds.OperacaoDoGrupoRecusada,
            new OperacaoDoGrupoRecusada { Codigo = codigo }
        );

    private void Enviar<T>(long personagemId, ushort id, T mensagem)
    {
        if (_sessoes.TryGetValue(personagemId, out var sessao))
        {
            tempoReal.Enviar(sessao, id, mensagem);
        }
    }
}
