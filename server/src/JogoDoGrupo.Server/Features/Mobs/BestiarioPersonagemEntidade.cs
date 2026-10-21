using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Registro persistente de mortes e observação de uma criatura no bestiário de um personagem (#922).
/// </summary>
public sealed class BestiarioPersonagemEntidade
{
    public long Id { get; set; }
    public long PersonagemId { get; set; }
    public int CriaturaId { get; set; }
    public int Derrotas { get; set; }
    public long PrimeiroRegistroEm { get; set; }
    public long UltimoRegistroEm { get; set; }
}

internal sealed class BestiarioPersonagemConfiguracao
    : IEntityTypeConfiguration<BestiarioPersonagemEntidade>
{
    public void Configure(EntityTypeBuilder<BestiarioPersonagemEntidade> builder)
    {
        builder.ToTable("BestiarioPersonagem");
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.PersonagemId, b.CriaturaId }).IsUnique();
        builder.HasIndex(b => b.PersonagemId);
        // Relacionamento de persistência, sem uso das regras de Personagem.
        builder
            .HasOne<JogoDoGrupo.Server.Features.Personagem.Personagem>()
            .WithMany()
            .HasForeignKey(b => b.PersonagemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
