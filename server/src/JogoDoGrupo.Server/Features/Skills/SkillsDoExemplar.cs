using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Skills;

/// <summary>
/// As skills sorteadas de um exemplar de arma (SKILL-004): fixas até o exemplar deixar de existir.
/// Ficam aqui, fora do <c>EquipamentoJson</c> de Itens, e a chave é o id do exemplar, que a fusão
/// mantém no item principal: por isso a fusão preserva as skills (DEC-003).
/// </summary>
public sealed class SkillsDoExemplar
{
    /// <summary>Id do exemplar da arma (<c>ItensInventario.Id</c>); também é a chave.</summary>
    public long InstanciaId { get; set; }

    /// <summary>Modelo-base do exemplar quando sorteou; se mudar, o sorteio vale de novo.</summary>
    public int ModeloId { get; set; }

    /// <summary>Ids das skills, separados por vírgula, na ordem dos espaços da barra.</summary>
    public string SkillIds { get; set; } = "";
}

internal sealed class SkillsDoExemplarConfiguracao : IEntityTypeConfiguration<SkillsDoExemplar>
{
    public const int IdsMaximo = 256;

    public void Configure(EntityTypeBuilder<SkillsDoExemplar> skills)
    {
        skills.ToTable("SkillsDoExemplar");
        skills.HasKey(s => s.InstanciaId);
        skills.Property(s => s.InstanciaId).ValueGeneratedNever();
        skills.Property(s => s.SkillIds).HasMaxLength(IdsMaximo).IsRequired();
    }
}
