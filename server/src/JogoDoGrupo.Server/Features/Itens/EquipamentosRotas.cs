using System.Data;
using System.Security.Claims;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

internal static class EquipamentosRotas
{
    public static void Mapear(IEndpointRouteBuilder rotas)
    {
        InfusaoRotas.Mapear(rotas);
        rotas
            .MapGet(
                RegrasEquipamentos.Rota,
                (
                    string personagemId,
                    ClaimsPrincipal usuario,
                    JogoDbContext db,
                    CancellationToken c
                ) => Executar(personagemId, usuario, db, null, null, null, null, c)
            )
            .RequireAuthorization();
        rotas
            .MapPost(
                RegrasEquipamentos.Rota + "/equipar",
                (
                    string personagemId,
                    PedidoEquipar pedido,
                    ClaimsPrincipal usuario,
                    JogoDbContext db,
                    GeradorEquipamentos gerador,
                    CancellationToken c
                ) => Executar(personagemId, usuario, db, pedido, null, null, gerador, c)
            )
            .RequireAuthorization();
        rotas
            .MapPost(
                RegrasEquipamentos.Rota + "/desequipar",
                (
                    string personagemId,
                    PedidoDesequipar pedido,
                    ClaimsPrincipal usuario,
                    JogoDbContext db,
                    CancellationToken c
                ) => Executar(personagemId, usuario, db, null, pedido, null, null, c)
            )
            .RequireAuthorization();
        rotas
            .MapPost(
                RegrasEquipamentos.Rota + "/conjunto",
                (
                    string personagemId,
                    PedidoConjunto pedido,
                    ClaimsPrincipal usuario,
                    JogoDbContext db,
                    CancellationToken c
                ) => Executar(personagemId, usuario, db, null, null, pedido, null, c)
            )
            .RequireAuthorization();
        if (rotas.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment())
            rotas.MapPost(RegrasEquipamentos.Rota + "/bau-teste", BauAsync).RequireAuthorization();
    }

    private static IResult Erro(string codigo, int status = 409) =>
        RespostasApi.Erro(status, codigo);

