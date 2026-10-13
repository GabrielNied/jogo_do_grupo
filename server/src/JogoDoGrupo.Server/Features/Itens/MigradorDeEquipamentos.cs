using System.Data;
using JogoDoGrupo.Server.Core.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Grava de volta, na subida do servidor, os exemplares que ainda estão na escala antiga dos
/// atributos (issue #615). O exemplar é um JSON na coluna <c>EquipamentoJson</c>, então não há
/// mudança de esquema para uma migração do EF: a conversão é a regra de
/// <see cref="Shared.Features.Itens.MigracaoDeTier"/>, aplicada por
/// <see cref="ItemInventario.LerEquipamento"/>. Gravar cedo fixa o <c>TierOrigem</c> (ele não
/// pode mudar se os limiares forem ajustados depois). Enquanto isso não roda, ler já devolve o
/// exemplar convertido, então nada depende desta passagem para funcionar.
/// </summary>
public sealed class MigradorDeEquipamentos(
    IServiceScopeFactory escopos,
    ILogger<MigradorDeEquipamentos> log
) : IHostedService
{
    private const int TamanhoDoLote = 200;

    // Exemplar já na escala nova tem o campo gravado, com qualquer versão.
    private const string PadraoDaVersao = "%\"VersaoDosAtributos\":%";

    public async Task StartAsync(CancellationToken cancelamento)
    {
        try
        {
            var migrados = await MigrarAsync(cancelamento);
            if (migrados > 0)
                log.LogInformation(
                    "{Quantidade} equipamentos convertidos para a escala 0 a 100.",
                    migrados
                );
        }
        catch (Exception erro) when (erro is not OperationCanceledException)
        {
            // Banco ainda sem esquema (migração do EF desligada): a leitura converte na hora.
            log.LogWarning(erro, "Não foi possível converter os equipamentos antigos agora.");
        }
    }

    public Task StopAsync(CancellationToken cancelamento) => Task.CompletedTask;

    /// <summary>Converte e grava todos os exemplares antigos. Devolve quantos foram gravados.</summary>
    public async Task<int> MigrarAsync(CancellationToken cancelamento)
    {
        var total = 0;
        var ultimoId = 0L;
        while (true)
        {
            await using var escopo = escopos.CreateAsyncScope();
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            await using var transacao = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancelamento
            );
            var lote = await db.Set<ItemInventario>()
                .Where(i =>
                    i.Id > ultimoId
                    && i.EquipamentoJson != null
                    && !EF.Functions.Like(i.EquipamentoJson, PadraoDaVersao)
                )
                .OrderBy(i => i.Id)
                .Take(TamanhoDoLote)
                .ToListAsync(cancelamento);
            if (lote.Count == 0)
                return total;
            foreach (var item in lote)
            {
                ultimoId = item.Id;
                try
                {
                    var equipamento = item.LerEquipamento();
                    if (equipamento == null)
                        continue;
                    item.GravarEquipamento(equipamento);
                    total++;
                }
                catch (System.Text.Json.JsonException erro)
                {
                    log.LogWarning(
                        erro,
                        "Equipamento {Item} com JSON inválido; não convertido.",
                        item.Id
                    );
                }
            }
            await db.SaveChangesAsync(cancelamento);
            await transacao.CommitAsync(cancelamento);
        }
    }
}
