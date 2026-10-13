using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Talentos;

/// <summary>
/// Cabeçalho da árvore de um personagem: a versão da configuração em que ele comprou e quantos
/// pontos pagou. O saldo livre continua em Personagem. Criada na primeira compra.
/// </summary>
public sealed class ArvoreDoPersonagem
{
    public long PersonagemId { get; set; }

    /// <summary>Versão da configuração em que os ranks foram comprados.</summary>
    public int Versao { get; set; }

    /// <summary>Soma do que os ranks custaram (o que uma redistribuição devolveria).</summary>
    public int PontosGastos { get; set; }
}

/// <summary>Rank de um nó comprado por um personagem. Só existe com rank maior que zero.</summary>
public sealed class NoComprado
{
    public long PersonagemId { get; set; }

    public string NoId { get; set; } = "";

    public int Rank { get; set; }
}

internal sealed class ArvoreDoPersonagemConfiguracao : IEntityTypeConfiguration<ArvoreDoPersonagem>
{
    public void Configure(EntityTypeBuilder<ArvoreDoPersonagem> arvore)
    {
        arvore.ToTable("TalentosArvores");
        arvore.HasKey(a => a.PersonagemId);
        arvore.Property(a => a.PersonagemId).ValueGeneratedNever();
    }
}

internal sealed class NoCompradoConfiguracao : IEntityTypeConfiguration<NoComprado>
{
    public const int IdMaximo = 64;

    public void Configure(EntityTypeBuilder<NoComprado> no)
    {
        no.ToTable("TalentosNos");
        no.HasKey(n => new { n.PersonagemId, n.NoId });
        no.Property(n => n.NoId).HasMaxLength(IdMaximo).IsRequired();
    }
}
