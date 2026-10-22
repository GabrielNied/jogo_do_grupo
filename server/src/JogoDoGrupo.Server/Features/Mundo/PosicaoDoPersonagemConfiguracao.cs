using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Mundo;

internal sealed class PosicaoDoPersonagemConfiguracao
    : IEntityTypeConfiguration<PosicaoDoPersonagem>
{
    public void Configure(EntityTypeBuilder<PosicaoDoPersonagem> builder)
    {
        builder.ToTable("PosicoesDosPersonagens");
        builder.HasKey(p => p.PersonagemId);
        builder.Property(p => p.PersonagemId).ValueGeneratedNever();
        builder.Property(p => p.Area).HasMaxLength(64).IsRequired();
        builder.Property(p => p.X).IsRequired();
        builder.Property(p => p.Y).IsRequired();
        builder.Property(p => p.Z).IsRequired();
        builder.Property(p => p.RotacaoY).IsRequired();
        builder.Property(p => p.AtualizadoEm).IsRequired();
    }
}
