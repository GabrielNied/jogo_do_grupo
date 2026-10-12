using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Decide o que um inimigo derrotado dá a <b>um</b> participante. É uma interface para os
/// testes trocarem o sorteio por um resultado conhecido.
/// </summary>
public interface ISorteadorDeLoot
{
    /// <summary>
    /// Peças sorteadas para um participante: de zero a <see cref="RegrasLoot.MaximoPorInimigo"/>.
    /// A raridade pesa pela <paramref name="origem"/> e o grau vem do nível do inimigo. Cada
    /// chamada é um sorteio novo.
    /// </summary>
    QuedaDeLoot[] Sortear(OrigemDoLoot origem, int nivelDoInimigo);
}

/// <summary>
/// Sorteio de verdade: a quantidade vem de <see cref="RegrasLoot.QuantidadeSorteada"/>, cada
/// item vira equipamento com <see cref="ConfigLoot.ChanceDeEquipamento"/> e a peça sai de
/// <see cref="TabelaDeLoot.Sortear"/> (as 72 do catálogo; poção e moeda de teste não caem).
/// </summary>
public sealed class SorteadorDeLoot(Random aleatorio) : ISorteadorDeLoot
{
    // Random.Shared é seguro entre threads; um Random próprio não é, então só ele trava.
    private readonly object _trava = new();

    public SorteadorDeLoot()
        : this(Random.Shared) { }

    public QuedaDeLoot[] Sortear(OrigemDoLoot origem, int nivelDoInimigo)
    {
        lock (_trava)
        {
            var quedas = new List<QuedaDeLoot>(RegrasLoot.MaximoPorInimigo);
            var quantidade = RegrasLoot.QuantidadeSorteada(aleatorio.NextDouble());
            for (var i = 0; i < quantidade; i++)
            {
                if (aleatorio.NextDouble() < ConfigLoot.ChanceDeEquipamento)
                {
                    quedas.Add(TabelaDeLoot.Sortear(aleatorio, origem, nivelDoInimigo));
                }
            }

            return quedas.ToArray();
        }
    }
}
