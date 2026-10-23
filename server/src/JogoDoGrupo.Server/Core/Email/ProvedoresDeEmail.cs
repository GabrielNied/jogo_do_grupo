using System.Net.Http.Json;

namespace JogoDoGrupo.Server.Core.Email;

/// <summary>
/// Seção <c>Email</c> da configuração. A chave da API nunca fica no appsettings commitado: em
/// produção vem da variável de ambiente <c>Email__ChaveApiBrevo</c> (ADR 0058).
/// </summary>
public sealed class ConfiguracaoEmail
{
    public const string Secao = "Email";

    /// <summary>Chave da API do Brevo. Vazia: o servidor não envia e só registra no log.</summary>
    public string ChaveApiBrevo { get; set; } = "";

    /// <summary>Remetente verificado no Brevo (um e-mail do grupo; não exige domínio próprio).</summary>
    public string Remetente { get; set; } = "";

    public string NomeRemetente { get; set; } = "Jogo do Grupo";
}

/// <summary>Envio pela API transacional do Brevo (plano gratuito, 300 e-mails por dia).</summary>
internal sealed class ProvedorEmailBrevo(HttpClient http, ConfiguracaoEmail configuracao)
    : IProvedorDeEmail
{
    public const string Endereco = "https://api.brevo.com/v3/smtp/email";

    public async Task EnviarAsync(MensagemEmail mensagem, CancellationToken cancelamento)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Post, Endereco)
        {
            Content = JsonContent.Create(
                new
                {
                    sender = new
                    {
                        name = configuracao.NomeRemetente,
                        email = configuracao.Remetente,
                    },
                    to = new[] { new { email = mensagem.Para } },
                    subject = mensagem.Assunto,
                    textContent = mensagem.Texto,
                }
            ),
        };
        pedido.Headers.Add("api-key", configuracao.ChaveApiBrevo);
        pedido.Headers.Add("accept", "application/json");

        using var resposta = await http.SendAsync(pedido, cancelamento);
        if (!resposta.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Brevo respondeu {(int)resposta.StatusCode} ao enviar e-mail.",
                null,
                resposta.StatusCode
            );
        }
    }
}

/// <summary>
/// Sem chave configurada: não envia. Em Development registra o texto no log (com o código),
/// para testar a recuperação sem serviço externo; fora dele, só avisa que não enviou.
/// </summary>
internal sealed class ProvedorEmailSoLog(IHostEnvironment ambiente, ILogger<ProvedorEmailSoLog> log)
    : IProvedorDeEmail
{
    public Task EnviarAsync(MensagemEmail mensagem, CancellationToken cancelamento)
    {
        if (ambiente.IsDevelopment())
        {
            log.LogInformation(
                "E-mail não enviado (sem Email:ChaveApiBrevo). {Assunto}: {Texto}",
                mensagem.Assunto,
                mensagem.Texto
            );
        }
        else
        {
            log.LogWarning(
                "E-mail não enviado: defina Email__ChaveApiBrevo e Email__Remetente ({Assunto}).",
                mensagem.Assunto
            );
        }

        return Task.CompletedTask;
    }
}

internal static class EmailServicos
{
    /// <summary>Registra a fila, o envio em segundo plano e o provedor (Brevo, se houver chave).</summary>
    public static IServiceCollection AdicionarEmail(
        this IServiceCollection servicos,
        IConfiguration configuracao
    )
    {
        var email =
            configuracao.GetSection(ConfiguracaoEmail.Secao).Get<ConfiguracaoEmail>()
            ?? new ConfiguracaoEmail();
        servicos.AddSingleton(email);
        servicos.AddSingleton<FilaDeEmails>();
        servicos.AddHostedService<EnvioDeEmails>();

        if (string.IsNullOrWhiteSpace(email.ChaveApiBrevo))
        {
            servicos.AddSingleton<IProvedorDeEmail, ProvedorEmailSoLog>();
            return servicos;
        }

        if (string.IsNullOrWhiteSpace(email.Remetente))
        {
            throw new InvalidOperationException(
                "Email:ChaveApiBrevo definida sem Email:Remetente (o e-mail verificado no Brevo)."
            );
        }

        servicos.AddHttpClient<IProvedorDeEmail, ProvedorEmailBrevo>(http =>
            http.Timeout = TimeSpan.FromSeconds(10)
        );
        return servicos;
    }
}
