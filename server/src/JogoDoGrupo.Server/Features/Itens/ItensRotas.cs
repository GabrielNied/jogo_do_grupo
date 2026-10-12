using System.Data;
using System.Globalization;
using System.Security.Claims;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonagemEntidade = JogoDoGrupo.Server.Features.Personagem.Personagem;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Inventário e baú de teste (rotas <see cref="RotasItens"/>, com token). A conta vem sempre do
/// token; personagem inexistente e personagem de outra conta respondem o mesmo 404.
/// </summary>
internal static class ItensRotas
{
    // Espera máxima pela trava do banco na abertura (o client desiste em 10 s). Só a conexão
    // desta requisição muda, e volta ao valor anterior no fim.
    private const int EsperaTravaSegundos = 5;

    public static void Mapear(IEndpointRouteBuilder rotas)
    {
        EquipamentosRotas.Mapear(rotas);
        rotas.MapGet(RotasItens.Inventario, InventarioAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.Bau, AbrirBauAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.Descartar, DescartarAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.Mover, MoverAsync).RequireAuthorization();
    }

    private static async Task<IResult> InventarioAsync(
        string personagemId,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        CancellationToken cancelamento
    )
    {
        var contaId = usuario.ObterContaId();
        if (!await PersonagemDaContaAsync(personagemId, contaId, db, cancelamento))
        {
            return PersonagemNaoEncontrado();
        }

        var id = ParaId(personagemId);
        var entradas = await LerInventarioAsync(db, id, cancelamento);
        return Results.Ok(
            new InventarioResponse { Entradas = entradas, Capacidade = Bolsa.CapacidadeDe(id) }
        );
    }

    /// <summary>
    /// Destrói definitivamente um exemplar da bolsa (não vira loot no chão). Transação
    /// Serializable, como o equipar: dois descartes do mesmo exemplar rodam em série e só o
    /// primeiro apaga; o segundo não acha o exemplar (404 <c>ItemNaoEncontrado</c>, sem efeito).
    /// Um descarte concorrente a um equipar vê o exemplar na bolsa ou já equipado (409), nunca
    /// um meio-termo.
    /// </summary>
    private static async Task<IResult> DescartarAsync(
        string personagemId,
        PedidoDescartar pedido,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        CancellationToken cancelamento
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );
        if (!await PersonagemDaContaAsync(personagemId, usuario.ObterContaId(), db, cancelamento))
        {
            return PersonagemNaoEncontrado();
        }

