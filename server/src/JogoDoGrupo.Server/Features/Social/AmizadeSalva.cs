namespace JogoDoGrupo.Server.Features.Social;

/// <summary>Linha de amizade persistida (uma direção por linha; o par tem duas).</summary>
public sealed class AmizadeSalva
{
    public long Id { get; set; }

    public long ContaId { get; set; }

    public long AmigoContaId { get; set; }

    /// <summary>Nome do personagem pelo qual a amizade foi feita (lado desta conta).</summary>
    public string NomeReferencia { get; set; } = "";
}
