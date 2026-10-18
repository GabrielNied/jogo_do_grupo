using System.Data;
using System.Globalization;
using System.Security.Claims;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Mundo;
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
        rotas.MapPost(RotasItens.DescartarLote, DescartarLoteAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.Lixo, LixoAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.Favorito, FavoritoAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.Mover, MoverAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.Ordenar, OrdenarAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.Expandir, ExpandirBolsaAsync).RequireAuthorization();
        rotas.MapGet(RotasItens.Banco, BancoAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.BancoDepositar, DepositarBancoAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.BancoRetirar, RetirarBancoAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.BancoMover, MoverBancoAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.BancoExpandir, ExpandirBancoAsync).RequireAuthorization();
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
        var estado = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { id }, cancelamento);
        var capacidade = Bolsa.CapacidadeDe(estado);
        var entradas = await LerInventarioAsync(db, id, cancelamento);
        return Results.Ok(new InventarioResponse { Entradas = entradas, Capacidade = capacidade });
    }

    /// <summary>
    /// Destrói definitivamente um exemplar da bolsa (não vira loot no chão). Transação
    /// Serializable, como o equipar: dois descartes do mesmo exemplar rodam em série e só o
    /// primeiro apaga; o segundo não acha o exemplar (404 <c>ItemNaoEncontrado</c>, sem efeito).
    /// Um descarte concorrente a um equipar vê o exemplar na bolsa ou já equipado (409), nunca
    /// um meio-termo. Item favorito é protegido (409 <c>ItemFavorito</c>).
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
        var estado = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { id }, cancelamento);
        var capacidade = Bolsa.CapacidadeDe(estado);
        var itens = await db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == id)
            .OrderBy(i => i.Id)
            .ToListAsync(cancelamento);
        Bolsa.Normalizar(itens, capacidade);
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
        if (alvo.LerEquipamento()?.Favorito == true)
        {
            return RespostasApi.Erro(StatusCodes.Status409Conflict, CodigosErroItens.ItemFavorito);
        }

        db.Remove(alvo);
        itens.Remove(alvo);
        await db.SaveChangesAsync(cancelamento);
        var resposta = EquipamentosRotas.Resposta(
            itens,
            estado?.ConjuntoAtivo ?? 1,
            await NivelAsync(id, db, cancelamento),
            capacidade
        );
        await transacao.CommitAsync(cancelamento);
        return Results.Ok(resposta);
    }

    /// <summary>
    /// Destrói múltiplos exemplares da bolsa em lote ou todos os marcados como lixo.
    /// Transação Serializable. Recusa com 409 se algum item for favorito ou equipado.
    /// </summary>
    private static async Task<IResult> DescartarLoteAsync(
        string personagemId,
        PedidoDescartarLote pedido,
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

        List<ItemInventario> alvos;
        if (pedido.ApenasLixo || (pedido.InstanciaIds != null && pedido.InstanciaIds.Length == 0))
        {
            alvos = itens
                .Where(i =>
                    i.Posicao == PosicaoEquipamento.Inventario && i.LerEquipamento()?.Lixo == true
                )
                .ToList();
        }
        else
        {
            var idsDesejados = (pedido.InstanciaIds ?? Array.Empty<long>()).ToHashSet();
            alvos = itens.Where(i => idsDesejados.Contains(i.Id)).ToList();
        }

        foreach (var item in alvos)
        {
            if (item.Posicao != PosicaoEquipamento.Inventario)
            {
                return RespostasApi.Erro(
                    StatusCodes.Status409Conflict,
                    CodigosErroItens.ItemEquipado
                );
            }
            if (item.LerEquipamento()?.Favorito == true)
            {
                return RespostasApi.Erro(
                    StatusCodes.Status409Conflict,
                    CodigosErroItens.ItemFavorito
                );
            }
        }

        if (alvos.Count > 0)
        {
            db.RemoveRange(alvos);
            foreach (var item in alvos)
            {
                itens.Remove(item);
            }
            await db.SaveChangesAsync(cancelamento);
        }

        var estado = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { id }, cancelamento);
        var resposta = EquipamentosRotas.Resposta(
            itens,
            estado?.ConjuntoAtivo ?? 1,
            await NivelAsync(id, db, cancelamento)
        );
        resposta.Aviso =
            alvos.Count > 0 ? $"{alvos.Count} item(ns) descartado(s)." : "Nenhum item descartado.";
        await transacao.CommitAsync(cancelamento);
        return Results.Ok(resposta);
    }

    /// <summary>
    /// Marca ou desmarca item(ns) como lixo. Item favorito não pode ser marcado como lixo (409 ItemFavorito).
    /// </summary>
    private static async Task<IResult> LixoAsync(
        string personagemId,
        PedidoLixo pedido,
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

        var ids = new HashSet<long>();
        if (pedido.InstanciaIds != null && pedido.InstanciaIds.Length > 0)
        {
            foreach (var instId in pedido.InstanciaIds)
            {
                ids.Add(instId);
            }
        }
        else if (pedido.InstanciaId > 0)
        {
            ids.Add(pedido.InstanciaId);
        }

        var alvos = itens.Where(i => ids.Contains(i.Id)).ToList();
        if (alvos.Count == 0 && pedido.InstanciaId > 0)
        {
            return RespostasApi.Erro(
                StatusCodes.Status404NotFound,
                CodigosErroItens.ItemNaoEncontrado
            );
        }

        foreach (var item in alvos)
        {
            var equip = item.LerEquipamento();
            if (equip == null)
            {
                continue;
            }
            if (pedido.Lixo && equip.Favorito)
            {
                return RespostasApi.Erro(
                    StatusCodes.Status409Conflict,
                    CodigosErroItens.ItemFavorito
                );
            }
            equip.Lixo = pedido.Lixo;
            item.GravarEquipamento(equip);
        }

        await db.SaveChangesAsync(cancelamento);
        var estado = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { id }, cancelamento);
        var resposta = EquipamentosRotas.Resposta(
            itens,
            estado?.ConjuntoAtivo ?? 1,
            await NivelAsync(id, db, cancelamento)
        );
        resposta.Aviso = pedido.Lixo
            ? "Item(ns) marcado(s) como lixo."
            : "Marcação de lixo removida.";
        await transacao.CommitAsync(cancelamento);
        return Results.Ok(resposta);
    }

    /// <summary>
    /// Marca ou desmarca item(ns) como favorito (protegido). Ao marcar como favorito, desmarca lixo automaticamente.
    /// </summary>
    private static async Task<IResult> FavoritoAsync(
        string personagemId,
        PedidoFavorito pedido,
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

        var ids = new HashSet<long>();
        if (pedido.InstanciaIds != null && pedido.InstanciaIds.Length > 0)
        {
            foreach (var instId in pedido.InstanciaIds)
            {
                ids.Add(instId);
            }
        }
        else if (pedido.InstanciaId > 0)
        {
            ids.Add(pedido.InstanciaId);
        }

        var alvos = itens.Where(i => ids.Contains(i.Id)).ToList();
        if (alvos.Count == 0 && pedido.InstanciaId > 0)
        {
            return RespostasApi.Erro(
                StatusCodes.Status404NotFound,
                CodigosErroItens.ItemNaoEncontrado
            );
        }

        foreach (var item in alvos)
        {
            var equip = item.LerEquipamento();
            if (equip == null)
            {
                continue;
            }
            equip.Favorito = pedido.Favorito;
            if (pedido.Favorito)
            {
                equip.Lixo = false;
            }
            item.GravarEquipamento(equip);
        }

        await db.SaveChangesAsync(cancelamento);
        var estado = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { id }, cancelamento);
        var resposta = EquipamentosRotas.Resposta(
            itens,
            estado?.ConjuntoAtivo ?? 1,
            await NivelAsync(id, db, cancelamento)
        );
        resposta.Aviso = pedido.Favorito
            ? "Item protegido como favorito."
            : "Proteção de favorito removida.";
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
        var estado = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { id }, cancelamento);
        var capacidade = Bolsa.CapacidadeDe(estado);
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

        var resposta = EquipamentosRotas.Resposta(
            itens,
            estado?.ConjuntoAtivo ?? 1,
            await NivelAsync(id, db, cancelamento),
            capacidade
        );
        await transacao.CommitAsync(cancelamento);
        return Results.Ok(resposta);
    }

    /// <summary>
    /// Ordena uma aba da bolsa (tipo, tier, nível; favoritos ficam onde estão, ver
    /// <see cref="Bolsa.PlanoDeOrdenacao"/>). Transação Serializable, como o mover: uma ordenação
    /// concorrente a um mover/equipar roda antes ou depois, nunca no meio, e as casas nunca ficam
    /// com duas peças. Ordenar de novo uma aba já ordenada não muda nada.
    /// </summary>
    private static async Task<IResult> OrdenarAsync(
        string personagemId,
        PedidoOrdenar pedido,
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
        var estado = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { id }, cancelamento);
        var capacidade = Bolsa.CapacidadeDe(estado);
        if (!capacidade.Contem(pedido.Aba, 0))
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
        Bolsa.Normalizar(itens, capacidade);
        var plano = Bolsa.PlanoDeOrdenacao(itens, pedido.Aba, capacidade);
        // Em dois passos: o índice único não aceita duas peças na mesma casa nem por um instante.
        foreach (var (item, _) in plano)
        {
            Bolsa.Limpar(item);
        }
        await db.SaveChangesAsync(cancelamento);
        foreach (var (item, casa) in plano)
        {
            Bolsa.Colocar(item, pedido.Aba, casa);
        }
        await db.SaveChangesAsync(cancelamento);

        var resposta = EquipamentosRotas.Resposta(
            itens,
            estado?.ConjuntoAtivo ?? 1,
            await NivelAsync(id, db, cancelamento),
            capacidade
        );
        await transacao.CommitAsync(cancelamento);
        return Results.Ok(resposta);
    }

    private static bool PertoDoBau(PosicaoNoMundo posicao) =>
        posicao.Area == ConfigBau.Area && ConfigBau.DentroDoRaio(posicao.X, posicao.Z);

    private static async Task<IResult> AbrirBauAsync(
        string personagemId,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        ISorteadorBau sorteador,
        GeradorEquipamentos gerador,
        IPosicoesDoMundo posicoes,
        OpcoesBau opcoesBau,
        RecargaDoBau recarga,
        ILogger<ItensModule> log,
        CancellationToken cancelamento
    )
    {
        var id = ParaId(personagemId);
        if (id <= 0)
        {
            return PersonagemNaoEncontrado();
        }

        // O servidor decide: a posição é a que o Mundo aceitou, nunca a que o client diz.
        if (
            opcoesBau.ExigirPosicao
            && !(posicoes.TentarObter(id, out var posicao) && PertoDoBau(posicao))
        )
        {
            return RespostasApi.Erro(
                StatusCodes.Status409Conflict,
                CodigosErroItens.BauLongeDemais,
                "Chegue mais perto do baú."
            );
        }

        // Reserva atômica: dois pedidos simultâneos nunca passam os dois. Devolvida se nada
        // foi aberto (bolsa cheia, banco ocupado, personagem inexistente).
        if (!recarga.TentarReservar(id))
        {
            return RespostasApi.Erro(
                StatusCodes.Status429TooManyRequests,
                CodigosErroItens.BauEmRecarga,
                "O baú ainda está recarregando."
            );
        }

        var contaId = usuario.ObterContaId();
        if (!await PersonagemDaContaAsync(personagemId, contaId, db, cancelamento))
        {
            recarga.Liberar(id);
            return PersonagemNaoEncontrado();
        }

        var sorteados = sorteador.Sortear();

        // O Microsoft.Data.Sqlite espera a trava pelo tempo do comando, que vem do
        // DefaultTimeout da conexão (inclusive no BEGIN e no COMMIT).
        var sqlite = db.Database.GetDbConnection() as SqliteConnection;
        var esperaAnterior = sqlite?.DefaultTimeout;
        if (sqlite != null)
        {
            sqlite.DefaultTimeout = EsperaTravaSegundos;
        }

        var abriu = false;
        try
        {
            var resultado = await GravarEmTransacaoAsync(id, sorteados, db, gerador, cancelamento);
            abriu = resultado is IStatusCodeHttpResult { StatusCode: StatusCodes.Status200OK };
            return resultado;
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
            if (!abriu)
            {
                recarga.Liberar(id);
            }

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

        var estado = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { personagemId }, cancelamento);
        var capacidade = Bolsa.CapacidadeDe(estado);
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
        var estado = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { personagemId }, cancelamento);
        Bolsa.Normalizar(itens, Bolsa.CapacidadeDe(estado));
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

    private static async Task<IResult> ExpandirBolsaAsync(
        string personagemId,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        ICarteiraCoroas carteira,
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
        var estado = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { id }, cancelamento);
        estado ??= new EstadoEquipamentos { PersonagemId = id };
        if (db.Entry(estado).State == EntityState.Detached)
        {
            db.Add(estado);
        }

        if (estado.AbasBolsa >= ConfigBolsa.AbasMaximas)
        {
            return RespostasApi.Erro(
                StatusCodes.Status400BadRequest,
                CodigosErroItens.BolsaNoLimite
            );
        }

        var custo = ConfigBolsa.CustoPorAba(estado.AbasBolsa + 1);
        if (custo > 0 && !await carteira.DebitarAsync(id, custo, cancelamento))
        {
            return RespostasApi.Erro(
                StatusCodes.Status409Conflict,
                CodigosErroItens.CoroasInsuficientes
            );
        }

        estado.AbasBolsa++;
        await db.SaveChangesAsync(cancelamento);

        var capacidade = Bolsa.CapacidadeDe(estado);
        var itens = await db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == id)
            .OrderBy(i => i.Id)
            .ToListAsync(cancelamento);
        Bolsa.Normalizar(itens, capacidade);

        var resposta = EquipamentosRotas.Resposta(
            itens,
            estado.ConjuntoAtivo,
            await NivelAsync(id, db, cancelamento),
            capacidade
        );
        resposta.Coroas = await carteira.SaldoAsync(id, cancelamento);
        resposta.Aviso = $"Bolsa expandida para {estado.AbasBolsa} abas.";

        await transacao.CommitAsync(cancelamento);
        return Results.Ok(resposta);
    }

    private static async Task<IResult> BancoAsync(
        string personagemId,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        ICarteiraCoroas carteira,
        CancellationToken cancelamento
    )
    {
        var contaId = usuario.ObterContaId();
        if (!await PersonagemDaContaAsync(personagemId, contaId, db, cancelamento))
        {
            return PersonagemNaoEncontrado();
        }

        var id = ParaId(personagemId);
        var resposta = await MontarBancoResponseAsync(
            contaId,
            id,
            db,
            carteira,
            null,
            cancelamento
        );
        return Results.Ok(resposta);
    }

    private static async Task<IResult> DepositarBancoAsync(
        string personagemId,
        PedidoDepositarBanco pedido,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        ICarteiraCoroas carteira,
        CancellationToken cancelamento
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );
        var contaId = usuario.ObterContaId();
        if (!await PersonagemDaContaAsync(personagemId, contaId, db, cancelamento))
        {
            return PersonagemNaoEncontrado();
        }

        var id = ParaId(personagemId);
        var estadoBanco = await db.Set<EstadoBanco>()
            .FindAsync(new object[] { contaId }, cancelamento);
        var capacidadeBanco = Banco.CapacidadeDe(estadoBanco);
        var itensBanco = await db.Set<ItemBanco>()
            .Where(i => i.ContaId == contaId)
            .OrderBy(i => i.Id)
            .ToListAsync(cancelamento);
        Banco.Normalizar(itensBanco, capacidadeBanco);

        var estadoBolsa = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { id }, cancelamento);
        var capacidadeBolsa = Bolsa.CapacidadeDe(estadoBolsa);
        var itensBolsa = await db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == id)
            .OrderBy(i => i.Id)
            .ToListAsync(cancelamento);
        Bolsa.Normalizar(itensBolsa, capacidadeBolsa);

        var alvo = itensBolsa.SingleOrDefault(i => i.Id == pedido.InstanciaId);
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

        int destinoAba;
        int destinoCasa;
        if (pedido.TemDestino)
        {
            if (!capacidadeBanco.Contem(pedido.Aba, pedido.Casa))
            {
                return RespostasApi.Erro(
                    StatusCodes.Status400BadRequest,
                    CodigosErroItens.CasaInvalida
                );
            }

            destinoAba = pedido.Aba;
            destinoCasa = pedido.Casa;
        }
        else
        {
            var livre = Banco.PrimeiraLivre(itensBanco, capacidadeBanco);
            if (livre == null)
            {
                return RespostasApi.Erro(
                    StatusCodes.Status409Conflict,
                    CodigosErroItens.BancoCheio,
                    "Banco cheio."
                );
            }

            destinoAba = livre.Value.Aba;
            destinoCasa = livre.Value.Casa;
        }

        var ocupanteBanco = Banco.Em(itensBanco, destinoAba, destinoCasa);
        if (ocupanteBanco != null)
        {
            var abaOrigem = alvo.Aba ?? 0;
            var casaOrigem = alvo.Casa ?? 0;

            db.Remove(ocupanteBanco);
            db.Remove(alvo);
            await db.SaveChangesAsync(cancelamento);

            var novoBolsa = new ItemInventario
            {
                PersonagemId = id,
                ItemId = ocupanteBanco.ItemId,
                Posicao = PosicaoEquipamento.Inventario,
                Conteiner = ConteinerItem.Bolsa,
                Aba = abaOrigem,
                Casa = casaOrigem,
                EquipamentoJson = ocupanteBanco.EquipamentoJson,
            };
            var novoBanco = new ItemBanco
            {
                ContaId = contaId,
                ItemId = alvo.ItemId,
                Aba = destinoAba,
                Casa = destinoCasa,
                EquipamentoJson = alvo.EquipamentoJson,
            };
            db.Add(novoBolsa);
            db.Add(novoBanco);
            await db.SaveChangesAsync(cancelamento);
        }
        else
        {
            db.Remove(alvo);
            await db.SaveChangesAsync(cancelamento);

            var novoBanco = new ItemBanco
            {
                ContaId = contaId,
                ItemId = alvo.ItemId,
                Aba = destinoAba,
                Casa = destinoCasa,
                EquipamentoJson = alvo.EquipamentoJson,
            };
            db.Add(novoBanco);
            await db.SaveChangesAsync(cancelamento);
        }

        var resposta = await MontarBancoResponseAsync(
            contaId,
            id,
            db,
            carteira,
            null,
            cancelamento
        );
        await transacao.CommitAsync(cancelamento);
        return Results.Ok(resposta);
    }

    private static async Task<IResult> RetirarBancoAsync(
        string personagemId,
        PedidoRetirarBanco pedido,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        ICarteiraCoroas carteira,
        CancellationToken cancelamento
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );
        var contaId = usuario.ObterContaId();
        if (!await PersonagemDaContaAsync(personagemId, contaId, db, cancelamento))
        {
            return PersonagemNaoEncontrado();
        }

        var id = ParaId(personagemId);
        var estadoBanco = await db.Set<EstadoBanco>()
            .FindAsync(new object[] { contaId }, cancelamento);
        var capacidadeBanco = Banco.CapacidadeDe(estadoBanco);
        var itensBanco = await db.Set<ItemBanco>()
            .Where(i => i.ContaId == contaId)
            .OrderBy(i => i.Id)
            .ToListAsync(cancelamento);
        Banco.Normalizar(itensBanco, capacidadeBanco);

        var alvo = itensBanco.SingleOrDefault(i => i.Id == pedido.InstanciaId);
        if (alvo == null)
        {
            return RespostasApi.Erro(
                StatusCodes.Status404NotFound,
                CodigosErroItens.ItemNaoEncontrado
            );
        }

        var estadoBolsa = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { id }, cancelamento);
        var capacidadeBolsa = Bolsa.CapacidadeDe(estadoBolsa);
        var itensBolsa = await db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == id)
            .OrderBy(i => i.Id)
            .ToListAsync(cancelamento);
        Bolsa.Normalizar(itensBolsa, capacidadeBolsa);

        int destinoAba;
        int destinoCasa;
        if (pedido.TemDestino)
        {
            if (!capacidadeBolsa.Contem(pedido.Aba, pedido.Casa))
            {
                return RespostasApi.Erro(
                    StatusCodes.Status400BadRequest,
                    CodigosErroItens.CasaInvalida
                );
            }

            destinoAba = pedido.Aba;
            destinoCasa = pedido.Casa;
        }
        else
        {
            var livre = Bolsa.PrimeiraLivre(itensBolsa, capacidadeBolsa);
            if (livre == null)
            {
                return RespostasApi.Erro(
                    StatusCodes.Status409Conflict,
                    CodigosErroItens.InventarioCheio,
                    "Inventário cheio."
                );
            }

            destinoAba = livre.Value.Aba;
            destinoCasa = livre.Value.Casa;
        }

        var ocupanteBolsa = Bolsa.Em(itensBolsa, destinoAba, destinoCasa);
        if (ocupanteBolsa != null)
        {
            var abaOrigem = alvo.Aba ?? 0;
            var casaOrigem = alvo.Casa ?? 0;

            db.Remove(ocupanteBolsa);
            db.Remove(alvo);
            await db.SaveChangesAsync(cancelamento);

            var novoBanco = new ItemBanco
            {
                ContaId = contaId,
                ItemId = ocupanteBolsa.ItemId,
                Aba = abaOrigem,
                Casa = casaOrigem,
                EquipamentoJson = ocupanteBolsa.EquipamentoJson,
            };
            var novoBolsa = new ItemInventario
            {
                PersonagemId = id,
                ItemId = alvo.ItemId,
                Posicao = PosicaoEquipamento.Inventario,
                Conteiner = ConteinerItem.Bolsa,
                Aba = destinoAba,
                Casa = destinoCasa,
                EquipamentoJson = alvo.EquipamentoJson,
            };
            db.Add(novoBanco);
            db.Add(novoBolsa);
            await db.SaveChangesAsync(cancelamento);
        }
        else
        {
            db.Remove(alvo);
            await db.SaveChangesAsync(cancelamento);

            var novoBolsa = new ItemInventario
            {
                PersonagemId = id,
                ItemId = alvo.ItemId,
                Posicao = PosicaoEquipamento.Inventario,
                Conteiner = ConteinerItem.Bolsa,
                Aba = destinoAba,
                Casa = destinoCasa,
                EquipamentoJson = alvo.EquipamentoJson,
            };
            db.Add(novoBolsa);
            await db.SaveChangesAsync(cancelamento);
        }

        var resposta = await MontarBancoResponseAsync(
            contaId,
            id,
            db,
            carteira,
            null,
            cancelamento
        );
        await transacao.CommitAsync(cancelamento);
        return Results.Ok(resposta);
    }

    private static async Task<IResult> MoverBancoAsync(
        string personagemId,
        PedidoMoverBanco pedido,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        ICarteiraCoroas carteira,
        CancellationToken cancelamento
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );
        var contaId = usuario.ObterContaId();
        if (!await PersonagemDaContaAsync(personagemId, contaId, db, cancelamento))
        {
            return PersonagemNaoEncontrado();
        }

        var id = ParaId(personagemId);
        var estadoBanco = await db.Set<EstadoBanco>()
            .FindAsync(new object[] { contaId }, cancelamento);
        var capacidadeBanco = Banco.CapacidadeDe(estadoBanco);
        if (!capacidadeBanco.Contem(pedido.Aba, pedido.Casa))
        {
            return RespostasApi.Erro(
                StatusCodes.Status400BadRequest,
                CodigosErroItens.CasaInvalida
            );
        }

        var itensBanco = await db.Set<ItemBanco>()
            .Where(i => i.ContaId == contaId)
            .OrderBy(i => i.Id)
            .ToListAsync(cancelamento);
        var alvo = itensBanco.SingleOrDefault(i => i.Id == pedido.InstanciaId);
        if (alvo == null)
        {
            return RespostasApi.Erro(
                StatusCodes.Status404NotFound,
                CodigosErroItens.ItemNaoEncontrado
            );
        }

        Banco.Normalizar(itensBanco, capacidadeBanco);
        var ocupante = Banco.Em(itensBanco, pedido.Aba, pedido.Casa);
        if (ocupante?.Id != alvo.Id)
        {
            var abaOrigem = alvo.Aba;
            var casaOrigem = alvo.Casa;
            Banco.Limpar(alvo);
            await db.SaveChangesAsync(cancelamento);
            if (ocupante != null && abaOrigem is int aba && casaOrigem is int casa)
            {
                Banco.Colocar(ocupante, aba, casa);
                await db.SaveChangesAsync(cancelamento);
            }
            Banco.Colocar(alvo, pedido.Aba, pedido.Casa);
            await db.SaveChangesAsync(cancelamento);
        }

        var resposta = await MontarBancoResponseAsync(
            contaId,
            id,
            db,
            carteira,
            null,
            cancelamento
        );
        await transacao.CommitAsync(cancelamento);
        return Results.Ok(resposta);
    }

    private static async Task<IResult> ExpandirBancoAsync(
        string personagemId,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        ICarteiraCoroas carteira,
        CancellationToken cancelamento
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );
        var contaId = usuario.ObterContaId();
        if (!await PersonagemDaContaAsync(personagemId, contaId, db, cancelamento))
        {
            return PersonagemNaoEncontrado();
        }

        var id = ParaId(personagemId);
        var estadoBanco = await db.Set<EstadoBanco>()
            .FindAsync(new object[] { contaId }, cancelamento);
        estadoBanco ??= new EstadoBanco { ContaId = contaId };
        if (db.Entry(estadoBanco).State == EntityState.Detached)
        {
            db.Add(estadoBanco);
        }

        if (estadoBanco.Abas >= RegrasBanco.AbasBancoMaximas)
        {
            return RespostasApi.Erro(
                StatusCodes.Status400BadRequest,
                CodigosErroItens.BancoNoLimite
            );
        }

        var custo = RegrasBanco.CustoExpansaoBancoCoroas;
        if (custo > 0 && !await carteira.DebitarAsync(id, custo, cancelamento))
        {
            return RespostasApi.Erro(
                StatusCodes.Status409Conflict,
                CodigosErroItens.CoroasInsuficientes
            );
        }

        estadoBanco.Abas++;
        await db.SaveChangesAsync(cancelamento);

        var resposta = await MontarBancoResponseAsync(
            contaId,
            id,
            db,
            carteira,
            $"Banco expandido para {estadoBanco.Abas} abas.",
            cancelamento
        );
        await transacao.CommitAsync(cancelamento);
        return Results.Ok(resposta);
    }

    private static async Task<BancoResponse> MontarBancoResponseAsync(
        long contaId,
        long personagemId,
        JogoDbContext db,
        ICarteiraCoroas carteira,
        string? aviso,
        CancellationToken cancelamento
    )
    {
        var estadoBanco = await db.Set<EstadoBanco>()
            .FindAsync(new object[] { contaId }, cancelamento);
        var capacidadeBanco = Banco.CapacidadeDe(estadoBanco);
        var itensBanco = await db.Set<ItemBanco>()
            .Where(i => i.ContaId == contaId)
            .OrderBy(i => i.Id)
            .ToListAsync(cancelamento);
        Banco.Normalizar(itensBanco, capacidadeBanco);

        var estadoBolsa = await db.Set<EstadoEquipamentos>()
            .FindAsync(new object[] { personagemId }, cancelamento);
        var capacidadeBolsa = Bolsa.CapacidadeDe(estadoBolsa);
        var itensBolsa = await db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == personagemId)
            .OrderBy(i => i.Id)
            .ToListAsync(cancelamento);
        Bolsa.Normalizar(itensBolsa, capacidadeBolsa);

        var bolsaResponse = EquipamentosRotas.Resposta(
            itensBolsa,
            estadoBolsa?.ConjuntoAtivo ?? 1,
            await NivelAsync(personagemId, db, cancelamento),
            capacidadeBolsa
        );
        var saldo = await carteira.SaldoAsync(personagemId, cancelamento);
        bolsaResponse.Coroas = saldo;

        return new BancoResponse
        {
            Itens = itensBanco.Select(i => i.ParaEntrada()).ToArray(),
            Capacidade = capacidadeBanco,
            Coroas = saldo,
            Aviso = aviso ?? "",
            Bolsa = bolsaResponse,
        };
    }

    private static IResult PersonagemNaoEncontrado() =>
        RespostasApi.Erro(StatusCodes.Status404NotFound, CodigosErroItens.PersonagemNaoEncontrado);
}
