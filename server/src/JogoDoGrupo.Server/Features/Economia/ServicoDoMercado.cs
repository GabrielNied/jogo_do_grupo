using System.Data;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Economia;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Economia;

/// <summary>Relógio do mercado (segundos Unix); os testes avançam o tempo sem esperar 7 dias.</summary>
public sealed class RelogioDoMercado
{
    private long _deslocamentoSegundos;

    public long AgoraSegundos =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Interlocked.Read(ref _deslocamentoSegundos);

    /// <summary>Só para teste: adianta o relógio.</summary>
    public void Avancar(TimeSpan tempo) =>
        Interlocked.Add(ref _deslocamentoSegundos, (long)tempo.TotalSeconds);
}

/// <summary>Uma venda concluída: quem comprou, quem vendeu, o preço e a taxa destruída.</summary>
public sealed record NegocioFeito(
    long VendedorId,
    long CompradorId,
    int ItemId,
    long PrecoUnitario,
    long Taxa
);

/// <summary>Uma ordem que saiu do livro sem casar: venceu ou foi cancelada.</summary>
public sealed record OrdemEncerrada(long PersonagemId, int ItemId, bool Venda, bool Expirou);

/// <summary>Resultado de uma operação do mercado: <see cref="Erro"/> nulo é sucesso.</summary>
public sealed class ResultadoDoMercado
{
    public string? Erro { get; init; }
    public List<NegocioFeito> Negocios { get; } = [];
    public List<OrdemEncerrada> Encerradas { get; } = [];

    /// <summary>Itens que voltaram à bolsa de quem anunciou (cancelar, vencer, receber).</summary>
    public List<(long PersonagemId, int ItemId)> Devolvidos { get; } = [];

    public static ResultadoDoMercado Falha(string erro) => new() { Erro = erro };
}

