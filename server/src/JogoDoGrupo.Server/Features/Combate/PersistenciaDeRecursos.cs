using System.Data;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Combate;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Gerencia a leitura inicial e persistência em banco dos recursos dos personagens (<see cref="RecursosPersonagem"/>).
/// </summary>
internal sealed class PersistenciaDeRecursos
{
    // Mesma espera pela trava do banco da rota de recursos (o client desiste em 10 s).
    public const int EsperaTravaSegundos = 5;

    private readonly IServiceScopeFactory _escopos;

    public PersistenciaDeRecursos(IServiceScopeFactory escopos)
    {
        _escopos = escopos;
    }

    // Uma ida ao banco por personagem: na transação Serializable (BEGIN IMMEDIATE no SQLite),
    // como a rota sempre fez, porque cria a linha quando ela não existe.
    public async Task<RecursosPersonagem> CarregarAsync(
        long personagemId,
        MaximosDeRecursos maximos,
        DateTime agora,
        CancellationToken cancelamento
    )
    {
        using var escopo = _escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();

        var sqlite = db.Database.GetDbConnection() as SqliteConnection;
        var esperaAnterior = sqlite?.DefaultTimeout;
        if (sqlite != null)
        {
            sqlite.DefaultTimeout = EsperaTravaSegundos;
        }

        try
        {
            await using var transacao = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancelamento
            );
            var linha = await db.Set<RecursosPersonagem>()
                .FirstOrDefaultAsync(r => r.PersonagemId == personagemId, cancelamento);
            if (linha == null)
            {
                linha = CalculadoraRecursos.Novo(personagemId, maximos, agora);
                db.Set<RecursosPersonagem>().Add(linha);
                await db.SaveChangesAsync(cancelamento);
            }

            await transacao.CommitAsync(cancelamento);
            return Copiar(linha);
        }
        finally
        {
            if (sqlite != null && esperaAnterior != null)
            {
                sqlite.DefaultTimeout = esperaAnterior.Value;
            }
        }
    }

    public async Task SalvarAsync(long personagemId, RecursosPersonagem copia)
    {
        using var escopo = _escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var linha = await db.Set<RecursosPersonagem>()
            .FirstOrDefaultAsync(r => r.PersonagemId == personagemId);
        if (linha == null)
        {
            db.Set<RecursosPersonagem>().Add(copia);
        }
        else
        {
            Copiar(copia, linha);
        }

        await db.SaveChangesAsync();
    }

    public static RecursosPersonagem Copiar(RecursosPersonagem origem) =>
        Copiar(origem, new RecursosPersonagem { PersonagemId = origem.PersonagemId });

    public static RecursosPersonagem Copiar(RecursosPersonagem origem, RecursosPersonagem destino)
    {
        destino.Vida = origem.Vida;
        destino.Stamina = origem.Stamina;
        destino.Mana = origem.Mana;
        destino.UltimoGastoStaminaEm = origem.UltimoGastoStaminaEm;
        destino.UltimoGastoManaEm = origem.UltimoGastoManaEm;
        destino.StaminaContadaAte = origem.StaminaContadaAte;
        destino.ManaContadaAte = origem.ManaContadaAte;
        return destino;
    }
}
