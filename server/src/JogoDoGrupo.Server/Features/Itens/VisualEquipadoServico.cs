using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Avisa a área do que cada personagem veste e se vê (armas na mão e nas costas). Sempre vai o
/// estado inteiro do personagem (<see cref="VisualEquipado"/>). Sai quando ele equipa, desequipa
/// ou troca de conjunto (quem decide a mudança é a rota, já gravada) e quando alguém entra na
/// área: o recém-chegado recebe o visual de quem já estava e a área recebe o dele.
/// </summary>
public sealed class VisualEquipadoServico(
    ServidorTempoReal tempoReal,
    IPosicoesDoMundo posicoes,
    IServiceScopeFactory escopos,
    ILogger<VisualEquipadoServico> log
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken parada) => Task.CompletedTask;

    /// <summary>
    /// Manda o visual à área do personagem, ele incluído. Sem efeito se ele não está em nenhuma
    /// área (não tem quem veja).
    /// </summary>
    public void Publicar(VisualEquipado visual)
    {
        if (!posicoes.TentarObter(visual.PersonagemId, out var posicao))
        {
            return;
        }

        var presentes = new HashSet<long>(posicoes.PersonagensNaArea(posicao.Area))
        {
            visual.PersonagemId,
        };
        foreach (var sessao in tempoReal.Sessoes)
        {
            if (presentes.Contains(sessao.PersonagemId))
            {
                tempoReal.Enviar(sessao, ItensMessageIds.VisualEquipado, visual);
            }
        }
    }

    /// <summary>Trata <see cref="JogadorEntrouNaArea"/>: roda numa tarefa (lê o banco).</summary>
    public void AoEntrarNaArea(JogadorEntrouNaArea entrada) => _ = EntrouAsync(entrada);

    internal async Task EntrouAsync(JogadorEntrouNaArea entrada)
    {
        try
        {
            await using var escopo = escopos.CreateAsyncScope();
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var visualDele = await LerAsync(db, entrada.PersonagemId);
            Publicar(visualDele);

            var novo = tempoReal.Sessoes.FirstOrDefault(s =>
                s.PersonagemId == entrada.PersonagemId
            );
            if (novo == null)
            {
                return;
            }

            foreach (var outroId in posicoes.PersonagensNaArea(entrada.Area))
            {
                if (outroId == entrada.PersonagemId)
                {
                    continue;
                }

                var visual = await LerAsync(db, outroId);
                if (visual.Armas.Length > 0)
                {
                    tempoReal.Enviar(novo, ItensMessageIds.VisualEquipado, visual);
                }
            }
        }
        catch (Exception erro)
        {
            log.LogError(
                erro,
                "Erro ao avisar o visual equipado de {Personagem}.",
                entrada.PersonagemId
            );
        }
    }

    /// <summary>O visual gravado do personagem (uma leitura, sem transação).</summary>
    internal static async Task<VisualEquipado> LerAsync(JogoDbContext db, long personagemId)
    {
        var equipados = await db.Set<ItemInventario>()
            .Where(i =>
                i.PersonagemId == personagemId && i.Posicao != PosicaoEquipamento.Inventario
            )
            .ToListAsync();
        var estado = await db.Set<EstadoEquipamentos>().FindAsync(personagemId);
        return RegrasVisualEquipado.Montar(
            personagemId,
            estado?.ConjuntoAtivo ?? 1,
            equipados.Select(i => i.ParaEntrada())
        );
    }
}
