using System.Data;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Profissoes;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Profissoes;

/// <summary>
/// As regras de Profissões (#192): aprender, coletar nos pontos do mundo e produzir na forja.
/// O servidor decide tudo; o client só pede. Materiais são saldos por personagem e a produção
/// grava gasto e item na mesma transação (tudo ou nada).
/// </summary>
public sealed class ServicoDeProfissoes(
    JogoDbContext db,
    IInventarioEconomia inventario,
    IPosicoesDoMundo posicoes,
    PontosDeColetaDoMundo pontos,
    OpcoesProfissoes opcoes
)
{
    public async Task<EstadoDasProfissoes> ObterEstadoAsync(long personagemId, CancellationToken c)
    {
        var aprendidas = await db.Set<ProfissaoAprendida>()
            .AsNoTracking()
            .Where(a => a.PersonagemId == personagemId)
            .Select(a => (int)a.Profissao)
            .ToArrayAsync(c);
        var saldos = await db.Set<SaldoDeMaterialDeProfissao>()
            .AsNoTracking()
            .Where(s => s.PersonagemId == personagemId && s.Quantidade > 0)
            .OrderBy(s => s.MaterialId)
            .Select(s => new SaldoDeMaterial(s.MaterialId, s.Quantidade))
            .ToArrayAsync(c);
        return new EstadoDasProfissoes(aprendidas, saldos);
    }

    /// <summary>A área em que o personagem está (a cidade se a posição não é exigida).</summary>
    public string AreaDe(long personagemId) =>
        posicoes.TentarObter(personagemId, out var posicao) ? posicao.Area : AreasDoMundo.Cidade;

    public IReadOnlyList<long> PersonagensNaArea(string area) => posicoes.PersonagensNaArea(area);

    public EstadoDosNos EstadoDosNos(string area) => pontos.EstadoDaArea(area);

    /// <summary>Aprende a profissão. Devolve o código de erro, ou nulo se deu certo.</summary>
    public async Task<string?> AprenderAsync(
        long personagemId,
        Profissao profissao,
        CancellationToken c
    )
    {
        if (!ConfigProfissoes.PodeAprender(profissao))
            return CodigosErroProfissoes.ProfissaoInvalida;
        if (await Aprendeu(personagemId, profissao, c))
            return CodigosErroProfissoes.JaAprendida;

        db.Add(new ProfissaoAprendida { PersonagemId = personagemId, Profissao = profissao });
        try
        {
            await db.SaveChangesAsync(c);
        }
        catch (DbUpdateException e) when (e.EhViolacaoDeUnicidade())
        {
            return CodigosErroProfissoes.JaAprendida;
        }

        return null;
    }

    /// <summary>
    /// Coleta o ponto: confere profissão e distância, leva o ponto (primeiro a chegar) e credita
    /// o material. Se o crédito falha, o ponto volta.
    /// </summary>
    public async Task<(string? Erro, ColetaConcluida? Resultado, string Area)> ColetarAsync(
        long personagemId,
        int noId,
        CancellationToken c
    )
    {
        var area = AreaDe(personagemId);
        var ponto = ConfigProfissoes.ObterPonto(noId);
        if (ponto == null)
            return (CodigosErroProfissoes.NoInexistente, null, area);
        if (!await Aprendeu(personagemId, ponto.Profissao, c))
            return (CodigosErroProfissoes.ProfissaoNaoAprendida, null, area);
        if (!EstaPerto(personagemId, p => ConfigProfissoes.PertoDoPonto(ponto, p.X, p.Z)))
            return (CodigosErroProfissoes.Longe, null, area);
        if (!pontos.TentarColetar(area, noId))
            return (CodigosErroProfissoes.NoEsgotado, null, area);

        try
        {
            await using var transacao = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                c
            );
            await CreditarAsync(personagemId, ponto.MaterialId, ponto.Quantidade, c);
            await transacao.CommitAsync(c);
        }
        catch
        {
            pontos.Devolver(area, noId);
            throw;
        }

        return (null, new ColetaConcluida(noId, ponto.MaterialId, ponto.Quantidade), area);
    }

    /// <summary>
    /// Produz a receita na forja: debita os materiais e põe o item na bolsa, tudo na mesma
    /// transação. Sem material ou com a bolsa cheia, nada muda.
    /// </summary>
    public async Task<(string? Erro, ProducaoConcluida? Resultado)> ProduzirAsync(
        long personagemId,
        int receitaId,
        CancellationToken c
    )
    {
        var receita = ConfigProfissoes.ObterReceita(receitaId);
        if (receita == null)
            return (CodigosErroProfissoes.ReceitaInexistente, null);
        if (!await Aprendeu(personagemId, receita.Profissao, c))
            return (CodigosErroProfissoes.ProfissaoNaoAprendida, null);
        if (
            !EstaPerto(
                personagemId,
                p => ConfigProfissoes.PertoDaEstacao(receita.Profissao, p.X, p.Z)
            )
        )
            return (CodigosErroProfissoes.Longe, null);

        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            c
        );
        for (var i = 0; i < receita.MateriaisIds.Length; i++)
        {
            var id = receita.MateriaisIds[i];
            var quantidade = receita.MateriaisQuantidades[i];
            var debitou =
                await db.Set<SaldoDeMaterialDeProfissao>()
                    .Where(s =>
                        s.PersonagemId == personagemId
                        && s.MaterialId == id
                        && s.Quantidade >= quantidade
                    )
                    .ExecuteUpdateAsync(
                        p => p.SetProperty(s => s.Quantidade, s => s.Quantidade - quantidade),
                        c
                    ) == 1;
            if (!debitou)
                return (CodigosErroProfissoes.MateriaisInsuficientes, null);
        }

        if (!await inventario.AdicionarItemCompradoAsync(personagemId, receita.ModeloId, 1, c))
            return (CodigosErroProfissoes.InventarioCheio, null);

        await transacao.CommitAsync(c);
        return (null, new ProducaoConcluida(receita.Id, receita.ModeloId));
    }

    private Task<bool> Aprendeu(long personagemId, Profissao profissao, CancellationToken c) =>
        db.Set<ProfissaoAprendida>()
            .AnyAsync(a => a.PersonagemId == personagemId && a.Profissao == profissao, c);

    private bool EstaPerto(long personagemId, Func<PosicaoNoMundo, bool> perto)
    {
        if (!opcoes.ExigirProximidade)
            return true;
        return posicoes.TentarObter(personagemId, out var posicao)
            && ConfigProfissoes.EhDaArea(posicao.Area)
            && perto(posicao);
    }

    private async Task CreditarAsync(
        long personagemId,
        int materialId,
        int quantidade,
        CancellationToken c
    )
    {
        var linhas = await AcrescentarAsync(personagemId, materialId, quantidade, c);
        if (linhas == 1)
            return;

        db.Add(
            new SaldoDeMaterialDeProfissao
            {
                PersonagemId = personagemId,
                MaterialId = materialId,
                Quantidade = quantidade,
            }
        );
        try
        {
            await db.SaveChangesAsync(c);
        }
        catch (DbUpdateException e) when (e.EhViolacaoDeUnicidade())
        {
            db.ChangeTracker.Clear();
            await AcrescentarAsync(personagemId, materialId, quantidade, c);
        }
    }

    private Task<int> AcrescentarAsync(
        long personagemId,
        int materialId,
        int quantidade,
        CancellationToken c
    ) =>
        db.Set<SaldoDeMaterialDeProfissao>()
            .Where(s => s.PersonagemId == personagemId && s.MaterialId == materialId)
            .ExecuteUpdateAsync(
                p => p.SetProperty(s => s.Quantidade, s => s.Quantidade + quantidade),
                c
            );
}
