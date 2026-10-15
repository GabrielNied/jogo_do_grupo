using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Itens;

public sealed class SaldoMateriaisFusao
{
    public long PersonagemId { get; set; }
    public int EssenciasDungeon { get; set; }
    public int EssenciasRaid { get; set; }
}

internal sealed class SaldoMateriaisFusaoConfiguracao
    : IEntityTypeConfiguration<SaldoMateriaisFusao>
{
    public void Configure(EntityTypeBuilder<SaldoMateriaisFusao> saldo)
    {
        saldo.ToTable(
            "SaldosMateriaisFusao",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_SaldosMateriaisFusao_Dungeon",
                    "\"EssenciasDungeon\" >= 0"
                );
                t.HasCheckConstraint("CK_SaldosMateriaisFusao_Raid", "\"EssenciasRaid\" >= 0");
            }
        );
        saldo.HasKey(s => s.PersonagemId);
        saldo.Property(s => s.PersonagemId).ValueGeneratedNever();
        saldo
            .HasOne<JogoDoGrupo.Server.Features.Personagem.Personagem>()
            .WithMany()
            .HasForeignKey(s => s.PersonagemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SaldosMateriaisFusao(JogoDbContext db) : ISaldosMateriaisFusao
{
    public async Task<(int Dungeon, int Raid)> SaldoAsync(long personagemId, CancellationToken c)
    {
        var linha = await db.Set<SaldoMateriaisFusao>()
            .Where(s => s.PersonagemId == personagemId)
            .Select(s => new { s.EssenciasDungeon, s.EssenciasRaid })
            .SingleOrDefaultAsync(c);
        return linha != null ? (linha.EssenciasDungeon, linha.EssenciasRaid) : (0, 0);
    }

    public async Task<bool> DebitarAsync(
        long personagemId,
        int dungeon,
        int raid,
        CancellationToken c
    )
    {
        if (dungeon < 0 || raid < 0)
            throw new ArgumentOutOfRangeException("Valores de débito não podem ser negativos.");
        if (dungeon == 0 && raid == 0)
            return true;

        ExigirTransacao();
        return await db.Set<SaldoMateriaisFusao>()
                .Where(s =>
                    s.PersonagemId == personagemId
                    && s.EssenciasDungeon >= dungeon
                    && s.EssenciasRaid >= raid
                )
                .ExecuteUpdateAsync(
                    p =>
                        p.SetProperty(s => s.EssenciasDungeon, s => s.EssenciasDungeon - dungeon)
                            .SetProperty(s => s.EssenciasRaid, s => s.EssenciasRaid - raid),
                    c
                ) == 1;
    }

    public async Task CreditarDungeonAsync(long personagemId, int quantidade, CancellationToken c)
    {
        if (quantidade <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidade));
        ExigirTransacao();
        var saldo = await db.Set<SaldoMateriaisFusao>().FindAsync(new object[] { personagemId }, c);
        if (saldo == null)
            db.Add(
                new SaldoMateriaisFusao
                {
                    PersonagemId = personagemId,
                    EssenciasDungeon = quantidade,
                }
            );
        else
            saldo.EssenciasDungeon = checked(saldo.EssenciasDungeon + quantidade);
        await db.SaveChangesAsync(c);
    }

    public async Task CreditarRaidAsync(long personagemId, int quantidade, CancellationToken c)
    {
        if (quantidade <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidade));
        ExigirTransacao();
        var saldo = await db.Set<SaldoMateriaisFusao>().FindAsync(new object[] { personagemId }, c);
        if (saldo == null)
            db.Add(
                new SaldoMateriaisFusao { PersonagemId = personagemId, EssenciasRaid = quantidade }
            );
        else
            saldo.EssenciasRaid = checked(saldo.EssenciasRaid + quantidade);
        await db.SaveChangesAsync(c);
    }

    private void ExigirTransacao()
    {
        if (db.Database.CurrentTransaction == null)
            throw new InvalidOperationException(
                "Os saldos de materiais exigem transação do chamador."
            );
    }
}
