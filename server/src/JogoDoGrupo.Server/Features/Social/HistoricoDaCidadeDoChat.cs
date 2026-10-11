using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// As últimas mensagens do canal Cidade, para quem entra na cidade ver o que estava sendo dito.
/// Fica só na memória (reiniciar o servidor limpa); grupo e sussurro não têm histórico. Os
/// limites (quantas mensagens e de que idade) vêm de <see cref="RegrasChat"/>. Pode ser chamado
/// de qualquer thread.
/// </summary>
public sealed class HistoricoDaCidadeDoChat(TimeProvider relogio)
{
    private readonly Lock _trava = new();
    private readonly Queue<MensagemDeChat> _mensagens = new();

    /// <summary>Guarda uma mensagem aceita do canal Cidade; a mais antiga sai se passar do limite.</summary>
    public void Guardar(MensagemDeChat mensagem)
    {
        lock (_trava)
        {
            _mensagens.Enqueue(mensagem);
            while (_mensagens.Count > RegrasChat.HistoricoDaCidadeMensagens)
            {
                _mensagens.Dequeue();
            }
        }
    }

    /// <summary>
    /// O que entregar agora, da mais antiga para a mais nova, já sem as mensagens velhas demais.
    /// </summary>
    public MensagemDeChat[] Atual()
    {
        var agoraMs = relogio.GetUtcNow().ToUnixTimeMilliseconds();
        lock (_trava)
        {
            while (
                _mensagens.TryPeek(out var antiga)
                && RegrasChat.VelhaDemaisParaOHistorico((agoraMs - antiga.HoraUnixMs) / 1000.0)
            )
            {
                _mensagens.Dequeue();
            }

            return _mensagens.ToArray();
        }
    }
}
