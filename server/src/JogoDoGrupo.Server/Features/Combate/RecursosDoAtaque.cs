using System.Data;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Combate;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Resultado de conferir os recursos no começo de um ataque: <see cref="Recusa"/> é um código
/// de <see cref="CodigosErroCombate"/> (nada foi gasto) ou nulo se o ataque vale;
/// <see cref="Recursos"/> é o novo estado, só quando o ataque gastou vigor.
/// </summary>
public sealed record InicioDoAtaque(string? Recusa, RecursosMudaram? Recursos);

/// <summary>
/// A parte do ataque que mexe nos recursos guardados: recusa quem está caído ou sem vigor e
/// cobra o vigor do pesado, uma vez só. Lê, regenera, confere e grava numa transação
/// <c>Serializable</c> (no SQLite abre com <c>BEGIN IMMEDIATE</c>), a mesma da rota de recursos,
/// então pedidos simultâneos rodam um depois do outro e o vigor nunca é cobrado duas vezes.
/// </summary>
internal static class RecursosDoAtaque
{
    // Mesma espera pela trava do banco da rota de recursos.
    private const int EsperaTravaSegundos = 5;

    public static async Task<InicioDoAtaque> TentarIniciarAsync(
        IServiceScopeFactory escopos,
        TimeProvider relogio,
        long personagemId,
        TipoDeAtaque tipo,
        CancellationToken cancelamento
    )
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var bonus = escopo.ServiceProvider.GetRequiredService<IBonusDeAtributos>();

        var atributos = CalculadoraRecursos.AtributosFinais(
            await bonus.EquipamentosAsync(personagemId, cancelamento),
            await bonus.EfeitosAsync(personagemId, cancelamento)
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

            string? recusa = null;
            var custo = RegrasAtaque.CustoDeVigor(tipo);
            if (recursos.Vida <= 0)
            {
                recusa = CodigosErroCombate.VoceEstaCaido;
            }
            else if (!RegrasAtaque.TemVigor(tipo, recursos.Vigor))
            {
                recusa = CodigosErroCombate.VigorInsuficiente;
            }
            else if (custo > 0)
            {
                recursos.Vigor -= custo;
                recursos.UltimoGastoVigorEm = agora;
                recursos.VigorContadoAte = agora;
            }

            await db.SaveChangesAsync(cancelamento);
            await transacao.CommitAsync(cancelamento);

            if (recusa != null)
            {
                return new InicioDoAtaque(recusa, null);
            }

            return new InicioDoAtaque(
                null,
                custo > 0
                    ? new RecursosMudaram
                    {
                        VidaAtual = recursos.Vida,
                        VidaMaxima = RegrasRecursos.VidaMaxima(atributos.Vitalidade),
                        VigorAtual = recursos.Vigor,
                        VigorMaximo = RegrasRecursos.VigorMaximo(),
                        EnergiaAtual = recursos.Energia,
                        EnergiaMaxima = RegrasRecursos.EnergiaMaxima(atributos.Espirito),
                    }
                    : null
            );
        }
        finally
        {
            if (sqlite != null && esperaAnterior != null)
            {
                sqlite.DefaultTimeout = esperaAnterior.Value;
            }
        }
    }

    /// <summary>A Força final do personagem agora (equipamentos mais efeitos).</summary>
    public static async Task<int> ForcaAsync(
        IServiceScopeFactory escopos,
        long personagemId,
        CancellationToken cancelamento
    )
    {
        using var escopo = escopos.CreateScope();
        var bonus = escopo.ServiceProvider.GetRequiredService<IBonusDeAtributos>();
        return CalculadoraRecursos
            .AtributosFinais(
                await bonus.EquipamentosAsync(personagemId, cancelamento),
                await bonus.EfeitosAsync(personagemId, cancelamento)
            )
            .Forca;
    }
}
