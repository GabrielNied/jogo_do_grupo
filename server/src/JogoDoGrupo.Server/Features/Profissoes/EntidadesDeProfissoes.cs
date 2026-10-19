using JogoDoGrupo.Shared.Features.Profissoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonagemEntidade = JogoDoGrupo.Server.Features.Personagem.Personagem;

namespace JogoDoGrupo.Server.Features.Profissoes;

/// <summary>Uma profissão que o personagem aprendeu (todas podem ser aprendidas).</summary>
public sealed class ProfissaoAprendida
{
    public long PersonagemId { get; set; }
    public Profissao Profissao { get; set; }
}

internal sealed class ProfissaoAprendidaConfiguracao : IEntityTypeConfiguration<ProfissaoAprendida>
{
    public void Configure(EntityTypeBuilder<ProfissaoAprendida> aprendida)
    {
        aprendida.ToTable("ProfissoesAprendidas");
        aprendida.HasKey(a => new { a.PersonagemId, a.Profissao });
        aprendida
            .HasOne<PersonagemEntidade>()
            .WithMany()
            .HasForeignKey(a => a.PersonagemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// O saldo de um material de profissão. Fica fora da bolsa (a bolsa é 1x1 e não empilha), no
/// mesmo modelo dos materiais de fusão: um inteiro por personagem e material, alterado só em
/// transação.
/// </summary>
public sealed class SaldoDeMaterialDeProfissao
{
    public long PersonagemId { get; set; }
    public int MaterialId { get; set; }
    public int Quantidade { get; set; }
}

internal sealed class SaldoDeMaterialDeProfissaoConfiguracao
    : IEntityTypeConfiguration<SaldoDeMaterialDeProfissao>
{
    public void Configure(EntityTypeBuilder<SaldoDeMaterialDeProfissao> saldo)
    {
        saldo.ToTable(
            "SaldosMateriaisProfissao",
            t =>
                t.HasCheckConstraint(
                    "CK_SaldosMateriaisProfissao_Quantidade",
                    "\"Quantidade\" >= 0"
                )
        );
        saldo.HasKey(s => new { s.PersonagemId, s.MaterialId });
        saldo
            .HasOne<PersonagemEntidade>()
            .WithMany()
            .HasForeignKey(s => s.PersonagemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
