using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// O que o equipamento dá ao combate (#623). Duas portas para o mesmo snapshot das peças ativas:
/// <see cref="IAtributosDosEquipamentos"/> (dano da arma, bônus de dano, vida, stamina e mana máximas e
/// defesas, já convertidos) e <see cref="IFonteDeModificadores"/> com a fonte
/// <see cref="FonteDeModificador.Equipamento"/> (os nove secundários, uma parcela por peça, com o nome
/// da peça como origem). Nada daqui soma nos cinco atributos do personagem.
/// </summary>
internal sealed class EquipamentosParaCombate(IServiceScopeFactory escopos)
    : IAtributosDosEquipamentos,
        IFonteDeModificadores,
        IArmaEquipada,
        IEfeitosDoEquipamento
{
    /// <summary>Grupo de acúmulo das parcelas de equipamento (somas; o grupo é só obrigatório).</summary>
    public const string Grupo = "equipamento";

    public FonteDeModificador Fonte => FonteDeModificador.Equipamento;

    public async Task<AtributosDosEquipamentos> ObterAsync(long personagemId, CancellationToken c)
    {
        var (entradas, conjunto) = await LerAsync(personagemId, c);
        return RegrasAtributosDeItem.Resumir(entradas, conjunto);
    }

    /// <summary>A arma principal do conjunto ativo (de onde Skills tira a barra), ou nulo sem arma.</summary>
    async Task<ArmaEquipada?> IArmaEquipada.ObterAsync(long personagemId, CancellationToken c)
    {
        var (entradas, conjunto) = await LerAsync(personagemId, c);
        var principal = RegrasEquipamentos.Principal(conjunto);
        var arma = entradas.FirstOrDefault(e => e.Posicao == principal && e.Equipamento != null);
        return arma == null
            ? null
            : new ArmaEquipada(
                arma.InstanciaId,
                arma.Equipamento!.ModeloId,
                arma.Equipamento.Tipo,
                conjunto
            );
    }

    /// <summary>Os poderes das peças vestidas e os marcos de set atingidos (só o conjunto ativo).</summary>
    async Task<IReadOnlyList<EfeitoAtivo>> IEfeitosDoEquipamento.ObterAsync(
        long personagemId,
        CancellationToken c
    )
    {
        var (entradas, conjunto) = await LerAsync(personagemId, c);
        return RegrasDePoderes.Ativos(entradas, conjunto);
    }

    async Task<IReadOnlyList<Modificador>> IFonteDeModificadores.ObterAsync(
        long personagemId,
        CancellationToken c
    )
    {
        var (entradas, conjunto) = await LerAsync(personagemId, c);
        return Modificadores(entradas, conjunto);
    }

    /// <summary>
    /// Uma parcela de soma por peça ativa e por secundário elegível, já convertida; valor 0
    /// não vira parcela. A ordem é a de equipar (id do item), então o resultado é estável.
    /// </summary>
    internal static IReadOnlyList<Modificador> Modificadores(
        IEnumerable<EntradaInventario> entradas,
        int conjunto
    )
    {
        var lista = new List<Modificador>();
        foreach (var entrada in entradas)
        {
            if (!RegrasEquipamentos.EstaAtiva(entrada, conjunto))
                continue;
            var item = entrada.Equipamento;
            foreach (var atributo in item.Atributos)
            {
                if (SecundarioDe(atributo.Status) is not { } secundario)
                    continue;
                var valor = ConfigAtributosItem.Converter(
                    item.Tipo,
                    atributo.Status,
                    atributo.Valor
                );
                if (valor == 0)
                    continue;
                lista.Add(
                    new Modificador
                    {
                        Atributo = secundario,
                        Fonte = FonteDeModificador.Equipamento,
                        Origem = item.Nome,
                        Operacao = OperacaoDeModificador.Soma,
                        Ordem = ConfigAtributosSecundarios.OrdemEquipamento,
                        Grupo = Grupo,
                        Valor = valor,
                    }
                );
            }
        }

        return lista;
    }

    /// <summary>O secundário do Combate que corresponde ao atributo do item, ou nulo se não é secundário.</summary>
    internal static AtributoSecundario? SecundarioDe(StatusItem status) =>
        status switch
        {
            StatusItem.RegeneracaoVida => AtributoSecundario.RegeneracaoVida,
            StatusItem.RegeneracaoStamina => AtributoSecundario.RegeneracaoStamina,
            StatusItem.RegeneracaoMana => AtributoSecundario.RegeneracaoMana,
            StatusItem.ChanceCritica => AtributoSecundario.ChanceCritica,
            StatusItem.DanoCritico => AtributoSecundario.DanoCritico,
            StatusItem.VelocidadeAtaqueFisico => AtributoSecundario.VelocidadeAtaqueFisico,
            StatusItem.VelocidadeAtaqueMagico => AtributoSecundario.VelocidadeAtaqueMagico,
            StatusItem.PenetracaoFisica => AtributoSecundario.PenetracaoFisica,
            StatusItem.PenetracaoMagica => AtributoSecundario.PenetracaoMagica,
            _ => null,
        };

    // Mesmo snapshot para conjunto e peças, inclusive durante troca concorrente.
    private async Task<(List<EntradaInventario> Entradas, int Conjunto)> LerAsync(
        long personagemId,
        CancellationToken c
    )
    {
        await using var escopo = escopos.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        await using var transacao = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            c
        );
        var conjunto =
            await db.Set<EstadoEquipamentos>()
                .AsNoTracking()
                .Where(e => e.PersonagemId == personagemId)
                .Select(e => (int?)e.ConjuntoAtivo)
                .SingleOrDefaultAsync(c)
            ?? 1;
        var itens = await db.Set<ItemInventario>()
            .AsNoTracking()
            .Where(i =>
                i.PersonagemId == personagemId && i.Posicao != PosicaoEquipamento.Inventario
            )
            .OrderBy(i => i.Id)
            .ToListAsync(c);
        var entradas = itens.Select(i => i.ParaEntrada()).ToList();
        await transacao.CommitAsync(c);
        return (entradas, conjunto);
    }
}
