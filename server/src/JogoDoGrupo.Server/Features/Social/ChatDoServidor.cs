using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// O que fazer com um pedido de chat: ou entregar <see cref="Mensagem"/> a
/// <see cref="Destinatarios"/> (ids de personagem, o remetente entre eles), ou devolver
/// <see cref="Recusa"/> só a quem pediu.
/// </summary>
public sealed record ResultadoDoChat(
    MensagemDeChat? Mensagem,
    IReadOnlyList<long> Destinatarios,
    MensagemDeChatRecusada? Recusa
)
{
    public static ResultadoDoChat Recusar(string codigo, int segundosRestantes = 0) =>
        new(
            null,
            [],
            new MensagemDeChatRecusada { Codigo = codigo, SegundosRestantes = segundosRestantes }
        );
}

/// <summary>
/// Decide o chat de texto (regras em docs/features/social.md): quem falou, o que chega e a
/// quem. Não conhece a rede: recebe o id de quem pediu (da sessão autenticada, nunca de um
/// campo do client) e devolve o que entregar. Quem é o grupo de um personagem vem de quem
/// chama (<c>grupoDe</c>), para o chat não depender de como os grupos são guardados.
/// </summary>
public sealed class ChatDoServidor(
    LimitesDoChat limites,
    IDiretorioDePersonagens diretorio,
    IPosicoesDoMundo posicoes,
    TimeProvider relogio
)
{
    /// <param name="remetenteId">Personagem da sessão que mandou o pedido.</param>
    /// <param name="pedido">O que o client mandou.</param>
    /// <param name="estaOnline">Se um personagem tem sessão de tempo real agora.</param>
    /// <param name="grupoDe">
    /// A lista do grupo de um personagem (vazia se não tem grupo). Sem ela, ninguém tem grupo.
    /// </param>
    public ResultadoDoChat Processar(
        long remetenteId,
        EnviarMensagemDeChat pedido,
        Func<long, bool> estaOnline,
        Func<long, ListaDoGrupo>? grupoDe = null,
        Func<long, long, bool>? contatoBarradoEntrePersonagens = null
    )
    {
        if (!RegrasChat.CanalValido(pedido.Canal))
        {
            return ResultadoDoChat.Recusar(CodigosErroSocial.CanalIndisponivel);
        }

        // Os limites vêm antes de qualquer consulta ao banco: quem está mandando rápido demais
        // não faz o servidor trabalhar.
        var limite = limites.Conferir(remetenteId, pedido.Canal, pedido.Texto);
        if (!limite.Passou)
        {
            return ResultadoDoChat.Recusar(limite.Codigo!, limite.SegundosRestantes);
        }

        if (!diretorio.TentarObterNome(remetenteId, out var nomeDoRemetente))
        {
            // Sessão de um personagem que não existe mais: não fala em canal nenhum.
            return ResultadoDoChat.Recusar(CodigosErroSocial.CanalIndisponivel);
        }

        var nomeDoAlvo = "";
        IReadOnlyList<long> destinatarios;
        switch (pedido.Canal)
        {
            case CanalDeChat.Cidade:
                // Só dentro dos muros (#500): o mundo tem 3 km, e o canal não chega à lava.
                if (
                    !posicoes.TentarObter(remetenteId, out var posicaoDoRemetente)
                    || !AreasDoMundo.DentroDosMuros(posicaoDoRemetente)
                )
                {
                    return ResultadoDoChat.Recusar(CodigosErroSocial.CanalIndisponivel);
                }

                destinatarios = posicoes
                    .PersonagensNaArea(AreasDoMundo.Cidade)
                    .Where(id =>
                        posicoes.TentarObter(id, out var posicao)
                        && AreasDoMundo.DentroDosMuros(posicao)
                    )
                    .ToArray();
                break;

            case CanalDeChat.Sussurro:
                if (!diretorio.TentarObterPorNome(pedido.NomeDoAlvo, out var alvoId))
                {
                    return ResultadoDoChat.Recusar(CodigosErroSocial.AlvoNaoEncontrado);
                }

                if (alvoId == remetenteId)
                {
                    return ResultadoDoChat.Recusar(CodigosErroSocial.SussurroParaSiMesmo);
                }

                if (contatoBarradoEntrePersonagens?.Invoke(remetenteId, alvoId) == true)
                {
                    return ResultadoDoChat.Recusar(CodigosErroSocial.AlvoNaoEncontrado);
                }

                // Offline responde igual a nome inexistente, para não revelar quem existe.
                if (!estaOnline(alvoId) || !diretorio.TentarObterNome(alvoId, out nomeDoAlvo))
                {
                    return ResultadoDoChat.Recusar(CodigosErroSocial.AlvoNaoEncontrado);
                }

                destinatarios = [alvoId, remetenteId];
                break;

            default:
                var membros = grupoDe?.Invoke(remetenteId).Membros ?? [];
                if (membros.All(membro => membro.PersonagemId != remetenteId))
                {
                    return ResultadoDoChat.Recusar(CodigosErroSocial.NaoEstaEmGrupo);
                }

                // Membro offline com a vaga guardada não recebe o que foi dito enquanto estava fora.
                destinatarios = membros
                    .Where(membro => membro.Online || membro.PersonagemId == remetenteId)
                    .Select(membro => membro.PersonagemId)
                    .ToArray();
                break;
        }

        limites.Registrar(remetenteId, pedido.Canal, limite.Texto);
        var mensagem = new MensagemDeChat
        {
            Canal = pedido.Canal,
            RemetenteId = remetenteId,
            NomeDoRemetente = nomeDoRemetente,
            NomeDoAlvo = nomeDoAlvo,
            Texto = limite.Texto,
            HoraUnixMs = relogio.GetUtcNow().ToUnixTimeMilliseconds(),
        };
        return new ResultadoDoChat(mensagem, destinatarios, null);
    }
}
