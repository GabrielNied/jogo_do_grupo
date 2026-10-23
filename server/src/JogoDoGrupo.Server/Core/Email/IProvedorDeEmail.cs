namespace JogoDoGrupo.Server.Core.Email;

/// <summary>E-mail de texto simples para um destinatário.</summary>
public sealed record MensagemEmail(string Para, string Assunto, string Texto);

/// <summary>
/// Envia e-mail por um serviço externo (ADR 0058). Trocar de provedor é trocar a implementação
/// registrada; os testes registram um falso. Ninguém chama direto: use <see cref="FilaDeEmails"/>.
/// </summary>
public interface IProvedorDeEmail
{
    /// <summary>Envia ou lança exceção; quem chama registra a falha no log.</summary>
    Task EnviarAsync(MensagemEmail mensagem, CancellationToken cancelamento);
}
