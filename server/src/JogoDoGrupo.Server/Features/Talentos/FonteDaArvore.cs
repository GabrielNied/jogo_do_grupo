using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Talentos;
using Microsoft.EntityFrameworkCore;
using TipoDeEfeito = JogoDoGrupo.Shared.Features.Talentos.TipoDeEfeito;

namespace JogoDoGrupo.Server.Features.Talentos;

/// <summary>
/// Modificadores dos atributos secundários que os nós comprados dão, para a pipeline de Combate
/// (fonte <see cref="FonteDeModificador.Arvore"/>). Cada efeito de secundário de um nó comprado
/// vira um modificador com o <b>total atual</b> (valor por rank vezes o rank), que Combate
/// soma ou multiplica na ordem da árvore. Os efeitos de atributo principal não passam por aqui
/// (vão por <see cref="Contratos.Talentos.IAtributosDaArvore"/>).
/// </summary>
public sealed class FonteDaArvore(IServiceScopeFactory escopos, ConfiguracaoDaArvore configuracao)
    : IFonteDeModificadores
{
    /// <summary>Grupo de acúmulo dos bônus percentuais da árvore: somam entre si.</summary>
    public const string Grupo = "arvore";

    public FonteDeModificador Fonte => FonteDeModificador.Arvore;

    public async Task<IReadOnlyList<Modificador>> ObterAsync(
        long personagemId,
        CancellationToken cancelamento
    )
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var comprados = await db.Set<NoComprado>()
            .AsNoTracking()
            .Where(n => n.PersonagemId == personagemId)
            .ToListAsync(cancelamento);
        var ranks = comprados.ToDictionary(n => n.NoId, n => n.Rank);

        var modificadores = new List<Modificador>();
        foreach (var no in configuracao.Nos)
        {
            var rank = RegrasDaArvore.RankDe(ranks, no.Id);
            if (rank <= 0)
            {
                continue;
            }

            foreach (var efeito in no.Efeitos)
            {
                if (
                    efeito.Tipo == TipoDeEfeito.AtributoSecundario
                    && Enum.TryParse<AtributoSecundario>(efeito.Chave, out var atributo)
                )
                {
                    modificadores.Add(
                        new Modificador
                        {
                            Atributo = atributo,
                            Fonte = FonteDeModificador.Arvore,
                            Origem = "Árvore: " + no.Nome,
                            Operacao = efeito.Multiplica
                                ? OperacaoDeModificador.Multiplica
                                : OperacaoDeModificador.Soma,
                            Ordem = ConfigAtributosSecundarios.OrdemArvore,
                            Grupo = Grupo,
                            Valor = efeito.ValorPorRank * rank,
                        }
                    );
                }
            }
        }

        return modificadores;
    }

    /// <summary>
    /// Efeitos de secundário cuja chave não é o nome de um <see cref="AtributoSecundario"/>.
    /// Entram no conferir da configuração ao subir o servidor.
    /// </summary>
    public static IEnumerable<string> ProblemasDeSecundarios(ConfiguracaoDaArvore configuracao)
    {
        foreach (var no in configuracao?.Nos ?? [])
        {
            foreach (var efeito in no.Efeitos ?? [])
            {
                if (
                    efeito.Tipo == TipoDeEfeito.AtributoSecundario
                    && !Enum.TryParse<AtributoSecundario>(efeito.Chave, out _)
                )
                {
                    yield return $"Nó '{no.Id}' tem efeito num secundário que não existe: '{efeito.Chave}'.";
                }
            }
        }
    }
}
