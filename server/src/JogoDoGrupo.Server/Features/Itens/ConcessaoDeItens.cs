using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Item de recompensa de missão: arma sem arma vestida entra equipada no conjunto 1, o resto vai
/// para a bolsa pelo mesmo caminho da loja. Se a regra de equipar recusar (nível, por exemplo), a
/// arma fica na bolsa.
/// </summary>
internal sealed class ConcessaoDeItens(
    JogoDbContext db,
    IInventarioEconomia inventario,
    GeradorEquipamentos gerador,
    VisualEquipadoServico visual,
    EventosDoServidor eventos
) : IConcessaoDeItens
{
    private static readonly PosicaoEquipamento[] SlotsDeArma =
    [
        PosicaoEquipamento.Arma1,
        PosicaoEquipamento.Secundaria1,
        PosicaoEquipamento.Arma2,
        PosicaoEquipamento.Secundaria2,
    ];

    public async Task<DestinoDaConcessao> ConcederAsync(
        long personagemId,
        int itemId,
        CancellationToken c
    )
    {
        if (!await inventario.AdicionarItemCompradoAsync(personagemId, itemId, 1, c))
            return DestinoDaConcessao.Recusado;

        var modelo = CatalogoEquipamentos.Obter(itemId);
        if (
            modelo == null
            || modelo.Tipo < TipoEquipamento.Espada
            || modelo.Tipo > TipoEquipamento.Cajado
        )
            return DestinoDaConcessao.Bolsa;

        var itens = await db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == personagemId)
            .OrderBy(i => i.Id)
            .ToListAsync(c);
        if (itens.Any(i => SlotsDeArma.Contains(i.Posicao)))
            return DestinoDaConcessao.Bolsa;

        var novo = itens.LastOrDefault(i =>
            i.ItemId == itemId && i.Posicao == PosicaoEquipamento.Inventario
        );
        if (novo == null)
            return DestinoDaConcessao.Bolsa;

        var estado = await db.Set<EstadoEquipamentos>().FindAsync(new object[] { personagemId }, c);
        var recusa = await EquipamentosRotas.EquiparPecaAsync(
            db,
            itens,
            novo,
            PosicaoEquipamento.Arma1,
            personagemId,
            Bolsa.CapacidadeDe(estado),
            gerador,
            c
        );
        if (recusa != null)
            return DestinoDaConcessao.Bolsa;

        await db.SaveChangesAsync(c);
        return DestinoDaConcessao.Equipado;
    }

    public async Task AvisarAsync(long personagemId, CancellationToken c)
    {
        var estado = await db.Set<EstadoEquipamentos>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.PersonagemId == personagemId, c);
        var itens = await db.Set<ItemInventario>()
            .AsNoTracking()
            .Where(i =>
                i.PersonagemId == personagemId && i.Posicao != PosicaoEquipamento.Inventario
            )
            .ToListAsync(c);
        visual.Publicar(
            RegrasVisualEquipado.Montar(
                personagemId,
                estado?.ConjuntoAtivo ?? 1,
                itens.Select(i => i.ParaEntrada())
            )
        );
        eventos.Publicar(new EquipamentoMudou(personagemId));
    }
}
