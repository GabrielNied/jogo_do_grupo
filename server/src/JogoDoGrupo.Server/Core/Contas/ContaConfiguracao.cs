using JogoDoGrupo.Shared.Core.Contas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Core.Contas;

internal sealed class ContaConfiguracao : IEntityTypeConfiguration<Conta>
{
    public void Configure(EntityTypeBuilder<Conta> conta)
    {
        conta.ToTable("Contas");
        conta.HasKey(c => c.Id);
        conta.Property(c => c.Usuario).HasMaxLength(RegrasConta.UsuarioMaximo).IsRequired();
        conta
            .Property(c => c.UsuarioNormalizado)
            .HasMaxLength(RegrasConta.UsuarioMaximo)
            .IsRequired();
        conta.HasIndex(c => c.UsuarioNormalizado).IsUnique();
        conta.Property(c => c.SenhaHash).IsRequired();
        conta.Property(c => c.CriadaEm).IsRequired();
        conta.Property(c => c.CarimboSeguranca).HasMaxLength(64).IsRequired().HasDefaultValue("");
        conta.Property(c => c.EhGm).IsRequired().HasDefaultValue(false);
        conta.Property(c => c.BanidaAte);
        conta.Property(c => c.MotivoBanimento).HasMaxLength(256);
    }
}
