using System.Data;
using System.Security.Claims;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Rotas do banco pessoal da conta (<see cref="RotasItens.Banco"/> e associadas). O banco é
/// compartilhado entre os personagens da mesma conta.
/// </summary>
internal static class BancoPessoalRotas
{
    public static void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas.MapGet(RotasItens.Banco, BancoAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.BancoDepositar, DepositarBancoAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.BancoRetirar, RetirarBancoAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.BancoMover, MoverBancoAsync).RequireAuthorization();
        rotas.MapPost(RotasItens.BancoExpandir, ExpandirBancoAsync).RequireAuthorization();
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
        if (!await ItensRotas.PersonagemDaContaAsync(personagemId, contaId, db, cancelamento))
        {
            return ItensRotas.PersonagemNaoEncontrado();
        }

        var id = ItensRotas.ParaId(personagemId);
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
        if (!await ItensRotas.PersonagemDaContaAsync(personagemId, contaId, db, cancelamento))
        {
            return ItensRotas.PersonagemNaoEncontrado();
        }

        var id = ItensRotas.ParaId(personagemId);
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
        if (!await ItensRotas.PersonagemDaContaAsync(personagemId, contaId, db, cancelamento))
        {
            return ItensRotas.PersonagemNaoEncontrado();
        }

        var id = ItensRotas.ParaId(personagemId);
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
        if (!await ItensRotas.PersonagemDaContaAsync(personagemId, contaId, db, cancelamento))
        {
            return ItensRotas.PersonagemNaoEncontrado();
        }

        var id = ItensRotas.ParaId(personagemId);
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
        if (!await ItensRotas.PersonagemDaContaAsync(personagemId, contaId, db, cancelamento))
        {
            return ItensRotas.PersonagemNaoEncontrado();
        }

        var id = ItensRotas.ParaId(personagemId);
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
            await ItensRotas.NivelAsync(personagemId, db, cancelamento),
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
}
