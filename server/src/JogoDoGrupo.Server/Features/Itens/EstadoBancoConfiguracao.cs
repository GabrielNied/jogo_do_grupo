using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Itens;

internal sealed class EstadoBancoConfiguracao : IEntityTypeConfiguration<EstadoBanco>
{
    public void Configure(EntityTypeBuilder<EstadoBanco> estado)
    {
        estado.ToTable("EstadoBanco");
        estado.HasKey(e => e.ContaId);
        estado.Property(e => e.ContaId).ValueGeneratedNever();
        estado.Property(e => e.Abas).HasDefaultValue(RegrasBanco.AbasBancoInicial);
    }
}
