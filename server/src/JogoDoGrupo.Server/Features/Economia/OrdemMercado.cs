using JogoDoGrupo.Shared.Features.Economia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Economia;

/// <summary>
/// Uma ordem do livro do mercado (#176). Venda: um exemplar guardado na ordem
/// (<see cref="ItemEmCustodia"/>, com a cópia em <see cref="EquipamentoJson"/>). Compra: um
/// modelo, as unidades que faltam e o preço máximo; as coroas guardadas são sempre
/// <c>Quantidade * PrecoUnitario</c> enquanto a ordem está aberta.
/// </summary>
public sealed class OrdemMercado
{
    public long Id { get; set; }
    public long PersonagemId { get; set; }
    public bool Venda { get; set; }
    public int ItemId { get; set; }

    /// <summary>Unidades que faltam (sempre 1 na venda aberta, 0 na cumprida).</summary>
    public int Quantidade { get; set; }

    public long PrecoUnitario { get; set; }
    public EstadoDaOrdemDeMercado Estado { get; set; }

    /// <summary>Segundos Unix (UTC).</summary>
    public long CriadaEm { get; set; }

    /// <summary>Segundos Unix (UTC).</summary>
    public long ExpiraEm { get; set; }

    public long? EncerradaEm { get; set; }

    /// <summary>
    /// Venda cujo exemplar ainda está guardado: aberta, ou cancelada e vencida que aguarda a
    /// devolução à bolsa.
    /// </summary>
    public bool ItemEmCustodia { get; set; }

    /// <summary>A cópia opaca do exemplar guardado (nula nos itens sem equipamento).</summary>
    public string? EquipamentoJson { get; set; }
}

internal sealed class OrdemMercadoConfiguracao : IEntityTypeConfiguration<OrdemMercado>
{
    public void Configure(EntityTypeBuilder<OrdemMercado> ordem)
    {
        ordem.ToTable(
            "OrdensMercado",
            t => t.HasCheckConstraint("CK_OrdensMercado_PrecoUnitario", "\"PrecoUnitario\" > 0")
        );
        ordem.HasKey(o => o.Id);
        ordem.HasIndex(o => new
        {
            o.ItemId,
            o.Venda,
            o.Estado,
            o.PrecoUnitario,
        });
        ordem.HasIndex(o => new { o.PersonagemId, o.Estado });
        ordem.HasIndex(o => new { o.Estado, o.ExpiraEm });
        // Relacionamento de persistência, sem uso das regras de Personagem.
        ordem
            .HasOne<JogoDoGrupo.Server.Features.Personagem.Personagem>()
            .WithMany()
            .HasForeignKey(o => o.PersonagemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
