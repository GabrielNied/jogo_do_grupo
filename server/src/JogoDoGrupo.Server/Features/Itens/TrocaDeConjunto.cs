using System.Data;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// A troca de arma (tecla R) vista por Itens: vira o conjunto ativo entre 1 e 2, grava, avisa a
/// área do visual e publica <see cref="EquipamentoMudou"/> (Combate invalida os atributos e Skills
/// reenvia a barra da arma nova). O bloqueio de 0,4 s é de Combate, que chama isto depois de
/// reservar o personagem.
/// </summary>
internal sealed class TrocaDeConjunto(
    IServiceScopeFactory escopos,
    VisualEquipadoServico visual,
    EventosDoServidor eventos
) : ITrocaDeConjunto
{
    public async Task<int?> AlternarAsync(long personagemId, CancellationToken cancelamento)
    {
        await using var escopo = escopos.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );
        var estado = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { personagemId }, cancelamento);
        var novo = (estado?.ConjuntoAtivo ?? 1) == 1 ? 2 : 1;
        if (estado == null)
        {
            estado = new EstadoEquipamentos { PersonagemId = personagemId };
            db.Add(estado);
        }

        estado.ConjuntoAtivo = novo;
        var itens = await db.Set<ItemInventario>()
            .AsNoTracking()
            .Where(i =>
                i.PersonagemId == personagemId && i.Posicao != PosicaoEquipamento.Inventario
            )
            .ToListAsync(cancelamento);
        await db.SaveChangesAsync(cancelamento);
        await transacao.CommitAsync(cancelamento);

        // Só depois de gravado, como na rota do inventário.
        visual.Publicar(
            RegrasVisualEquipado.Montar(personagemId, novo, itens.Select(i => i.ParaEntrada()))
        );
        eventos.Publicar(new EquipamentoMudou(personagemId));
        return novo;
    }
}
