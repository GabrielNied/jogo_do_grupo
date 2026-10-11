using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Decide o que um inimigo comum derrotado dá a <b>um</b> participante. É uma interface para os
/// testes trocarem o sorteio por um resultado conhecido.
/// </summary>
public interface ISorteadorDeLoot
{
    /// <summary>
    /// Ids do catálogo sorteados para um participante: de zero a
    /// <see cref="RegrasLoot.MaximoPorInimigo"/> itens. Cada chamada é um sorteio novo.
    /// </summary>
    int[] Sortear();
}

/// <summary>
/// Sorteio de verdade: a quantidade vem de <see cref="RegrasLoot.QuantidadeSorteada"/> e cada
/// item é uniforme entre os ids de <see cref="CatalogoItens.Todos"/>, com reposição.
/// </summary>
public sealed class SorteadorDeLoot(Random aleatorio) : ISorteadorDeLoot
{
    // Random.Shared é seguro entre threads; um Random próprio não é, então só ele trava.
    private readonly object _trava = new();

    public SorteadorDeLoot()
        : this(Random.Shared) { }

    public int[] Sortear()
    {
        lock (_trava)
        {
            var itens = new int[RegrasLoot.QuantidadeSorteada(aleatorio.NextDouble())];
            for (var i = 0; i < itens.Length; i++)
            {
                itens[i] = CatalogoItens.Todos[aleatorio.Next(CatalogoItens.Todos.Length)];
            }

            return itens;
        }
    }
}
