using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Core.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JogoDoGrupo.Server.Features.Economia;

public sealed class SaldoCoroas
{
    public long PersonagemId { get; set; }
    public long Saldo { get; set; }
}

internal sealed class SaldoCoroasConfiguracao : IEntityTypeConfiguration<SaldoCoroas>
{
    public void Configure(EntityTypeBuilder<SaldoCoroas> saldo)
    {
        saldo.ToTable(
            "SaldosCoroas",
            t => t.HasCheckConstraint("CK_SaldosCoroas_Saldo", "\"Saldo\" >= 0")
        );
        saldo.HasKey(s => s.PersonagemId);
        saldo.Property(s => s.PersonagemId).ValueGeneratedNever();
        // Relacionamento de persistência, sem uso das regras de Personagem.
        saldo
            .HasOne<JogoDoGrupo.Server.Features.Personagem.Personagem>()
            .WithMany()
            .HasForeignKey(s => s.PersonagemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CarteiraCoroas(JogoDbContext db) : ICarteiraCoroas
{
    public async Task<long> SaldoAsync(long personagemId, CancellationToken c) =>
        await db.Set<SaldoCoroas>()
            .Where(s => s.PersonagemId == personagemId)
            .Select(s => s.Saldo)
            .SingleOrDefaultAsync(c);

    public async Task<bool> DebitarAsync(long personagemId, long valor, CancellationToken c)
    {
        if (valor <= 0)
            throw new ArgumentOutOfRangeException(nameof(valor));
        ExigirTransacao();
        return await db.Set<SaldoCoroas>()
                .Where(s => s.PersonagemId == personagemId && s.Saldo >= valor)
                .ExecuteUpdateAsync(p => p.SetProperty(s => s.Saldo, s => s.Saldo - valor), c) == 1;
    }

    public async Task CreditarAsync(long personagemId, long valor, CancellationToken c)
    {
        if (valor <= 0)
            throw new ArgumentOutOfRangeException(nameof(valor));
        ExigirTransacao();
        var saldo = await db.Set<SaldoCoroas>().FindAsync(new object[] { personagemId }, c);
        if (saldo == null)
            db.Add(new SaldoCoroas { PersonagemId = personagemId, Saldo = valor });
        else
            saldo.Saldo = checked(saldo.Saldo + valor);
        await db.SaveChangesAsync(c);
    }

    private void ExigirTransacao()
    {
        if (db.Database.CurrentTransaction == null)
            throw new InvalidOperationException("A carteira exige transação do chamador.");
    }
}
