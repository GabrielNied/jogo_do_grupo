using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Economia;

/// <summary>
/// Item armazenado no cofre compartilhado da guilda (#338).
/// </summary>
public sealed class ItemBancoGuildaEntidade
{
    public long Id { get; set; }
    public long GuildaId { get; set; }
    public int Aba { get; set; }
    public int Casa { get; set; }
    public int ItemId { get; set; }
    public string? EquipamentoJson { get; set; }
    public string DepositadoPorNome { get; set; } = "";
    public long DataDepositoUnix { get; set; }
}

/// <summary>
/// Saldo de coroas depositado no cofre compartilhado da guilda (#338).
/// </summary>
public sealed class SaldoBancoGuildaEntidade
{
    public long GuildaId { get; set; }
    public long Coroas { get; set; }
}

internal sealed class ItemBancoGuildaConfiguracao
    : IEntityTypeConfiguration<ItemBancoGuildaEntidade>
{
    public void Configure(EntityTypeBuilder<ItemBancoGuildaEntidade> builder)
    {
        builder.ToTable("ItensBancoGuilda");
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.GuildaId);
        builder
            .HasIndex(i => new
            {
                i.GuildaId,
                i.Aba,
                i.Casa,
            })
            .IsUnique();
    }
}

internal sealed class SaldoBancoGuildaConfiguracao
    : IEntityTypeConfiguration<SaldoBancoGuildaEntidade>
{
    public void Configure(EntityTypeBuilder<SaldoBancoGuildaEntidade> builder)
    {
        builder.ToTable("SaldosBancoGuilda");
        builder.HasKey(s => s.GuildaId);
    }
}
