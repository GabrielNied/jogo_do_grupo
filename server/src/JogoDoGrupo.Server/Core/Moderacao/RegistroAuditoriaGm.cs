namespace JogoDoGrupo.Server.Core.Moderacao;

/// <summary>
/// Registro em banco de auditoria para cada ação executada por GM (#187).
/// </summary>
public sealed class RegistroAuditoriaGm
{
    public long Id { get; set; }

    public long GmContaId { get; set; }

    public long GmPersonagemId { get; set; }

    public string Acao { get; set; } = "";

    public string Alvo { get; set; } = "";

    public string Detalhes { get; set; } = "";

    public DateTime ExecutadaEm { get; set; }
}
