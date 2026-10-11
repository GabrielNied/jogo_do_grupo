using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Social;

internal sealed class AmizadeSalvaConfiguracao : IEntityTypeConfiguration<AmizadeSalva>
{
    public void Configure(EntityTypeBuilder<AmizadeSalva> entidade)
    {
        entidade.ToTable("SocialAmizades");
        entidade.HasKey(a => a.Id);
        entidade.HasIndex(a => new { a.ContaId, a.AmigoContaId }).IsUnique();
        entidade.Property(a => a.NomeReferencia).HasMaxLength(32).IsRequired();
    }
}

internal sealed class PedidoAmizadeSalvoConfiguracao : IEntityTypeConfiguration<PedidoAmizadeSalvo>
{
    public void Configure(EntityTypeBuilder<PedidoAmizadeSalvo> entidade)
    {
        entidade.ToTable("SocialPedidosAmizade");
        entidade.HasKey(p => p.PedidoId);
        entidade.HasIndex(p => new { p.DeContaId, p.ParaContaId }).IsUnique();
        entidade.Property(p => p.NomeAlvo).HasMaxLength(32).IsRequired();
    }
}

internal sealed class IgnoradoSalvoConfiguracao : IEntityTypeConfiguration<IgnoradoSalvo>
{
    public void Configure(EntityTypeBuilder<IgnoradoSalvo> entidade)
    {
        entidade.ToTable("SocialIgnorados");
        entidade.HasKey(i => i.IgnoradoId);
        entidade.HasIndex(i => new { i.ContaId, i.IgnoradoContaId }).IsUnique();
        entidade.Property(i => i.Nome).HasMaxLength(32).IsRequired();
    }
}
