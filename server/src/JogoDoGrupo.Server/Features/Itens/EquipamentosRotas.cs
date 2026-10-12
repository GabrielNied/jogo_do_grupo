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
                    GeradorEquipamentos gerador,
                    CancellationToken c
                ) => Executar(personagemId, usuario, db, null, pedido, null, gerador, c)
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
        var capacidade = Bolsa.CapacidadeDe(id);
        var itens = await db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == id)
            .OrderBy(i => i.Id)
            .ToListAsync(c);
        Bolsa.Normalizar(itens, capacidade);
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
            if (desequipar.TemDestino)
            {
                if (!capacidade.Contem(desequipar.Aba, desequipar.Casa))
                    return Erro(CodigosErroItens.CasaInvalida, 400);
                var ocupante = Bolsa.Em(itens, desequipar.Aba, desequipar.Casa);
                if (ocupante != null)
                {
                    // Casa ocupada: o ocupante vai para o encaixe e a peça vestida ocupa a casa
                    // dele. Se ele não couber no encaixe, nada muda.
                    var recusa = await EquiparAsync(
                        db,
                        itens,
                        ocupante,
                        desequipar.Posicao,
                        id,
                        capacidade,
                        gerador,
                        c
                    );
                    if (recusa != null)
                        return recusa;
                }
                else
                {
                    Bolsa.Colocar(item, desequipar.Aba, desequipar.Casa);
                }
            }
            else
            {
                var livre = Bolsa.PrimeiraLivre(itens, capacidade);
                if (livre == null)
                    return Erro(CodigosErroItens.InventarioCheio);
                Bolsa.Colocar(item, livre.Value.Aba, livre.Value.Casa);
            }
        }
        if (equipar != null)
        {
            var item = itens.SingleOrDefault(i => i.Id == equipar.InstanciaId);
            if (item == null)
                return Erro("ItemNaoEncontrado", 404);
            var recusa = await EquiparAsync(
                db,
                itens,
                item,
                equipar.Posicao,
                id,
                capacidade,
                gerador,
                c
            );
            if (recusa != null)
                return recusa;
        }
        await db.SaveChangesAsync(c);
        var resposta = Resposta(itens, ativos);
        await transacao.CommitAsync(c);
        return Results.Ok(resposta);
    }

    /// <summary>
    /// Veste <paramref name="item"/> em <paramref name="posicao"/>. As peças deslocadas voltam
    /// para a bolsa: a primeira na casa que o item deixou, as outras na primeira casa livre.
    /// Devolve a recusa (nada muda) ou <c>null</c> se deu certo.
    /// </summary>
    private static async Task<IResult?> EquiparAsync(
        JogoDbContext db,
        List<ItemInventario> itens,
        ItemInventario item,
        PosicaoEquipamento posicao,
        long personagemId,
        CapacidadeBolsa capacidade,
        GeradorEquipamentos? gerador,
        CancellationToken c
    )
    {
        var dados = item.LerEquipamento() ?? gerador?.Gerar(item.ItemId, Random.Shared);
        if (dados == null)
            return Erro("ItemNaoEquipavel", 400);
        if (!RegrasEquipamentos.Aceita(dados.Tipo, posicao))
            return Erro("PosicaoInvalida", 400);
        if (dados.NivelPersonagemMinimo > await ItensRotas.NivelAsync(personagemId, db, c))
            return Erro("NivelInsuficiente");
        var numero = RegrasEquipamentos.Conjunto(posicao);
        var deslocados = itens.Where(i => i.Id != item.Id && i.Posicao == posicao).ToList();
        if (dados.DuasMaos)
            deslocados.AddRange(
                itens.Where(i => i.Posicao == RegrasEquipamentos.Secundaria(numero))
            );
        if (posicao == RegrasEquipamentos.Secundaria(numero))
        {
            var principal = itens.SingleOrDefault(i =>
                i.Posicao == RegrasEquipamentos.Principal(numero)
            );
            if (principal?.LerEquipamento()?.DuasMaos == true)
                return Erro("ArmaDeDuasMaos");
        }
        deslocados = deslocados.Distinct().ToList();
        var vinhaDaBolsa = Bolsa.EstaNaBolsa(item);
        if (deslocados.Count > Bolsa.Livres(itens, capacidade) + (vinhaDaBolsa ? 1 : 0))
            return Erro(CodigosErroItens.InventarioCheio);

        (int Aba, int Casa)? casaLiberada =
            vinhaDaBolsa && item.Aba is int aba && item.Casa is int casa ? (aba, casa) : null;
        // Libera encaixes e casas antes de mover: o índice único protege também outras instâncias
        // do servidor.
        foreach (var deslocado in deslocados)
        {
            deslocado.Posicao = PosicaoEquipamento.Inventario;
            Bolsa.Limpar(deslocado);
        }
        item.Posicao = PosicaoEquipamento.Inventario;
        Bolsa.Limpar(item);
        await db.SaveChangesAsync(c);

        item.GravarEquipamento(dados);
        item.Posicao = posicao;
        var ocupadas = Bolsa.CasasOcupadas(itens);
        foreach (var deslocado in deslocados)
        {
            var destino =
                casaLiberada != null && !ocupadas.Contains(casaLiberada.Value)
                    ? casaLiberada
                    : Bolsa.PrimeiraLivre(ocupadas, capacidade);
            Bolsa.Colocar(deslocado, destino!.Value.Aba, destino.Value.Casa);
            ocupadas.Add((destino.Value.Aba, destino.Value.Casa));
        }
        return null;
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
        var capacidade = Bolsa.CapacidadeDe(id);
        var itens = await db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == id)
            .OrderBy(i => i.Id)
            .ToListAsync(c);
        Bolsa.Normalizar(itens, capacidade);
        var modelos = CatalogoEquipamentos.ModelosDeTeste;
        var novo = new ItemInventario
        {
            PersonagemId = id,
            ItemId = modelos[Random.Shared.Next(modelos.Length)],
        };
        novo.GravarEquipamento(gerador.Gerar(novo.ItemId, Random.Shared, grau: grauEscolhido)!);
        if (!Bolsa.Guardar(itens, new[] { novo }, capacidade))
            return Erro(CodigosErroItens.InventarioCheio);
        db.Add(novo);
        itens.Add(novo);
        await db.SaveChangesAsync(c);
        var estado = await db.Set<EstadoEquipamentos>().FindAsync(new object[] { id }, c);
        await transacao.CommitAsync(c);
        return Results.Ok(Resposta(itens, estado?.ConjuntoAtivo ?? 1));
    }

    internal static EquipamentosResponse Resposta(IEnumerable<ItemInventario> itens, int conjunto)
    {
        var lista = itens.ToList();
        var capacidade = Bolsa.CapacidadeDe(lista.Count > 0 ? lista[0].PersonagemId : 0);
        Bolsa.Normalizar(lista, capacidade);
        var entradas = lista.Select(i => i.ParaEntrada()).ToArray();
        var equipados = entradas.Where(i => i.Posicao != PosicaoEquipamento.Inventario).ToArray();
        return new EquipamentosResponse
        {
            Inventario = entradas.Where(i => i.Posicao == PosicaoEquipamento.Inventario).ToArray(),
            Equipados = equipados,
            Capacidade = capacidade,
            ConjuntoAtivo = conjunto,
            Bonus = RegrasEquipamentos.Somar(equipados, conjunto),
        };
    }
}
