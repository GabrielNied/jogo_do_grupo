using System.Data;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Combate;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// A parte do estado caído que mexe nos recursos guardados: a vida com que o personagem levanta
/// e a volta à cidade com tudo cheio. Mesma transação <c>Serializable</c> do resto de Combate.
/// Não mexe em item, XP, moeda nem nível: morrer não perde nada.
/// </summary>
internal static class RecursosDaQueda
{
    // Mesma espera pela trava do banco da rota de recursos.
    private const int EsperaTravaSegundos = 5;

    /// <summary>Vida em 20% do máximo (mínimo 1); vigor e energia como estavam.</summary>
    public static Task<RecursosMudaram> LevantarAsync(
        IServiceScopeFactory escopos,
        TimeProvider relogio,
        long personagemId,
        CancellationToken cancelamento
    ) =>
        AtualizarAsync(
            escopos,
            relogio,
            personagemId,
            (recursos, atributos) =>
                recursos.Vida = RegrasQueda.VidaAoLevantar(
                    RegrasRecursos.VidaMaxima(atributos.Vitalidade)
                ),
            cancelamento
        );

    /// <summary>Vida, vigor e energia cheios (a volta à cidade).</summary>
    public static Task<RecursosMudaram> EncherTudoAsync(
        IServiceScopeFactory escopos,
        TimeProvider relogio,
        long personagemId,
        CancellationToken cancelamento
    ) =>
        AtualizarAsync(
            escopos,
            relogio,
            personagemId,
            (recursos, atributos) =>
            {
                recursos.Vida = RegrasRecursos.VidaMaxima(atributos.Vitalidade);
                recursos.Vigor = RegrasRecursos.VigorMaximo();
                recursos.Energia = RegrasRecursos.EnergiaMaxima(atributos.Espirito);
            },
            cancelamento
        );

    private static async Task<RecursosMudaram> AtualizarAsync(
        IServiceScopeFactory escopos,
        TimeProvider relogio,
        long personagemId,
        Action<RecursosPersonagem, AtributosCombate> mudar,
        CancellationToken cancelamento
    )
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var atributos = await RecursosDoAtaque.AtributosAsync(
            escopo.ServiceProvider,
            personagemId,
            cancelamento
        );

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

            var agora = relogio.GetUtcNow().UtcDateTime;
            var recursos = await db.Set<RecursosPersonagem>()
                .FirstOrDefaultAsync(r => r.PersonagemId == personagemId, cancelamento);
            if (recursos == null)
            {
                recursos = CalculadoraRecursos.Novo(personagemId, atributos, agora);
                db.Set<RecursosPersonagem>().Add(recursos);
            }
            else
            {
                CalculadoraRecursos.Atualizar(recursos, atributos, agora);
            }

            mudar(recursos, atributos);
            recursos.VigorContadoAte = agora;
            recursos.EnergiaContadaAte = agora;

            await db.SaveChangesAsync(cancelamento);
            await transacao.CommitAsync(cancelamento);

            return new RecursosMudaram
            {
                VidaAtual = recursos.Vida,
                VidaMaxima = RegrasRecursos.VidaMaxima(atributos.Vitalidade),
                VigorAtual = recursos.Vigor,
                VigorMaximo = RegrasRecursos.VigorMaximo(),
                EnergiaAtual = recursos.Energia,
                EnergiaMaxima = RegrasRecursos.EnergiaMaxima(atributos.Espirito),
            };
        }
        finally
        {
            if (sqlite != null && esperaAnterior != null)
            {
                sqlite.DefaultTimeout = esperaAnterior.Value;
            }
        }
    }
}
