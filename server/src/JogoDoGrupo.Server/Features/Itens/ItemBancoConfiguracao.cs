using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Itens;

internal sealed class ItemBancoConfiguracao : IEntityTypeConfiguration<ItemBanco>
{
    public void Configure(EntityTypeBuilder<ItemBanco> item)
    {
        item.ToTable("ItensBanco");
        item.HasKey(i => i.Id);
        item.HasIndex(i => i.ContaId);
        item.HasIndex(i => new
            {
                i.ContaId,
                i.Aba,
                i.Casa,
            })
            .IsUnique()
            .HasFilter("\"Casa\" IS NOT NULL");
        item.Property(i => i.ItemId).IsRequired();
    }
}
