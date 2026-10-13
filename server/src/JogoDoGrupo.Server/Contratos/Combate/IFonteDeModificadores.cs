using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Contratos.Combate;

/// <summary>
/// Quem tem modificadores dos atributos secundários (árvore, equipamento, efeitos, alvo) registra
/// uma implementação, e Combate a consulta. A fonte devolve o <b>total atual</b> de cada
/// modificador (nunca um acréscimo) e declara a operação, a ordem e o grupo de acúmulo de cada um.
/// Modificador de outra fonte que não <see cref="Fonte"/>, ou da fonte Base, é recusado.
/// </summary>
public interface IFonteDeModificadores
{
    /// <summary>A única <see cref="FonteDeModificador"/> que esta implementação pode declarar.</summary>
    FonteDeModificador Fonte { get; }

    Task<IReadOnlyList<Modificador>> ObterAsync(long personagemId, CancellationToken cancelamento);
}
