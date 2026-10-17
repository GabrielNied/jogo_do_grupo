using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Pets;

/// <summary>
/// Registro persistido da coleção de pets por conta (issues #923 e #925).
/// </summary>
public sealed class PetDaConta
{
    public long Id { get; set; }

    /// <summary>Identificador da conta dona do pet.</summary>
    public long ContaId { get; set; }

    /// <summary>Espécie de pet desbloqueada (EspecieDePet).</summary>
    public int Especie { get; set; }

    /// <summary>Data e hora em UTC em que o pet foi desbloqueado.</summary>
    public DateTime DesbloqueadoEm { get; set; }
}

internal sealed class PetDaContaConfiguracao : IEntityTypeConfiguration<PetDaConta>
{
    public void Configure(EntityTypeBuilder<PetDaConta> pet)
    {
        pet.ToTable("PetsDaConta");
        pet.HasKey(p => p.Id);
        pet.HasIndex(p => new { p.ContaId, p.Especie }).IsUnique();
        pet.Property(p => p.DesbloqueadoEm).IsRequired();
    }
}