        var id = ParaId(personagemId);
        var itens = await db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == id)
            .OrderBy(i => i.Id)
            .ToListAsync(cancelamento);
        Bolsa.Normalizar(itens, Bolsa.CapacidadeDe(id));
        var alvo = itens.SingleOrDefault(i => i.Id == pedido.InstanciaId);
        if (alvo == null)
        {
            return RespostasApi.Erro(
                StatusCodes.Status404NotFound,
                CodigosErroItens.ItemNaoEncontrado
            );
        }
        if (alvo.Posicao != PosicaoEquipamento.Inventario)
        {
            return RespostasApi.Erro(StatusCodes.Status409Conflict, CodigosErroItens.ItemEquipado);
        }

        db.Remove(alvo);
        itens.Remove(alvo);
        await db.SaveChangesAsync(cancelamento);
        var estado = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { id }, cancelamento);
        var resposta = EquipamentosRotas.Resposta(itens, estado?.ConjuntoAtivo ?? 1);
        await transacao.CommitAsync(cancelamento);
        return Results.Ok(resposta);
    }

    /// <summary>
    /// Move um item da bolsa para uma casa. Casa ocupada: os dois trocam de lugar na mesma
    /// transação (Serializable, como o equipar), então dois pedidos para a mesma casa rodam em
    /// série e nunca deixam duas peças nela. Item equipado é 409 (desequipe pelo encaixe).
    /// </summary>
    private static async Task<IResult> MoverAsync(
        string personagemId,
        PedidoMover pedido,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        CancellationToken cancelamento
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );
        if (!await PersonagemDaContaAsync(personagemId, usuario.ObterContaId(), db, cancelamento))
        {
            return PersonagemNaoEncontrado();
        }

        var id = ParaId(personagemId);
        var capacidade = Bolsa.CapacidadeDe(id);
        if (!capacidade.Contem(pedido.Aba, pedido.Casa))
        {
            return RespostasApi.Erro(
                StatusCodes.Status400BadRequest,
                CodigosErroItens.CasaInvalida
            );
        }

        var itens = await db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == id)
            .OrderBy(i => i.Id)
            .ToListAsync(cancelamento);
        var alvo = itens.SingleOrDefault(i => i.Id == pedido.InstanciaId);
        if (alvo == null)
        {
            return RespostasApi.Erro(
                StatusCodes.Status404NotFound,
                CodigosErroItens.ItemNaoEncontrado
            );
        }
        if (!Bolsa.EstaNaBolsa(alvo))
        {
            return RespostasApi.Erro(StatusCodes.Status409Conflict, CodigosErroItens.ItemEquipado);
        }

        Bolsa.Normalizar(itens, capacidade);
        var ocupante = Bolsa.Em(itens, pedido.Aba, pedido.Casa);
        if (ocupante?.Id != alvo.Id)
        {
            var abaOrigem = alvo.Aba;
            var casaOrigem = alvo.Casa;
            // Um passo por vez: o índice único não aceita duas peças na mesma casa nem
            // por um instante.
            Bolsa.Limpar(alvo);
            await db.SaveChangesAsync(cancelamento);
            if (ocupante != null && abaOrigem is int aba && casaOrigem is int casa)
            {
                Bolsa.Colocar(ocupante, aba, casa);
                await db.SaveChangesAsync(cancelamento);
            }
            Bolsa.Colocar(alvo, pedido.Aba, pedido.Casa);
            await db.SaveChangesAsync(cancelamento);
        }
        else
        {
            await db.SaveChangesAsync(cancelamento);
        }

        var estado = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { id }, cancelamento);
        var resposta = EquipamentosRotas.Resposta(itens, estado?.ConjuntoAtivo ?? 1);
        await transacao.CommitAsync(cancelamento);
        return Results.Ok(resposta);
    }

    private static async Task<IResult> AbrirBauAsync(
        string personagemId,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        ISorteadorBau sorteador,
        GeradorEquipamentos gerador,
        ILogger<ItensModule> log,
        CancellationToken cancelamento
    )
    {
        var contaId = usuario.ObterContaId();
        if (!await PersonagemDaContaAsync(personagemId, contaId, db, cancelamento))
        {
            return PersonagemNaoEncontrado();
        }

        var id = ParaId(personagemId);
        var sorteados = sorteador.Sortear();

        // O Microsoft.Data.Sqlite espera a trava pelo tempo do comando, que vem do
        // DefaultTimeout da conexão (inclusive no BEGIN e no COMMIT).
        var sqlite = db.Database.GetDbConnection() as SqliteConnection;
        var esperaAnterior = sqlite?.DefaultTimeout;
        if (sqlite != null)
        {
            sqlite.DefaultTimeout = EsperaTravaSegundos;
        }

        try
        {
            return await GravarEmTransacaoAsync(id, sorteados, db, gerador, cancelamento);
        }
        catch (Exception e) when (e.EhBancoOcupado())
        {
            log.LogWarning(
                "Banco ocupado por mais de {Espera} s ao abrir baú.",
                EsperaTravaSegundos
            );
            return RespostasApi.Erro(
                StatusCodes.Status503ServiceUnavailable,
                CodigosErro.ServidorOcupado,
                "Servidor ocupado. Tente de novo."
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
    /// Confere o espaço e grava todos os itens sorteados numa transação só, ou não grava nada.
    /// No SQLite, Serializable abre a transação com <c>BEGIN IMMEDIATE</c>: ela já começa com a
    /// trava de escrita, então dois pedidos simultâneos rodam um depois do outro e o segundo
    /// conta o que o primeiro gravou. Assim o inventário nunca passa de
    /// <see cref="RegrasItens.EspacosInventario"/>. Isso vale só para o SQLite (no PostgreSQL
    /// o Core reconhece a falha de serialização e esta rota responde 503).
    /// </summary>
    private static async Task<IResult> GravarEmTransacaoAsync(
        long personagemId,
        int[] sorteados,
        JogoDbContext db,
        GeradorEquipamentos gerador,
        CancellationToken cancelamento
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );

        var capacidade = Bolsa.CapacidadeDe(personagemId);
        var existentes = await db.Set<ItemInventario>()
            .Where(i =>
                i.PersonagemId == personagemId && i.Posicao == PosicaoEquipamento.Inventario
            )
            .OrderBy(i => i.Id)
            .ToListAsync(cancelamento);
        Bolsa.Normalizar(existentes, capacidade);
        var novos = sorteados
            .Select(itemId => new ItemInventario { PersonagemId = personagemId, ItemId = itemId })
            .ToList();
        if (!Bolsa.Guardar(existentes, novos, capacidade))
        {
            return RespostasApi.Erro(
                StatusCodes.Status409Conflict,
                CodigosErroItens.InventarioCheio
            );
        }

        foreach (var novo in novos)
        {
            var equipamento = gerador.Gerar(novo.ItemId, Random.Shared);
            if (equipamento != null)
                novo.GravarEquipamento(equipamento);
        }
        db.Set<ItemInventario>().AddRange(novos);
        await db.SaveChangesAsync(cancelamento);

        var inventario = await LerInventarioAsync(db, personagemId, cancelamento);
        await transacao.CommitAsync(cancelamento);

        return Results.Ok(
            new AbrirBauResponse
            {
                Ganhos = novos.Select(i => i.ParaEntrada()).ToArray(),
                Inventario = inventario,
                Capacidade = capacidade,
            }
        );
    }

    private static async Task<EntradaInventario[]> LerInventarioAsync(
        JogoDbContext db,
        long personagemId,
        CancellationToken cancelamento
    )
    {
        var itens = await db.Set<ItemInventario>()
            .AsNoTracking()
            .Where(i =>
                i.PersonagemId == personagemId && i.Posicao == PosicaoEquipamento.Inventario
            )
            .OrderBy(i => i.Id)
            .ToListAsync(cancelamento);
        Bolsa.Normalizar(itens, Bolsa.CapacidadeDe(personagemId));
        return itens.Select(i => i.ParaEntrada()).ToArray();
    }

    /// <summary>
    /// O personagem existe e é da conta? Id que não é número também não existe (mesmo 404).
    /// </summary>
    internal static async Task<bool> PersonagemDaContaAsync(
        string personagemId,
        long contaId,
        JogoDbContext db,
        CancellationToken cancelamento
    )
    {
        var id = ParaId(personagemId);
        return id > 0
            && await db.Set<PersonagemEntidade>()
                .AnyAsync(p => p.Id == id && p.ContaId == contaId, cancelamento);
    }

    private static long ParaId(string personagemId) =>
        long.TryParse(personagemId, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : 0;

    internal static Task<int> NivelAsync(long id, JogoDbContext db, CancellationToken c) =>
        db.Set<PersonagemEntidade>().Where(p => p.Id == id).Select(p => p.Nivel).SingleAsync(c);

    private static IResult PersonagemNaoEncontrado() =>
        RespostasApi.Erro(StatusCodes.Status404NotFound, CodigosErroItens.PersonagemNaoEncontrado);
}
