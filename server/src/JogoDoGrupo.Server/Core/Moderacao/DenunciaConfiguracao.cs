using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Core.Moderacao;

internal sealed class DenunciaConfiguracao : IEntityTypeConfiguration<Denuncia>
{
    public void Configure(EntityTypeBuilder<Denuncia> builder)
    {
        builder.ToTable("Denuncias");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.AutorContaId).IsRequired();
        builder.Property(d => d.AutorPersonagemId).IsRequired();
        builder.Property(d => d.AlvoNome).HasMaxLength(32).IsRequired();
        builder.Property(d => d.Motivo).HasMaxLength(64).IsRequired();
        builder.Property(d => d.Detalhes).HasMaxLength(500);
        builder.Property(d => d.MetadadosContexto).HasMaxLength(1000);
        builder.Property(d => d.CriadaEm).IsRequired();

        builder.HasIndex(d => d.AutorContaId);
        builder.HasIndex(d => d.AlvoPersonagemId);
        builder.HasIndex(d => d.CriadaEm);
    }
}
