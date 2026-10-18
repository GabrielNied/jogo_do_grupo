using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Core.Moderacao;

internal sealed class RegistroAuditoriaGmConfiguracao
    : IEntityTypeConfiguration<RegistroAuditoriaGm>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoriaGm> builder)
    {
        builder.ToTable("AuditoriaGm");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.GmContaId).IsRequired();
        builder.Property(a => a.GmPersonagemId).IsRequired();
        builder.Property(a => a.Acao).HasMaxLength(64).IsRequired();
        builder.Property(a => a.Alvo).HasMaxLength(64).IsRequired();
        builder.Property(a => a.Detalhes).HasMaxLength(500);
        builder.Property(a => a.ExecutadaEm).IsRequired();

        builder.HasIndex(a => a.GmContaId);
        builder.HasIndex(a => a.ExecutadaEm);
    }
}
