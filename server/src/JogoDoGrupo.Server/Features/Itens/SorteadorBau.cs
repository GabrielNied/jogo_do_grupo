using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Decide o que uma abertura de baú dá. É uma interface para os testes trocarem o sorteio por
/// um resultado conhecido.
/// </summary>
public interface ISorteadorBau
{
    /// <summary>
    /// Ids do catálogo sorteados numa abertura: de <see cref="RegrasItens.MinimoItensPorBau"/> a
    /// <see cref="RegrasItens.MaximoItensPorBau"/> itens.
    /// </summary>
    int[] Sortear();
}

/// <summary>
/// Sorteio de verdade: sorteia a quantidade (1 a 3, uniforme) e depois cada item, com reposição
/// e uniforme entre os ids de <see cref="CatalogoItens.Todos"/>.
/// </summary>
public sealed class SorteadorBau(Random aleatorio) : ISorteadorBau
{
    // Random.Shared é seguro entre threads; um Random próprio não é, então só ele trava.
    private readonly object _trava = new();

    public SorteadorBau()
        : this(Random.Shared) { }

    public int[] Sortear()
    {
        lock (_trava)
        {
            var quantidade = aleatorio.Next(
                RegrasItens.MinimoItensPorBau,
                RegrasItens.MaximoItensPorBau + 1
            );
            var itens = new int[quantidade];
            for (var i = 0; i < itens.Length; i++)
            {
                itens[i] = CatalogoItens.Todos[aleatorio.Next(CatalogoItens.Todos.Length)];
            }

            return itens;
        }
    }
}
