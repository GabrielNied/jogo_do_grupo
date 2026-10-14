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
        item.HasIndex(i => new { i.PersonagemId, i.Posicao })
            .IsUnique()
            .HasFilter("\"Posicao\" <> 0");
        // Uma casa guarda um item só. Equipado tem casa nula, que não entra no índice.
        item.HasIndex(i => new
            {
                i.PersonagemId,
                i.Conteiner,
                i.Aba,
                i.Casa,
            })
            .IsUnique()
            .HasFilter("\"Casa\" IS NOT NULL");
        item.Property(i => i.ItemId).IsRequired();
        item.HasOne<PersonagemEntidade>()
            .WithMany()
            .HasForeignKey(i => i.PersonagemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class EstadoEquipamentosConfiguracao : IEntityTypeConfiguration<EstadoEquipamentos>
{
    public void Configure(EntityTypeBuilder<EstadoEquipamentos> estado)
    {
        estado.ToTable("EstadoEquipamentos");
        estado.HasKey(e => e.PersonagemId);
        estado.Property(e => e.PersonagemId).ValueGeneratedNever();
        estado
            .Property(e => e.AbasBolsa)
            .HasDefaultValue(JogoDoGrupo.Shared.Features.Itens.RegrasItens.AbasBolsaInicial);
        estado
            .HasOne<PersonagemEntidade>()
            .WithMany()
            .HasForeignKey(e => e.PersonagemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
