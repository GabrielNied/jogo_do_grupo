using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Instancias;

/// <summary>
/// Registra a conclusão e saque de loot de uma etapa de raid na semana atual (docs/features/instancias.md).
/// </summary>
public class BloqueioSemanalRaid
{
    public long PersonagemId { get; set; }
    public string ChaveSemana { get; set; } = string.Empty;
    public int Etapa { get; set; }
    public DateTimeOffset ConcluidoEm { get; set; }
}

internal sealed class BloqueioSemanalRaidConfiguracao
    : IEntityTypeConfiguration<BloqueioSemanalRaid>
{
    public void Configure(EntityTypeBuilder<BloqueioSemanalRaid> builder)
    {
        builder.ToTable("BloqueiosSemanaisRaid");
        builder.HasKey(b => new
        {
            b.PersonagemId,
            b.ChaveSemana,
            b.Etapa,
        });
        builder.Property(b => b.ChaveSemana).HasMaxLength(32).IsRequired();
    }
}
