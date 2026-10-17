using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Montarias;

/// <summary>
/// Registro persistido da coleção de montarias por conta (issue #928).
/// </summary>
public sealed class MontariaDaConta
{
    public long Id { get; set; }

    /// <summary>Identificador da conta que possui a montaria.</summary>
    public long ContaId { get; set; }

    /// <summary>Tipo da montaria desbloqueada (RegrasMontaria).</summary>
    public int TipoMontaria { get; set; }

    /// <summary>Data e hora em UTC em que a montaria foi desbloqueada.</summary>
    public DateTime DesbloqueadaEm { get; set; }
}

internal sealed class MontariaDaContaConfiguracao : IEntityTypeConfiguration<MontariaDaConta>
{
    public void Configure(EntityTypeBuilder<MontariaDaConta> montaria)
    {
        montaria.ToTable("MontariasDaConta");
        montaria.HasKey(m => m.Id);
        montaria.HasIndex(m => new { m.ContaId, m.TipoMontaria }).IsUnique();
        montaria.Property(m => m.DesbloqueadaEm).IsRequired();
    }
}
