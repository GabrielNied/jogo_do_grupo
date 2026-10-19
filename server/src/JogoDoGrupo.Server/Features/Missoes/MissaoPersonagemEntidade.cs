using JogoDoGrupo.Shared.Features.Missoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Missoes;

/// <summary>
/// Persistência do progresso e estado da missão de um personagem (#179, #1061).
/// </summary>
public sealed class MissaoPersonagemEntidade
{
    public long Id { get; set; }
    public long PersonagemId { get; set; }
    public int MissaoId { get; set; }
    public EstadoDaMissao Estado { get; set; }
    public bool Acompanhada { get; set; }
    public string ObjetivosJson { get; set; } = "[]";
    public long AceitaEm { get; set; }
    public long? ConcluidaEm { get; set; }
}

internal sealed class MissaoPersonagemConfiguracao
    : IEntityTypeConfiguration<MissaoPersonagemEntidade>
{
    public void Configure(EntityTypeBuilder<MissaoPersonagemEntidade> builder)
    {
        builder.ToTable("MissoesPersonagem");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.PersonagemId, m.MissaoId }).IsUnique();
        builder.HasIndex(m => new { m.PersonagemId, m.Estado });
        builder
            .HasOne<JogoDoGrupo.Server.Features.Personagem.Personagem>()
            .WithMany()
            .HasForeignKey(m => m.PersonagemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
