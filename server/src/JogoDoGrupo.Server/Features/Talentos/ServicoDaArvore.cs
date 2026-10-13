using System.Data;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Talentos;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Talentos;

/// <summary>Resultado de uma compra: o estado novo, ou o código de erro.</summary>
public sealed record ResultadoDaCompra(ArvoreResponse? Arvore, string? Erro);

/// <summary>
/// Lê e compra ranks da árvore. Usa o mesmo contexto da requisição, então a baixa dos pontos de
/// Personagem (<see cref="IPontosDeNivel"/>) e a gravação do rank saem na mesma transação.
/// </summary>
public sealed class ServicoDaArvore(
    JogoDbContext db,
    IPontosDeNivel pontos,
    ConfiguracaoDaArvore configuracao
)
{
    public async Task<ArvoreResponse> ObterAsync(long personagemId, CancellationToken cancelamento)
    {
        var cabecalho = await db.Set<ArvoreDoPersonagem>()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.PersonagemId == personagemId, cancelamento);
        var nos = await db.Set<NoComprado>()
            .AsNoTracking()
            .Where(n => n.PersonagemId == personagemId)
            .ToListAsync(cancelamento);
        var livres = await pontos.ObterAsync(personagemId, cancelamento);
        return Montar(cabecalho, nos, livres);
    }

    /// <summary>
    /// Compra o rank pedido ou não muda nada. A transação Serializable abre com a trava de
    /// escrita no SQLite (<c>BEGIN IMMEDIATE</c>): dois pedidos do mesmo personagem rodam um
    /// depois do outro, e o segundo já vê o rank que o primeiro comprou (e só responde sucesso).
    /// </summary>
    public async Task<ResultadoDaCompra> ComprarAsync(
        long personagemId,
        ComprarNoRequest pedido,
        CancellationToken cancelamento
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );

        var cabecalho = await db.Set<ArvoreDoPersonagem>()
            .FirstOrDefaultAsync(a => a.PersonagemId == personagemId, cancelamento);
        var nos = await db.Set<NoComprado>()
            .Where(n => n.PersonagemId == personagemId)
            .ToListAsync(cancelamento);

        if (
            pedido.VersaoConfiguracao != configuracao.Versao
            || (cabecalho != null && cabecalho.Versao != configuracao.Versao)
        )
        {
            return new ResultadoDaCompra(null, CodigosErroTalentos.ArvoreDesatualizada);
        }

        var livres = await pontos.ObterAsync(personagemId, cancelamento);
        var ranks = RegrasDaArvore.ParaDicionario(Ranks(nos));
        var avaliacao = RegrasDaArvore.AvaliarCompra(
            configuracao,
            ranks,
            livres,
            pedido.NoId,
            pedido.RankAlvo
        );
        if (avaliacao.Erro != null)
        {
            return new ResultadoDaCompra(null, avaliacao.Erro);
        }

        if (avaliacao.JaComprado)
        {
            return new ResultadoDaCompra(Montar(cabecalho, nos, livres), null);
        }

        if (!await pontos.TentarGastarAsync(personagemId, avaliacao.Custo, cancelamento))
        {
            return new ResultadoDaCompra(null, CodigosErroTalentos.PontosInsuficientes);
        }

        if (cabecalho == null)
        {
            cabecalho = new ArvoreDoPersonagem
            {
                PersonagemId = personagemId,
                Versao = configuracao.Versao,
            };
            db.Add(cabecalho);
        }

        cabecalho.PontosGastos += avaliacao.Custo;

        var no = nos.FirstOrDefault(n => n.NoId == pedido.NoId);
        if (no == null)
        {
            no = new NoComprado { PersonagemId = personagemId, NoId = pedido.NoId };
            nos.Add(no);
            db.Add(no);
        }

        no.Rank = pedido.RankAlvo;

        await db.SaveChangesAsync(cancelamento);
        await transacao.CommitAsync(cancelamento);
        return new ResultadoDaCompra(Montar(cabecalho, nos, livres - avaliacao.Custo), null);
    }

    private static RankDoNo[] Ranks(List<NoComprado> nos) =>
        nos.Where(n => n.Rank > 0)
            .OrderBy(n => n.NoId, StringComparer.Ordinal)
            .Select(n => new RankDoNo { NoId = n.NoId, Rank = n.Rank })
            .ToArray();

    private ArvoreResponse Montar(ArvoreDoPersonagem? cabecalho, List<NoComprado> nos, int livres)
    {
        var ranks = Ranks(nos);
        return new ArvoreResponse
        {
            Configuracao = configuracao,
            PontosLivres = livres,
            PontosGastos = cabecalho?.PontosGastos ?? 0,
            Ranks = ranks,
            Atributos = RegrasDaArvore.SomarAtributos(
                configuracao,
                RegrasDaArvore.ParaDicionario(ranks)
            ),
        };
    }
}
