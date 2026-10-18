using System.Collections.Concurrent;
using System.Data;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>Lembra, por personagem, o último item que a tela de teste gerou.</summary>
public sealed class MemoriaDeItensDeTeste
{
    private readonly ConcurrentDictionary<long, long> _ultimos = new();

    public void Lembrar(long personagemId, long instanciaId) =>
        _ultimos[personagemId] = instanciaId;

    public long? Ultimo(long personagemId) =>
        _ultimos.TryGetValue(personagemId, out var id) ? id : null;
}

/// <summary>
/// Itens da tela de teste. Gera pelo mesmo <see cref="GeradorEquipamentos"/> do loot, guarda pela
/// mesma regra da bolsa e veste pela mesma regra de equipar: a tela escolhe o que sortear, nunca
/// os valores.
/// </summary>
internal sealed class FerramentasDeItensDeTeste(
    JogoDbContext db,
    GeradorEquipamentos gerador,
    ISaldosMateriaisFusao materiais,
    MemoriaDeItensDeTeste memoria,
    VisualEquipadoServico visual,
    EventosDoServidor eventos
) : IFerramentasDeItensDeTeste
{
    public async Task<(bool Aceito, string Texto)> GerarAsync(
        long personagemId,
        int modeloId,
        GrauEquipamento grau,
        OrigemDoLoot origem,
        int quantidade,
        CancellationToken c
    )
    {
        if (quantidade < 1)
            return (false, "Quantidade inválida.");
        if (CatalogoEquipamentos.Obter(modeloId) == null)
            return (false, "Modelo de item desconhecido.");

        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            c
        );
        var estado = await db.Set<EstadoEquipamentos>().FindAsync(new object[] { personagemId }, c);
        var capacidade = Bolsa.CapacidadeDe(estado);
        var itens = await ItensDoPersonagemAsync(personagemId, c);
        Bolsa.Normalizar(itens, capacidade);

        var cabem = Math.Min(quantidade, Bolsa.Livres(itens, capacidade));
        if (cabem == 0)
            return (false, "A bolsa está cheia.");

        var novos = new List<ItemInventario>();
        var exemplares = new List<Equipamento>();
        for (var i = 0; i < cabem; i++)
        {
            var exemplar = gerador.Gerar(modeloId, Random.Shared, origem, grau);
            if (exemplar == null)
                return (false, "Modelo de item desconhecido.");
            var item = new ItemInventario { PersonagemId = personagemId, ItemId = modeloId };
            item.GravarEquipamento(exemplar);
            novos.Add(item);
            exemplares.Add(exemplar);
        }

        if (!Bolsa.Guardar(itens, novos, capacidade))
            return (false, "A bolsa está cheia.");

        db.Set<ItemInventario>().AddRange(novos);
        await db.SaveChangesAsync(c);
        await transacao.CommitAsync(c);
        memoria.Lembrar(personagemId, novos[^1].Id);

        var primeiro = exemplares[0];
        var tiers = string.Join(", ", exemplares.Select(e => NomeDoTier(e.TierOrigem)));
        var texto =
            $"{cabem} × {primeiro.Nome} ({primeiro.Grau}, {origem}), tier {tiers}; "
            + $"pede nível {primeiro.NivelPersonagemMinimo}.";
        if (cabem < quantidade)
            texto += $" {quantidade - cabem} não coube(ram) na bolsa.";
        return (true, texto);
    }

    public async Task<(bool Aceito, string Texto)> EquiparUltimoGeradoAsync(
        long personagemId,
        CancellationToken c
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            c
        );
        var estado = await db.Set<EstadoEquipamentos>().FindAsync(new object[] { personagemId }, c);
        var capacidade = Bolsa.CapacidadeDe(estado);
        var itens = await ItensDoPersonagemAsync(personagemId, c);
        Bolsa.Normalizar(itens, capacidade);

        var ultimoId = memoria.Ultimo(personagemId);
        var item = ultimoId == null ? null : itens.SingleOrDefault(i => i.Id == ultimoId);
        var dados = item?.LerEquipamento();
        if (item == null || dados == null)
            return (false, "Nenhum item gerado pela tela está na bolsa.");

        var conjunto = estado?.ConjuntoAtivo ?? 1;
        var posicao = RegrasEquipamentos.EncaixeAutomatico(
            dados.Tipo,
            itens.Where(i => i.Posicao != PosicaoEquipamento.Inventario).Select(i => i.Posicao),
            conjunto
        );
        var recusa = await EquipamentosRotas.EquiparPecaAsync(
            db,
            itens,
            item,
            posicao,
            personagemId,
            capacidade,
            gerador,
            c
        );
        if (recusa != null)
            return (false, $"Não vestiu: {recusa}.");

        await db.SaveChangesAsync(c);
        var nivel = await ItensRotas.NivelAsync(personagemId, db, c);
        var resposta = EquipamentosRotas.Resposta(itens, conjunto, nivel, capacidade);
        await transacao.CommitAsync(c);
        PublicarMudanca(personagemId, conjunto, resposta);
        return (true, $"{dados.Nome} vestido em {posicao}.");
    }

    public async Task<(bool Aceito, string Texto)> LimparBolsaAsync(
        long personagemId,
        CancellationToken c
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            c
        );
        var itens = await ItensDoPersonagemAsync(personagemId, c);
        var apagar = itens
            .Where(i => Bolsa.EstaNaBolsa(i) && i.LerEquipamento()?.Favorito != true)
            .ToList();
        if (apagar.Count == 0)
            return (false, "Nada para apagar na bolsa.");

        db.Set<ItemInventario>().RemoveRange(apagar);
        await db.SaveChangesAsync(c);
        await transacao.CommitAsync(c);
        return (true, $"{apagar.Count} item(ns) apagado(s) da bolsa; favoritos ficaram.");
    }

    public async Task<(bool Aceito, string Texto)> AjustarDurabilidadeDosEquipadosAsync(
        long personagemId,
        bool quebrar,
        CancellationToken c
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            c
        );
        var estado = await db.Set<EstadoEquipamentos>().FindAsync(new object[] { personagemId }, c);
        var capacidade = Bolsa.CapacidadeDe(estado);
        var itens = await ItensDoPersonagemAsync(personagemId, c);
        var mudados = 0;
        foreach (var item in itens.Where(i => i.Posicao != PosicaoEquipamento.Inventario))
        {
            var dados = item.LerEquipamento();
            if (dados == null)
                continue;
            dados.DurabilidadeAtual = quebrar ? 0 : dados.DurabilidadeMaxima;
            item.GravarEquipamento(dados);
            mudados++;
        }

        if (mudados == 0)
            return (false, "Nenhuma peça vestida.");

        await db.SaveChangesAsync(c);
        var conjunto = estado?.ConjuntoAtivo ?? 1;
        var nivel = await ItensRotas.NivelAsync(personagemId, db, c);
        var resposta = EquipamentosRotas.Resposta(itens, conjunto, nivel, capacidade);
        await transacao.CommitAsync(c);
        PublicarMudanca(personagemId, conjunto, resposta);
        return (
            true,
            quebrar
                ? $"{mudados} peça(s) quebrada(s)."
                : $"{mudados} peça(s) consertada(s), sem custo."
        );
    }

    public async Task<(bool Aceito, string Texto)> CreditarEssenciasAsync(
        long personagemId,
        int quantidade,
        CancellationToken c
    )
    {
        if (quantidade < 1)
            return (false, "Quantidade inválida.");

        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            c
        );
        await materiais.CreditarDungeonAsync(personagemId, quantidade, c);
        await materiais.CreditarRaidAsync(personagemId, quantidade, c);
        await db.SaveChangesAsync(c);
        await transacao.CommitAsync(c);
        var (dungeon, raid) = await materiais.SaldoAsync(personagemId, c);
        return (true, $"Essências: {dungeon} de dungeon e {raid} de raid.");
    }

    private Task<List<ItemInventario>> ItensDoPersonagemAsync(
        long personagemId,
        CancellationToken c
    ) =>
        db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == personagemId)
            .OrderBy(i => i.Id)
            .ToListAsync(c);

    // Só depois de gravado, como na rota: a área vê o que o servidor aceitou e o combate relê.
    private void PublicarMudanca(long personagemId, int conjunto, EquipamentosResponse resposta)
    {
        visual.Publicar(RegrasVisualEquipado.Montar(personagemId, conjunto, resposta.Equipados));
        eventos.Publicar(new EquipamentoMudou(personagemId));
    }

    private static string NomeDoTier(TierItem tier) =>
        tier == TierItem.SMais ? "S+" : tier.ToString();
}
