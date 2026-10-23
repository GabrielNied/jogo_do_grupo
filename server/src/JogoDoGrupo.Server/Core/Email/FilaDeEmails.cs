using System.Threading.Channels;

namespace JogoDoGrupo.Server.Core.Email;

/// <summary>
/// Fila de e-mails a enviar em segundo plano: a rota responde sem esperar o provedor, e o tempo
/// de resposta não revela se o e-mail existe. Cheia, descarta o mais novo e avisa no log.
/// </summary>
public sealed class FilaDeEmails(ILogger<FilaDeEmails> log)
{
    public const int Capacidade = 256;

    private readonly Channel<MensagemEmail> _canal = Channel.CreateBounded<MensagemEmail>(
        new BoundedChannelOptions(Capacidade)
        {
            // Com Wait, TryWrite devolve false quando cheia (não bloqueia).
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
        }
    );

    /// <summary>Põe na fila sem bloquear. <c>false</c> se a fila estava cheia.</summary>
    public bool Enfileirar(MensagemEmail mensagem)
    {
        if (_canal.Writer.TryWrite(mensagem))
        {
            return true;
        }

        log.LogWarning("Fila de e-mails cheia; um e-mail foi descartado.");
        return false;
    }

    internal ChannelReader<MensagemEmail> Leitor => _canal.Reader;
}

/// <summary>Esvazia a <see cref="FilaDeEmails"/> pelo <see cref="IProvedorDeEmail"/>.</summary>
internal sealed class EnvioDeEmails(
    FilaDeEmails fila,
    IProvedorDeEmail provedor,
    ILogger<EnvioDeEmails> log
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        await foreach (var mensagem in fila.Leitor.ReadAllAsync(parada))
        {
            try
            {
                await provedor.EnviarAsync(mensagem, parada);
            }
            catch (OperationCanceledException) when (parada.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                // Sem o texto: ele pode ter código de recuperação.
                log.LogError(e, "Falha ao enviar e-mail ({Assunto}).", mensagem.Assunto);
            }
        }
    }
}
