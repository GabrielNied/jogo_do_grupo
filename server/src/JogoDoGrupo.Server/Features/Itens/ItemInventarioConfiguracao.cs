using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonagemEntidade = JogoDoGrupo.Server.Features.Personagem.Personagem;

namespace JogoDoGrupo.Server.Features.Itens;

internal sealed class ItemInventarioConfiguracao : IEntityTypeConfiguration<ItemInventario>
{
    public void Configure(EntityTypeBuilder<ItemInventario> item)
    {
        item.ToTable("ItensInventario");
        item.HasKey(i => i.Id);
        item.HasIndex(i => i.PersonagemId);
        item.Property(i => i.ItemId).IsRequired();
        item.HasOne<PersonagemEntidade>()
            .WithMany()
            .HasForeignKey(i => i.PersonagemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