/// <summary>
/// Regras do mercado (leilão por ordens, #176). O livro é um só: ordens de venda (um exemplar,
/// guardado na ordem) e de compra (um modelo, coroas guardadas na ordem). Cada operação roda
/// numa transação Serializable, uma de cada vez no processo (<see cref="Trava"/>), então item e
/// coroa nunca duplicam nem somem.
///
/// Casamento: a ordem nova casa com a melhor ordem oposta (a venda mais barata ou a compra que
/// paga mais, a mais antiga no empate), pelo preço de quem já estava no livro, nunca com ordem
/// do mesmo personagem. A taxa só vale na venda e sai do vendedor uma vez.
///
/// Devolução (cancelar ou vencer): as coroas voltam direto; o item volta à bolsa. Se a bolsa está
/// cheia, a venda fica cancelada/vencida com o item guardado e a devolução é tentada de novo no
/// login ou no pedido do jogador (<see cref="ReceberDevolucoesAsync"/>). É o único lugar a mudar
/// quando o correio (#212) existir: trocar <see cref="TentarDevolverAsync"/>.
/// </summary>
public sealed class ServicoDoMercado(
    JogoDbContext db,
    ICarteiraCoroas carteira,
    ICustodiaDeItens custodia,
    IDiretorioDePersonagens diretorio,
    RelogioDoMercado relogio,
    IResolvedorDeBarracas? resolvedorBarracas = null
)
{
    /// <summary>
    /// Uma operação por vez no processo: o servidor tem uma só instância, e isto evita disputa de
    /// escrita entre duas ordens ao mesmo tempo (a regra continua valendo se houver mais de uma).
    /// </summary>
    private static readonly SemaphoreSlim Trava = new(1, 1);

    // ── Consulta ──────────────────────────────────────────────────────────────────

    public async Task<EstadoDoMercado> ConsultarAsync(
        long personagemId,
        int itemId,
        string? filtro,
        CancellationToken c
    )
    {
        var agora = relogio.AgoraSegundos;
        var filtroTexto = (filtro ?? "").Trim();

        IQueryable<OrdemMercado> abertas = db.Set<OrdemMercado>()
            .AsNoTracking()
            .Where(o => o.Estado == EstadoDaOrdemDeMercado.Aberta && o.ExpiraEm > agora);
        if (itemId > 0)
            abertas = abertas.Where(o => o.ItemId == itemId);

        var vendas = await abertas
            .Where(o => o.Venda)
            .OrderBy(o => o.PrecoUnitario)
            .ThenBy(o => o.Id)
            .ToListAsync(c);
        var compras = await abertas
            .Where(o => !o.Venda)
            .OrderByDescending(o => o.PrecoUnitario)
            .ThenBy(o => o.Id)
            .ToListAsync(c);
        if (filtroTexto.Length > 0)
        {
            bool Casa(OrdemMercado o) =>
                (ConfigMercado.NomeDoItem(o.ItemId) ?? "").Contains(
                    filtroTexto,
                    StringComparison.OrdinalIgnoreCase
                );
            vendas = vendas.Where(Casa).ToList();
            compras = compras.Where(Casa).ToList();
        }

        var minhas = await db.Set<OrdemMercado>()
            .AsNoTracking()
            .Where(o =>
                o.PersonagemId == personagemId
                && (
                    (o.Estado == EstadoDaOrdemDeMercado.Aberta && o.ExpiraEm > agora)
                    || o.ItemEmCustodia
                )
            )
            .OrderBy(o => o.Id)
            .ToListAsync(c);

        return new EstadoDoMercado
        {
            Saldo = await carteira.SaldoAsync(personagemId, c),
            Vendas = vendas
                .Take(ConfigMercado.LinhasPorLista)
                .Select(o => Para(o, personagemId))
                .ToArray(),
            Compras = compras
                .Take(ConfigMercado.LinhasPorLista)
                .Select(o => Para(o, personagemId))
                .ToArray(),
            Minhas = minhas.Select(o => Para(o, personagemId)).ToArray(),
            Bolsa = (await custodia.ListarNegociaveisAsync(personagemId, c)).ToArray(),
            Modelos = ConfigMercado
                .ItensConhecidos.OrderBy(p => p.Value, StringComparer.CurrentCultureIgnoreCase)
                .Select(p => new ModeloDoMercado { ItemId = p.Key, Nome = p.Value })
                .ToArray(),
        };
    }

    private OrdemDeMercado Para(OrdemMercado o, long personagemId)
    {
        diretorio.TentarObterNome(o.PersonagemId, out var anunciante);
        var barracaDe = "";
        if (
            resolvedorBarracas != null
            && resolvedorBarracas.TentarObterNomeBarraca(o.PersonagemId, out var nomeBarraca)
        )
        {
            barracaDe = nomeBarraca;
        }

        return new OrdemDeMercado
        {
            Id = o.Id,
            Venda = o.Venda,
            ItemId = o.ItemId,
            Nome = ConfigMercado.NomeDoItem(o.ItemId) ?? "",
            Quantidade = o.Quantidade,
            PrecoUnitario = o.PrecoUnitario,
            ExpiraEm = o.ExpiraEm,
            Estado = o.Estado,
            Minha = o.PersonagemId == personagemId,
            AguardaDevolucao = o.ItemEmCustodia && o.Estado != EstadoDaOrdemDeMercado.Aberta,
            Anunciante = anunciante,
            BarracaDe = barracaDe,
            Item =
                o.Venda && o.ItemEmCustodia
                    ? custodia.Descrever(new ItemEmCustodia(o.ItemId, o.EquipamentoJson), o.Id)
                    : null,
        };
    }

    public async Task<List<OrdemDeMercado>> ListarOrdensDaBarracaAsync(
        long vendedorId,
        CancellationToken c
    )
    {
        var agora = relogio.AgoraSegundos;
        var vendas = await db.Set<OrdemMercado>()
            .AsNoTracking()
            .Where(o =>
                o.PersonagemId == vendedorId
                && o.Venda
                && o.Estado == EstadoDaOrdemDeMercado.Aberta
                && o.ExpiraEm > agora
            )
            .OrderBy(o => o.PrecoUnitario)
            .ThenBy(o => o.Id)
            .ToListAsync(c);

        return vendas.Select(o => Para(o, vendedorId)).ToList();
    }

    // ── Ordens ────────────────────────────────────────────────────────────────────

    public async Task<ResultadoDoMercado> OferecerVendaAsync(
        long vendedorId,
        long instanciaId,
        long preco,
        CancellationToken c
    )
    {
        if (!ConfigMercado.PrecoValido(preco))
            return ResultadoDoMercado.Falha(CodigosErroEconomia.PrecoInvalido);

        return await ExecutarAsync(
            async resultado =>
            {
                if (
                    await AbertasDoPersonagemAsync(vendedorId, c)
                    >= ConfigMercado.OrdensAbertasMaximas
                )
                    return CodigosErroEconomia.LimiteDeOrdens;

                var item = await custodia.RetirarAsync(vendedorId, instanciaId, c);
                if (item == null)
                    return CodigosErroEconomia.ItemNaoEncontrado;

                var agora = relogio.AgoraSegundos;
                var venda = new OrdemMercado
                {
                    PersonagemId = vendedorId,
                    Venda = true,
                    ItemId = item.ItemId,
                    Quantidade = 1,
                    PrecoUnitario = preco,
                    Estado = EstadoDaOrdemDeMercado.Aberta,
                    CriadaEm = agora,
                    ExpiraEm = agora + ConfigMercado.ExpiracaoSegundos,
                    ItemEmCustodia = true,
                    EquipamentoJson = item.EquipamentoJson,
                };
                db.Set<OrdemMercado>().Add(venda);
                await db.SaveChangesAsync(c);

                // Casa com as compras que pagam o preço pedido ou mais, a mais alta primeiro.
                var compras = await db.Set<OrdemMercado>()
                    .Where(o =>
                        !o.Venda
                        && o.Estado == EstadoDaOrdemDeMercado.Aberta
                        && o.ItemId == venda.ItemId
                        && o.PrecoUnitario >= preco
                        && o.PersonagemId != vendedorId
                        && o.ExpiraEm > agora
                    )
                    .OrderByDescending(o => o.PrecoUnitario)
                    .ThenBy(o => o.Id)
                    .ToListAsync(c);
                foreach (var compra in compras)
                {
                    // O comprador que estava no livro paga o preço dele (já guardado).
                    if (
                        !await LiquidarAsync(
                            venda,
                            compra.PersonagemId,
                            compra.PrecoUnitario,
                            0,
                            resultado,
                            c
                        )
                    )
                        continue; // bolsa cheia: pula esta compra, ela segue aberta
                    BaixarUnidadeDaCompra(compra, agora);
                    await db.SaveChangesAsync(c);
                    break;
                }

                return null;
            },
            c
        );
    }

    public async Task<ResultadoDoMercado> OferecerCompraAsync(
        long compradorId,
        int itemId,
        int quantidade,
        long precoUnitario,
        CancellationToken c
    )
    {
        if (!ConfigMercado.PrecoValido(precoUnitario))
            return ResultadoDoMercado.Falha(CodigosErroEconomia.PrecoInvalido);
        if (quantidade < 1 || quantidade > ConfigMercado.QuantidadeMaximaDaCompra)
            return ResultadoDoMercado.Falha(CodigosErroEconomia.QuantidadeInvalida);
        if (ConfigMercado.NomeDoItem(itemId) == null)
            return ResultadoDoMercado.Falha(CodigosErroEconomia.ItemNaoNegociavel);

        return await ExecutarAsync(
            async resultado =>
            {
                if (
                    await AbertasDoPersonagemAsync(compradorId, c)
                    >= ConfigMercado.OrdensAbertasMaximas
                )
                    return CodigosErroEconomia.LimiteDeOrdens;

                // As coroas de todas as unidades ficam guardadas na ordem.
                var total = checked(precoUnitario * quantidade);
                if (!await carteira.DebitarAsync(compradorId, total, c))
                    return CodigosErroEconomia.SaldoInsuficiente;

                var agora = relogio.AgoraSegundos;
                var compra = new OrdemMercado
                {
                    PersonagemId = compradorId,
                    Venda = false,
                    ItemId = itemId,
                    Quantidade = quantidade,
                    PrecoUnitario = precoUnitario,
                    Estado = EstadoDaOrdemDeMercado.Aberta,
                    CriadaEm = agora,
                    ExpiraEm = agora + ConfigMercado.ExpiracaoSegundos,
                };
                db.Set<OrdemMercado>().Add(compra);
                await db.SaveChangesAsync(c);

                // Casa com as vendas mais baratas até o preço máximo, cada uma pelo preço dela.
                var vendas = await db.Set<OrdemMercado>()
                    .Where(o =>
                        o.Venda
                        && o.Estado == EstadoDaOrdemDeMercado.Aberta
                        && o.ItemId == itemId
                        && o.PrecoUnitario <= precoUnitario
                        && o.PersonagemId != compradorId
                        && o.ExpiraEm > agora
                    )
                    .OrderBy(o => o.PrecoUnitario)
                    .ThenBy(o => o.Id)
                    .Take(quantidade)
                    .ToListAsync(c);
                foreach (var venda in vendas)
                {
                    var reembolso = precoUnitario - venda.PrecoUnitario;
                    if (
                        !await LiquidarAsync(
                            venda,
                            compradorId,
                            venda.PrecoUnitario,
                            reembolso,
                            resultado,
                            c
                        )
                    )
                        break; // a bolsa de quem pede está cheia: o resto fica aberto
                    BaixarUnidadeDaCompra(compra, agora);
                    await db.SaveChangesAsync(c);
                }

                return null;
            },
            c
        );
    }

    public async Task<ResultadoDoMercado> ComprarOrdemAsync(
        long compradorId,
        long ordemId,
        CancellationToken c
    ) =>
        await ExecutarAsync(
            async resultado =>
            {
                var venda = await db.Set<OrdemMercado>()
                    .SingleOrDefaultAsync(o => o.Id == ordemId && o.Venda, c);
                if (venda == null)
                    return CodigosErroEconomia.OrdemNaoEncontrada;
                if (
                    venda.Estado != EstadoDaOrdemDeMercado.Aberta
                    || venda.ExpiraEm <= relogio.AgoraSegundos
                )
                    return CodigosErroEconomia.OrdemIndisponivel;
                if (venda.PersonagemId == compradorId)
                    return CodigosErroEconomia.OrdemPropria;

                if (!await carteira.DebitarAsync(compradorId, venda.PrecoUnitario, c))
                    return CodigosErroEconomia.SaldoInsuficiente;
                if (!await LiquidarAsync(venda, compradorId, venda.PrecoUnitario, 0, resultado, c))
                    return CodigosErroEconomia.InventarioCheio;

                return null;
            },
            c
        );

    public async Task<ResultadoDoMercado> ComprarOrdemNaBarracaAsync(
        long compradorId,
        long ordemId,
        long vendedorId,
        CancellationToken c
    ) =>
        await ExecutarAsync(
            async resultado =>
            {
                var venda = await db.Set<OrdemMercado>()
                    .SingleOrDefaultAsync(o => o.Id == ordemId && o.Venda, c);
                if (venda == null)
                    return CodigosErroEconomia.OrdemNaoEncontrada;
                if (
                    venda.Estado != EstadoDaOrdemDeMercado.Aberta
                    || venda.ExpiraEm <= relogio.AgoraSegundos
                )
                    return CodigosErroEconomia.OrdemIndisponivel;
                if (venda.PersonagemId != vendedorId)
                    return CodigosErroEconomia.OrdemNaoEncontrada;
                if (venda.PersonagemId == compradorId)
                    return CodigosErroEconomia.OrdemPropria;

                if (!await carteira.DebitarAsync(compradorId, venda.PrecoUnitario, c))
                    return CodigosErroEconomia.SaldoInsuficiente;
                if (
                    !await LiquidarAsync(
                        venda,
                        compradorId,
                        venda.PrecoUnitario,
                        0,
                        resultado,
                        c,
                        ConfigBarraca.TaxaVendaBarracaPercentual
                    )
                )
                    return CodigosErroEconomia.InventarioCheio;

                return null;
            },
            c
        );

    public async Task<ResultadoDoMercado> CancelarAsync(
        long personagemId,
        long ordemId,
        CancellationToken c
    ) =>
        await ExecutarAsync(
            async resultado =>
            {
                var ordem = await db.Set<OrdemMercado>()
                    .SingleOrDefaultAsync(
                        o => o.Id == ordemId && o.PersonagemId == personagemId,
                        c
                    );
                if (ordem == null)
                    return CodigosErroEconomia.OrdemNaoEncontrada;
                if (ordem.Estado != EstadoDaOrdemDeMercado.Aberta)
                    return CodigosErroEconomia.OrdemIndisponivel;

                await EncerrarAsync(ordem, EstadoDaOrdemDeMercado.Cancelada, resultado, c);
                return null;
            },
            c
        );

    /// <summary>
    /// Entrega à bolsa os itens de vendas vencidas ou canceladas que não couberam na hora.
    /// Devolve quantos voltaram.
    /// </summary>
    public async Task<ResultadoDoMercado> ReceberDevolucoesAsync(
        long personagemId,
        CancellationToken c
    ) =>
        await ExecutarAsync(
            async resultado =>
            {
                var pendentes = await db.Set<OrdemMercado>()
                    .Where(o => o.PersonagemId == personagemId && o.Venda && o.ItemEmCustodia)
                    .Where(o => o.Estado != EstadoDaOrdemDeMercado.Aberta)
                    .OrderBy(o => o.Id)
                    .ToListAsync(c);
                foreach (var ordem in pendentes)
                {
                    if (!await TentarDevolverAsync(ordem, resultado, c))
                        break; // bolsa cheia: as outras também não caberiam
                }

                return null;
            },
            c
        );

    /// <summary>
    /// Vence as ordens abertas que passaram da validade (coroas e itens voltam). Chamada pela
    /// varredura periódica; ordem vencida também nunca casa antes disso.
    /// </summary>
    public async Task<ResultadoDoMercado> ExpirarVencidasAsync(CancellationToken c) =>
        await ExecutarAsync(
            async resultado =>
            {
                var agora = relogio.AgoraSegundos;
                var vencidas = await db.Set<OrdemMercado>()
                    .Where(o => o.Estado == EstadoDaOrdemDeMercado.Aberta && o.ExpiraEm <= agora)
                    .OrderBy(o => o.Id)
                    .Take(200)
                    .ToListAsync(c);
                foreach (var ordem in vencidas)
                    await EncerrarAsync(ordem, EstadoDaOrdemDeMercado.Expirada, resultado, c);
                return null;
            },
            c
        );

    // ── Apoio ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Roda a operação sozinha no processo e numa transação Serializable. Erro devolvido (ou
    /// exceção) não confirma nada: o Dispose da transação desfaz tudo.
    /// </summary>
    private async Task<ResultadoDoMercado> ExecutarAsync(
        Func<ResultadoDoMercado, Task<string?>> operacao,
        CancellationToken c
    )
    {
        await Trava.WaitAsync(c);
        try
        {
            await using var transacao = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                c
            );
            var resultado = new ResultadoDoMercado();
            var erro = await operacao(resultado);
            if (erro != null)
                return ResultadoDoMercado.Falha(erro);

            await transacao.CommitAsync(c);
            return resultado;
        }
        finally
        {
            Trava.Release();
        }
    }

    private async Task<int> AbertasDoPersonagemAsync(long personagemId, CancellationToken c) =>
        await db.Set<OrdemMercado>()
            .CountAsync(
                o => o.PersonagemId == personagemId && o.Estado == EstadoDaOrdemDeMercado.Aberta,
                c
            );

    /// <summary>
    /// Fecha uma venda: o item vai para o comprador, o vendedor recebe o preço menos a taxa (uma
    /// vez) e o comprador recebe de volta o que guardou a mais. Falso, sem mudar nada, se a
    /// bolsa do comprador está cheia.
    /// </summary>
    private async Task<bool> LiquidarAsync(
        OrdemMercado venda,
        long compradorId,
        long precoUnitario,
        long reembolsoAoComprador,
        ResultadoDoMercado resultado,
        CancellationToken c,
        int taxaPercentual = ConfigMercado.TaxaVendaPercentual
    )
    {
        var entregue = await custodia.DevolverAsync(
            compradorId,
            new ItemEmCustodia(venda.ItemId, venda.EquipamentoJson),
            c
        );
        if (!entregue)
            return false;

        var taxa = (precoUnitario * taxaPercentual + 99) / 100;
        var liquido = precoUnitario - taxa;
        if (liquido > 0)
            await carteira.CreditarAsync(venda.PersonagemId, liquido, c);
        if (reembolsoAoComprador > 0)
            await carteira.CreditarAsync(compradorId, reembolsoAoComprador, c);

        venda.Quantidade = 0;
        venda.Estado = EstadoDaOrdemDeMercado.Cumprida;
        venda.EncerradaEm = relogio.AgoraSegundos;
        venda.ItemEmCustodia = false;
        venda.EquipamentoJson = null;
        await db.SaveChangesAsync(c);

        resultado.Negocios.Add(
            new NegocioFeito(venda.PersonagemId, compradorId, venda.ItemId, precoUnitario, taxa)
        );
        return true;
    }

    private static void BaixarUnidadeDaCompra(OrdemMercado compra, long agora)
    {
        compra.Quantidade--;
        if (compra.Quantidade <= 0)
        {
            compra.Estado = EstadoDaOrdemDeMercado.Cumprida;
            compra.EncerradaEm = agora;
        }
    }

    /// <summary>Cancela ou vence uma ordem aberta e devolve o que ela guardava.</summary>
    private async Task EncerrarAsync(
        OrdemMercado ordem,
        EstadoDaOrdemDeMercado novoEstado,
        ResultadoDoMercado resultado,
        CancellationToken c
    )
    {
        ordem.Estado = novoEstado;
        ordem.EncerradaEm = relogio.AgoraSegundos;
        if (!ordem.Venda)
        {
            var guardado = checked(ordem.PrecoUnitario * ordem.Quantidade);
            if (guardado > 0)
                await carteira.CreditarAsync(ordem.PersonagemId, guardado, c);
            ordem.Quantidade = 0;
            await db.SaveChangesAsync(c);
        }
        else
        {
            await db.SaveChangesAsync(c);
            await TentarDevolverAsync(ordem, resultado, c);
        }

        resultado.Encerradas.Add(
            new OrdemEncerrada(
                ordem.PersonagemId,
                ordem.ItemId,
                ordem.Venda,
                novoEstado == EstadoDaOrdemDeMercado.Expirada
            )
        );
    }

    /// <summary>
    /// Devolve à bolsa de quem anunciou o exemplar guardado de uma venda encerrada. Falso, com o
    /// item ainda guardado, se a bolsa está cheia. Ponto único a trocar pelo correio (#212).
    /// </summary>
    private async Task<bool> TentarDevolverAsync(
        OrdemMercado venda,
        ResultadoDoMercado resultado,
        CancellationToken c
    )
    {
        if (!venda.ItemEmCustodia)
            return true;

        var devolvido = await custodia.DevolverAsync(
            venda.PersonagemId,
            new ItemEmCustodia(venda.ItemId, venda.EquipamentoJson),
            c
        );
        if (!devolvido)
            return false;

        venda.ItemEmCustodia = false;
        venda.EquipamentoJson = null;
        await db.SaveChangesAsync(c);
        resultado.Devolvidos.Add((venda.PersonagemId, venda.ItemId));
        return true;
    }
}
