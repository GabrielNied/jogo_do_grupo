using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Personagem;

internal sealed class PersonagemConfiguracao : IEntityTypeConfiguration<Personagem>
{
    public void Configure(EntityTypeBuilder<Personagem> personagem)
    {
        personagem.ToTable("Personagens");
        personagem.HasKey(p => p.Id);
        personagem.HasIndex(p => p.ContaId);
        personagem.Property(p => p.Nome).HasMaxLength(RegrasPersonagem.NomeMaximo).IsRequired();
        personagem
            .Property(p => p.NomeNormalizado)
            .HasMaxLength(RegrasPersonagem.NomeMaximo)
            .IsRequired();
        personagem.HasIndex(p => p.NomeNormalizado).IsUnique();
        personagem.Property(p => p.Raca).IsRequired();
        personagem.Property(p => p.Nivel).IsRequired();
        personagem.Property(p => p.CriadoEm).IsRequired();
    }
}
