using System.Collections.Concurrent;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Leva as mensagens do grupo pelo tempo real (UDP), todas confiáveis e em ordem. Guarda a sessão
/// de cada personagem online para achar o destino pelo id do personagem.
/// </summary>
public sealed class SaidaTempoReal(ServidorTempoReal tempoReal)
    : ISaidaDoGrupo,
        ISaidaAmigos,
        ISaidaDeMarcadores,
        ISaidaDeInspecao,
        ISaidaDeTroca
{
    private readonly ConcurrentDictionary<long, SessaoTempoReal> _sessoes = new();

    /// <summary>A sessão passa a ser o destino do personagem (substitui a anterior, se houver).</summary>
    public void Registrar(SessaoTempoReal sessao) => _sessoes[sessao.PersonagemId] = sessao;

    /// <summary>
    /// Esquece a sessão. Falso se ela já tinha sido substituída por outra do mesmo personagem.
    /// </summary>
    public bool Remover(SessaoTempoReal sessao) =>
        _sessoes.TryRemove(new KeyValuePair<long, SessaoTempoReal>(sessao.PersonagemId, sessao));

    public bool EstaOnline(long personagemId) => _sessoes.ContainsKey(personagemId);

    public void Retrato(long personagemId, RetratoInspecaoResponse retrato) =>
        Enviar(personagemId, SocialMessageIds.RetratoInspecaoResponse, retrato);

    public void Recusada(long personagemId, InspecaoRecusada recusa) =>
        Enviar(personagemId, SocialMessageIds.InspecaoRecusada, recusa);

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

    public void ListaDeAmigos(long personagemId, ListaDeAmigos lista) =>
        Enviar(personagemId, SocialMessageIds.ListaDeAmigos, lista);

    public void ListaDeIgnorados(long personagemId, ListaDeIgnorados lista) =>
        Enviar(personagemId, SocialMessageIds.ListaDeIgnorados, lista);

    public void AmigoMudouDeEstado(long personagemId, AmigoMudouDeEstado mudanca) =>
        Enviar(personagemId, SocialMessageIds.AmigoMudouDeEstado, mudanca);

    public void RecusarAmizade(long personagemId, string codigo) =>
        Enviar(
            personagemId,
            SocialMessageIds.OperacaoDeAmizadeRecusada,
            new OperacaoDeAmizadeRecusada { Codigo = codigo }
        );

    public void Marcadores(long personagemId, MarcadoresDeAlvoMudaram mensagem) =>
        Enviar(personagemId, SocialMessageIds.MarcadoresDeAlvoMudaram, mensagem);

    public void Recusada(long personagemId, DefinirMarcadorRecusado recusa) =>
        Enviar(personagemId, SocialMessageIds.DefinirMarcadorRecusado, recusa);

    public void Convite(long personagemId, ConviteDeTroca convite) =>
        Enviar(personagemId, SocialMessageIds.ConviteDeTroca, convite);

    public void Estado(long personagemId, EstadoDaTroca estado) =>
        Enviar(personagemId, SocialMessageIds.EstadoDaTroca, estado);

    public void Bolsa(long personagemId, BolsaParaTroca bolsa) =>
        Enviar(personagemId, SocialMessageIds.BolsaParaTroca, bolsa);

    public void Encerrada(long personagemId, TrocaEncerrada encerrada) =>
        Enviar(personagemId, SocialMessageIds.TrocaEncerrada, encerrada);

    public void Recusada(long personagemId, TrocaRecusada recusa) =>
        Enviar(personagemId, SocialMessageIds.TrocaRecusada, recusa);

    private void Enviar<T>(long personagemId, ushort id, T mensagem)
    {
        if (_sessoes.TryGetValue(personagemId, out var sessao))
        {
            tempoReal.Enviar(sessao, id, mensagem);
        }
    }
}
