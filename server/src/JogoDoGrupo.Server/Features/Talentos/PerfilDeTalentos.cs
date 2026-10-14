using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Talentos;

/// <summary>
/// Perfil de build de talentos salvo pelo jogador para troca e experimentação de combinações.
/// Guarda um snapshot dos ranks, a versão da configuração em que foi salvo e a data.
/// </summary>
public sealed class PerfilDeTalentos
{
    public long Id { get; set; }

    public long PersonagemId { get; set; }

    public string Nome { get; set; } = "";

    /// <summary>Versão da configuração em que o perfil foi salvo.</summary>
    public int Versao { get; set; }

    /// <summary>Data e hora em UTC em que o perfil foi criado.</summary>
    public DateTime CriadoEm { get; set; }

    /// <summary>JSON com a lista de ranks do perfil (<c>RankDoNo[]</c>).</summary>
    public string RanksJson { get; set; } = "[]";
}

internal sealed class PerfilDeTalentosConfiguracao : IEntityTypeConfiguration<PerfilDeTalentos>
{
    public const int NomeMaximo = 64;

    public void Configure(EntityTypeBuilder<PerfilDeTalentos> perfil)
    {
        perfil.ToTable("TalentosPerfis");
        perfil.HasKey(p => p.Id);
        perfil.Property(p => p.Nome).HasMaxLength(NomeMaximo).IsRequired();
        perfil.HasIndex(p => p.PersonagemId);
    }
}
