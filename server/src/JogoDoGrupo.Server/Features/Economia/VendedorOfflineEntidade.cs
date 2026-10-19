using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Economia;

/// <summary>
/// Persistência do manequim vendedor offline (#905). Permanece na praça com a pessoa deslogada
/// até expirar o período pago de diárias.
/// </summary>
public sealed class VendedorOfflineEntidade
{
    public long PersonagemId { get; set; }
    public string NomeDono { get; set; } = "";
    public string Titulo { get; set; } = "";
    public float PosicaoX { get; set; }
    public float PosicaoZ { get; set; }
    public long ContratadoEm { get; set; }
    public long ExpiraEm { get; set; }
    public int DiariasPagas { get; set; }
    public bool Ativo { get; set; }
}

internal sealed class VendedorOfflineConfiguracao
    : IEntityTypeConfiguration<VendedorOfflineEntidade>
{
    public void Configure(EntityTypeBuilder<VendedorOfflineEntidade> builder)
    {
        builder.ToTable("VendedoresOffline");
        builder.HasKey(v => v.PersonagemId);
        builder.HasIndex(v => new { v.Ativo, v.ExpiraEm });
    }
}
