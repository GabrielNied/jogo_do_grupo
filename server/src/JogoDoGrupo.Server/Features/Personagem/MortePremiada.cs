using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Personagem;

/// <summary>
/// Uma morte que já deu experiência. A chave é o inimigo mais a sequência da morte: o mesmo
/// evento de novo não premia duas vezes, e a morte seguinte (mesmo id, outra sequência) premia.
/// </summary>
internal sealed class MortePremiada
{
    public long Id { get; set; }

    public long InimigoId { get; set; }

    public long Sequencia { get; set; }
}

internal sealed class MortePremiadaConfiguracao : IEntityTypeConfiguration<MortePremiada>
{
    public void Configure(EntityTypeBuilder<MortePremiada> morte)
    {
        morte.ToTable("MortesPremiadas");
        morte.HasKey(m => m.Id);
        morte.HasIndex(m => new { m.InimigoId, m.Sequencia }).IsUnique();
    }
}
