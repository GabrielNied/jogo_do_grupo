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
        personagem.Property(p => p.TomDePele).HasDefaultValue(0);
        personagem.Property(p => p.Cabelo).HasDefaultValue(0);
        personagem.Property(p => p.CorDeCabelo).HasDefaultValue(0);
        personagem.Property(p => p.CriadoEm).IsRequired();
    }
}

internal sealed class NomeReservadoConfiguracao : IEntityTypeConfiguration<NomeReservado>
{
    public void Configure(EntityTypeBuilder<NomeReservado> reserva)
    {
        reserva.ToTable("NomesReservados");
        reserva.HasKey(n => n.Id);
        reserva.HasIndex(n => n.NomeNormalizado);
        reserva.HasIndex(n => n.ContaId);
        reserva.Property(n => n.Nome).HasMaxLength(RegrasPersonagem.NomeMaximo).IsRequired();
        reserva
            .Property(n => n.NomeNormalizado)
            .HasMaxLength(RegrasPersonagem.NomeMaximo)
            .IsRequired();
        reserva.Property(n => n.ExcluidoEm).IsRequired();
        reserva.Property(n => n.ReservadoAte).IsRequired();
    }
}
