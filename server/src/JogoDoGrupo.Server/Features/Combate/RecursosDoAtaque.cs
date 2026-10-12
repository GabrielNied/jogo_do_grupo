using System.Data;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Resultado de conferir os recursos no começo de um ataque: <see cref="Recusa"/> é um código
/// de <see cref="CodigosErroCombate"/> (nada foi gasto) ou nulo se o ataque vale;
/// <see cref="Recursos"/> é o novo estado, só quando o ataque gastou vigor. <see cref="Forca"/>
/// é a Força final lida na mesma ida ao banco (o acerto não volta lá).
/// </summary>
public sealed record InicioDoAtaque(string? Recusa, RecursosMudaram? Recursos, int Forca = 0);

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

    public static Task<InicioDoAtaque> TentarIniciarAsync(
        IServiceScopeFactory escopos,
        TimeProvider relogio,
        long personagemId,
        TipoDeAtaque tipo,
        CancellationToken cancelamento
    ) =>
        TentarGastarVigorAsync(
            escopos,
            relogio,
            personagemId,
            RegrasAtaque.CustoDeVigor(tipo),
            cancelamento
        );

    /// <summary>
    /// A mesma conferência para qualquer ação que custa vigor (ataque ou esquiva): recusa quem
    /// está caído ou não tem <paramref name="custo"/> de vigor; senão cobra, uma vez só.
    /// </summary>
    public static async Task<InicioDoAtaque> TentarGastarVigorAsync(
        IServiceScopeFactory escopos,
        TimeProvider relogio,
        long personagemId,
        int custo,
        CancellationToken cancelamento
    )
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var atributos = await AtributosAsync(escopo.ServiceProvider, personagemId, cancelamento);

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
            if (recursos.Vida <= 0)
            {
                recusa = CodigosErroCombate.VoceEstaCaido;
            }
            else if (recursos.Vigor < custo)
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
                return new InicioDoAtaque(recusa, null, atributos.Forca);
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
                    : null,
                atributos.Forca
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

    /// <summary>
    /// Tira <paramref name="dano"/> da vida do personagem (nunca abaixo de 0), na mesma transação
    /// <c>Serializable</c> do resto: golpes simultâneos rodam um depois do outro e somam. Devolve
    /// o novo estado para o dono.
    /// </summary>
    public static async Task<RecursosMudaram> TirarVidaAsync(
        IServiceScopeFactory escopos,
        TimeProvider relogio,
        long personagemId,
        int dano,
        CancellationToken cancelamento
    )
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var atributos = await AtributosAsync(escopo.ServiceProvider, personagemId, cancelamento);

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

            recursos.Vida = Math.Max(0, recursos.Vida - dano);

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

    internal static async Task<AtributosCombate> AtributosAsync(
        IServiceProvider servicos,
        long personagemId,
        CancellationToken cancelamento
    )
    {
        var bonus = servicos.GetRequiredService<IBonusDeAtributos>();
        var pontos = servicos.GetRequiredService<IPontosDistribuidos>();
        return CalculadoraRecursos.AtributosFinais(
            await bonus.EquipamentosAsync(personagemId, cancelamento),
            await bonus.EfeitosAsync(personagemId, cancelamento),
            await pontos.ObterAsync(personagemId, cancelamento)
        );
    }
}
