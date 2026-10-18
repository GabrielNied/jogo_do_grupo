namespace JogoDoGrupo.Server.Core.Moderacao;

/// <summary>
/// Registro persistente de denúncia de jogador (#187).
/// </summary>
public sealed class Denuncia
{
    public long Id { get; set; }

    public long AutorContaId { get; set; }

    public long AutorPersonagemId { get; set; }

    public long? AlvoContaId { get; set; }

    public long? AlvoPersonagemId { get; set; }

    public string AlvoNome { get; set; } = "";

    public string Motivo { get; set; } = "";

    public string Detalhes { get; set; } = "";

    public string MetadadosContexto { get; set; } = "";

    public DateTime CriadaEm { get; set; }
}
