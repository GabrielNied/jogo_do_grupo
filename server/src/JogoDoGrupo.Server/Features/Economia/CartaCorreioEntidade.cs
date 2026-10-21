using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Economia;

/// <summary>
/// Uma carta enviada por correio a um personagem (#212).
/// Pode conter texto, coroas e múltiplos itens anexados em custódia.
/// </summary>
public sealed class CartaCorreio
{
    public long Id { get; set; }

    /// <summary>Id do personagem destinatário.</summary>
    public long DestinatarioId { get; set; }

    /// <summary>Id do personagem remetente (nulo se for carta gerada pelo sistema/leilão).</summary>
    public long? RemetenteId { get; set; }

    /// <summary>Nome do remetente exibido para o jogador.</summary>
    public string RemetenteNome { get; set; } = "";

    /// <summary>Assunto da carta.</summary>
    public string Assunto { get; set; } = "";

    /// <summary>Corpo da mensagem.</summary>
    public string Mensagem { get; set; } = "";

    /// <summary>Quantidade de coroas anexadas à carta.</summary>
    public long Coroas { get; set; }

    /// <summary>Indica se o destinatário já abriu e leu a carta.</summary>
    public bool Lida { get; set; }

    /// <summary>Indica se os anexos (coroas e itens) já foram retirados.</summary>
    public bool AnexosColetados { get; set; }

    /// <summary>Indica se a carta foi devolvida ao remetente por expiração.</summary>
    public bool Devolvida { get; set; }

    /// <summary>Verdadeiro se enviada automaticamente pelo sistema/serviço.</summary>
    public bool DoSistema { get; set; }

    /// <summary>Momento em que a carta foi enviada (segundos Unix).</summary>
    public long EnviadaEm { get; set; }

    /// <summary>Momento em que a carta expira caso não seja recolhida (segundos Unix).</summary>
    public long ExpiraEm { get; set; }

    /// <summary>Itens anexados a esta carta.</summary>
    public List<AnexoItemCorreioEntidade> Itens { get; set; } = [];
}

/// <summary>
/// Um item guardado em custódia como anexo de uma carta de correio.
/// </summary>
public sealed class AnexoItemCorreioEntidade
{
    public long Id { get; set; }

    public long CartaId { get; set; }

    public CartaCorreio? Carta { get; set; }

    public int ItemId { get; set; }

    /// <summary>JSON opaco dos atributos do equipamento, conforme custódia de itens.</summary>
    public string? EquipamentoJson { get; set; }
}

public sealed class CartaCorreioConfiguracao : IEntityTypeConfiguration<CartaCorreio>
{
    public void Configure(EntityTypeBuilder<CartaCorreio> builder)
    {
        builder.ToTable("CartasCorreio");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.DestinatarioId);
        builder.HasIndex(c => new { c.DestinatarioId, c.Lida });
        builder.HasIndex(c => c.ExpiraEm);

        builder.Property(c => c.RemetenteNome).HasMaxLength(40).IsRequired();
        builder.Property(c => c.Assunto).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Mensagem).HasMaxLength(1500).IsRequired();

        builder
            .HasMany(c => c.Itens)
            .WithOne(i => i.Carta!)
            .HasForeignKey(i => i.CartaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AnexoItemCorreioConfiguracao
    : IEntityTypeConfiguration<AnexoItemCorreioEntidade>
{
    public void Configure(EntityTypeBuilder<AnexoItemCorreioEntidade> builder)
    {
        builder.ToTable("AnexosItensCorreio");
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.CartaId);
    }
}
