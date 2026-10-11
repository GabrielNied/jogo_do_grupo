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
/// campo do client) e devolve o que entregar. O canal Grupo ainda responde
/// <c>NaoEstaEmGrupo</c> para todos: ele entra quando o servidor do grupo existir.
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
    public ResultadoDoChat Processar(
        long remetenteId,
        EnviarMensagemDeChat pedido,
        Func<long, bool> estaOnline
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
                destinatarios = posicoes.PersonagensNaArea(AreasDoMundo.Cidade);
                if (!destinatarios.Contains(remetenteId))
                {
                    return ResultadoDoChat.Recusar(CodigosErroSocial.CanalIndisponivel);
                }

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

                // Offline responde igual a nome inexistente, para não revelar quem existe.
                if (!estaOnline(alvoId) || !diretorio.TentarObterNome(alvoId, out nomeDoAlvo))
                {
                    return ResultadoDoChat.Recusar(CodigosErroSocial.AlvoNaoEncontrado);
                }

                destinatarios = [alvoId, remetenteId];
                break;

            default:
                return ResultadoDoChat.Recusar(CodigosErroSocial.NaoEstaEmGrupo);
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
