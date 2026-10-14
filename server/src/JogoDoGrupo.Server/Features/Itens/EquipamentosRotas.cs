using System.Data;
using System.Security.Claims;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

internal static class EquipamentosRotas
{
    public static void Mapear(IEndpointRouteBuilder rotas)
    {
        FusaoRotas.Mapear(rotas);
        rotas
            .MapGet(
                RegrasEquipamentos.Rota,
                (
                    string personagemId,
                    ClaimsPrincipal usuario,
                    JogoDbContext db,
                    CancellationToken c
                ) => Executar(personagemId, usuario, db, null, null, null, null, null, null, c)
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
                    VisualEquipadoServico visual,
                    EventosDoServidor eventos,
                    CancellationToken c
                ) =>
                    Executar(
                        personagemId,
                        usuario,
                        db,
                        pedido,
                        null,
                        null,
                        gerador,
                        visual,
                        eventos,
                        c
                    )
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
                    VisualEquipadoServico visual,
                    EventosDoServidor eventos,
                    CancellationToken c
                ) =>
                    Executar(
                        personagemId,
                        usuario,
                        db,
                        null,
                        pedido,
                        null,
                        gerador,
                        visual,
                        eventos,
                        c
                    )
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
                    VisualEquipadoServico visual,
                    EventosDoServidor eventos,
                    CancellationToken c
                ) =>
                    Executar(
                        personagemId,
                        usuario,
                        db,
                        null,
                        null,
                        pedido,
                        null,
                        visual,
                        eventos,
                        c
                    )
            )
            .RequireAuthorization();
        if (rotas.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            rotas.MapPost(RegrasEquipamentos.Rota + "/bau-teste", BauAsync).RequireAuthorization();
            rotas
                .MapPost(RegrasEquipamentos.Rota + "/limpar-teste", LimparTesteAsync)
                .RequireAuthorization();
        }
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
        VisualEquipadoServico? visual,
        EventosDoServidor? eventos,
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
        var estado = await db.Set<EstadoEquipamentos>().FindAsync(new object[] { id }, c);
        var capacidade = Bolsa.CapacidadeDe(estado);
        var itens = await db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == id)
            .OrderBy(i => i.Id)
            .ToListAsync(c);
        Bolsa.Normalizar(itens, capacidade);
        var ativos = estado?.ConjuntoAtivo ?? 1;
        if (conjunto != null)
        {
            if (conjunto.Conjunto != 1 && conjunto.Conjunto != 2)
                return Erro(CodigosErroItens.ConjuntoInvalido, 400);
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
                return Erro(CodigosErroItens.PosicaoInvalida, 400);
            var item = itens.SingleOrDefault(i => i.Posicao == desequipar.Posicao);
            if (item == null)
                return Erro(CodigosErroItens.ItemNaoEncontrado, 404);
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
                return Erro(CodigosErroItens.ItemNaoEncontrado, 404);
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
        var resposta = Resposta(itens, ativos, await ItensRotas.NivelAsync(id, db, c), capacidade);
        await transacao.CommitAsync(c);
        // Só depois de gravado: a área vê o estado inteiro do que o servidor aceitou.
        if (visual != null && (equipar != null || desequipar != null || conjunto != null))
        {
            visual.Publicar(RegrasVisualEquipado.Montar(id, ativos, resposta.Equipados));
            eventos?.Publicar(new EquipamentoMudou(id));
        }
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
            return Erro(CodigosErroItens.ItemNaoEquipavel, 400);
        var numero = RegrasEquipamentos.Conjunto(posicao);
        // A regra de recusa é a do shared: a mesma que a tela usa antes do clique.
        var principalDoConjunto =
            numero != 0 && posicao == RegrasEquipamentos.Secundaria(numero)
                ? itens
                    .SingleOrDefault(i => i.Posicao == RegrasEquipamentos.Principal(numero))
                    ?.LerEquipamento()
                : null;
        var recusaDaRegra = RegrasEquipar.Recusa(
            dados,
            posicao,
            await ItensRotas.NivelAsync(personagemId, db, c),
            principalDoConjunto
        );
        if (recusaDaRegra != null)
            return Erro(
                recusaDaRegra,
                recusaDaRegra == CodigosErroItens.PosicaoInvalida ? 400 : 409
            );
        var deslocados = itens.Where(i => i.Id != item.Id && i.Posicao == posicao).ToList();
        if (dados.DuasMaos)
            deslocados.AddRange(
                itens.Where(i => i.Posicao == RegrasEquipamentos.Secundaria(numero))
            );
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
        var estado = await db.Set<EstadoEquipamentos>().FindAsync(new object[] { id }, c);
        var capacidade = Bolsa.CapacidadeDe(estado);
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
        await transacao.CommitAsync(c);
        return Results.Ok(
            Resposta(
                itens,
                estado?.ConjuntoAtivo ?? 1,
                await ItensRotas.NivelAsync(id, db, c),
                capacidade
            )
        );
    }

    private static async Task<IResult> LimparTesteAsync(
        string personagemId,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        IConfiguration configuracao,
        IHostEnvironment ambiente,
        VisualEquipadoServico visual,
        EventosDoServidor eventos,
        CancellationToken c
    )
    {
        var contaId = usuario.ObterContaId();
        var contas = configuracao.GetSection("Desenvolvimento:Contas").Get<long[]>() ?? [];
        if (
            !ambiente.IsDevelopment()
            || !configuracao.GetValue<bool>("Desenvolvimento:PermitirComandos")
            || (contas.Length > 0 && !contas.Contains(contaId))
        )
            return Erro("ComandoDeTesteDesligado", 403);

        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            c
        );
        if (!await ItensRotas.PersonagemDaContaAsync(personagemId, contaId, db, c))
            return Erro(CodigosErroItens.PersonagemNaoEncontrado, 404);

        var id = long.Parse(personagemId, System.Globalization.CultureInfo.InvariantCulture);
        var itens = await db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == id)
            .OrderBy(i => i.Id)
            .ToListAsync(c);
        var estado = await db.Set<EstadoEquipamentos>().FindAsync(new object[] { id }, c);
        var conjunto = estado?.ConjuntoAtivo ?? 1;
        db.RemoveRange(itens);
        await db.SaveChangesAsync(c);
        await transacao.CommitAsync(c);

        visual.Publicar(
            RegrasVisualEquipado.Montar(id, conjunto, Array.Empty<EntradaInventario>())
        );
        eventos.Publicar(new EquipamentoMudou(id));
        return Results.Ok(
            new EquipamentosResponse
            {
                Inventario = Array.Empty<EntradaInventario>(),
                Equipados = Array.Empty<EntradaInventario>(),
                Capacidade = Bolsa.CapacidadeDe(estado),
                ConjuntoAtivo = conjunto,
                Atributos = RegrasAtributosDeItem.Resumir(
                    Array.Empty<EntradaInventario>(),
                    conjunto
                ),
            }
        );
    }

    internal static EquipamentosResponse Resposta(
        IEnumerable<ItemInventario> itens,
        int conjunto,
        int nivelDoPersonagem,
        CapacidadeBolsa? capacidade = null
    )
    {
        var lista = itens.ToList();
        capacidade ??= Bolsa.CapacidadeDe(lista.Count > 0 ? lista[0].PersonagemId : 0);
        Bolsa.Normalizar(lista, capacidade);
        var entradas = lista.Select(i => i.ParaEntrada()).ToArray();
        var equipados = entradas.Where(i => i.Posicao != PosicaoEquipamento.Inventario).ToArray();
        return new EquipamentosResponse
        {
            Inventario = entradas.Where(i => i.Posicao == PosicaoEquipamento.Inventario).ToArray(),
            Equipados = equipados,
            Capacidade = capacidade,
            ConjuntoAtivo = conjunto,
            NivelDoPersonagem = nivelDoPersonagem,
            Atributos = RegrasAtributosDeItem.Resumir(equipados, conjunto),
        };
    }
}