    private static async Task<IResult> Executar(
        string personagemId,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        PedidoEquipar? equipar,
        PedidoDesequipar? desequipar,
        PedidoConjunto? conjunto,
        GeradorEquipamentos? gerador,
        CancellationToken c
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            c
        );
        if (!await ItensRotas.PersonagemDaContaAsync(personagemId, usuario.ObterContaId(), db, c))
            return Erro(CodigosErroItens.PersonagemNaoEncontrado, 404);
        var id = long.Parse(personagemId, System.Globalization.CultureInfo.InvariantCulture);
        var itens = await db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == id)
            .OrderBy(i => i.Id)
            .ToListAsync(c);
        var estado = await db.Set<EstadoEquipamentos>().FindAsync(new object[] { id }, c);
        var ativos = estado?.ConjuntoAtivo ?? 1;
        if (conjunto != null)
        {
            if (conjunto.Conjunto != 1 && conjunto.Conjunto != 2)
                return Erro("ConjuntoInvalido", 400);
            estado ??= new EstadoEquipamentos { PersonagemId = id };
            if (db.Entry(estado).State == EntityState.Detached)
                db.Add(estado);
            ativos = estado.ConjuntoAtivo = conjunto.Conjunto;
        }
        if (desequipar != null)
        {
            if (
                !Enum.IsDefined(desequipar.Posicao)
                || desequipar.Posicao == PosicaoEquipamento.Inventario
            )
                return Erro("PosicaoInvalida", 400);
            var item = itens.SingleOrDefault(i => i.Posicao == desequipar.Posicao);
            if (item == null)
                return Erro("ItemNaoEncontrado", 404);
            if (
                itens.Count(i => i.Posicao == PosicaoEquipamento.Inventario)
                >= RegrasItens.EspacosInventario
            )
                return Erro(CodigosErroItens.InventarioCheio);
            item.Posicao = PosicaoEquipamento.Inventario;
        }
        if (equipar != null)
        {
            var item = itens.SingleOrDefault(i => i.Id == equipar.InstanciaId);
            if (item == null)
                return Erro("ItemNaoEncontrado", 404);
            var dados = item.LerEquipamento() ?? gerador!.Gerar(item.ItemId, Random.Shared);
            if (dados == null)
                return Erro("ItemNaoEquipavel", 400);
            if (!RegrasEquipamentos.Aceita(dados.Tipo, equipar.Posicao))
                return Erro("PosicaoInvalida", 400);
            if (dados.NivelPersonagemMinimo > await ItensRotas.NivelAsync(id, db, c))
                return Erro("NivelInsuficiente");
            var numero = RegrasEquipamentos.Conjunto(equipar.Posicao);
            var deslocados = itens
                .Where(i => i.Id != item.Id && i.Posicao == equipar.Posicao)
                .ToList();
            if (dados.DuasMaos)
                deslocados.AddRange(
                    itens.Where(i => i.Posicao == RegrasEquipamentos.Secundaria(numero))
                );
            if (equipar.Posicao == RegrasEquipamentos.Secundaria(numero))
            {
                var principal = itens.SingleOrDefault(i =>
                    i.Posicao == RegrasEquipamentos.Principal(numero)
                );
                if (principal?.LerEquipamento()?.DuasMaos == true)
                    return Erro("ArmaDeDuasMaos");
            }
            var espacos =
                itens.Count(i => i.Posicao == PosicaoEquipamento.Inventario)
                - (item.Posicao == PosicaoEquipamento.Inventario ? 1 : 0)
                + deslocados.Distinct().Count();
            if (espacos > RegrasItens.EspacosInventario)
                return Erro(CodigosErroItens.InventarioCheio);
            // Libera posições antes de mover: o índice único protege também outras instâncias do servidor.
            foreach (var deslocado in deslocados)
                deslocado.Posicao = PosicaoEquipamento.Inventario;
            item.Posicao = PosicaoEquipamento.Inventario;
            await db.SaveChangesAsync(c);
            item.GravarEquipamento(dados);
            item.Posicao = equipar.Posicao;
        }
        await db.SaveChangesAsync(c);
        var resposta = Resposta(itens, ativos);
        await transacao.CommitAsync(c);
        return Results.Ok(resposta);
    }

    private static async Task<IResult> BauAsync(
        string personagemId,
        int? grau,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        GeradorEquipamentos gerador,
        CancellationToken c
    )
    {
        var grauEscolhido = (GrauEquipamento)(grau ?? 0);
        if (!Enum.IsDefined(grauEscolhido))
            return Erro(CodigosErro.RequisicaoInvalida, 400);
        // Recorte de teste, como o baú já existente; não altera a tabela de loot de mobs.
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            c
        );
        if (!await ItensRotas.PersonagemDaContaAsync(personagemId, usuario.ObterContaId(), db, c))
            return Erro(CodigosErroItens.PersonagemNaoEncontrado, 404);
        var id = long.Parse(personagemId, System.Globalization.CultureInfo.InvariantCulture);
        var itens = await db.Set<ItemInventario>().Where(i => i.PersonagemId == id).ToListAsync(c);
        if (
            itens.Count(i => i.Posicao == PosicaoEquipamento.Inventario)
            >= RegrasItens.EspacosInventario
        )
            return Erro(CodigosErroItens.InventarioCheio);
        var modelos = CatalogoEquipamentos.ModelosDeTeste;
        var novo = new ItemInventario
        {
            PersonagemId = id,
            ItemId = modelos[Random.Shared.Next(modelos.Length)],
        };
        novo.GravarEquipamento(gerador.Gerar(novo.ItemId, Random.Shared, grau: grauEscolhido)!);
        db.Add(novo);
        itens.Add(novo);
        await db.SaveChangesAsync(c);
        var estado = await db.Set<EstadoEquipamentos>().FindAsync(new object[] { id }, c);
        await transacao.CommitAsync(c);
        return Results.Ok(Resposta(itens, estado?.ConjuntoAtivo ?? 1));
    }

    internal static EquipamentosResponse Resposta(IEnumerable<ItemInventario> itens, int conjunto)
    {
        var entradas = itens.Select(i => i.ParaEntrada()).ToArray();
        var equipados = entradas.Where(i => i.Posicao != PosicaoEquipamento.Inventario).ToArray();
        return new EquipamentosResponse
        {
            Inventario = entradas.Where(i => i.Posicao == PosicaoEquipamento.Inventario).ToArray(),
            Equipados = equipados,
            ConjuntoAtivo = conjunto,
            Bonus = RegrasEquipamentos.Somar(equipados, conjunto),
        };
    }
}
