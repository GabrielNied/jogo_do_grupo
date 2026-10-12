using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Uma criatura que acabou de aparecer, já com a variante sorteada e os valores finais.
/// </summary>
public sealed record CriaturaSorteada(
    DefinicaoDeCriatura Definicao,
    VarianteDeCriatura Variante,
    int VidaMaxima,
    int Dano,
    int Experiencia,
    double MultiplicadorDeRecompensa,
    float Escala,
    int Nivel = 0
);

/// <summary>
/// Decide os sorteios de criaturas. É uma interface para os testes trocarem o sorteio por um
/// resultado conhecido.
/// </summary>
public interface ISorteadorDeCriaturas
{
    /// <summary>
    /// Sorteia a variante de uma criatura que vai aparecer e calcula os valores finais. Só a
    /// comum pode vir fortalecida; chefe e chefe raro saem sempre com os valores de base.
    /// </summary>
    CriaturaSorteada Sortear(DefinicaoDeCriatura definicao);

    /// <summary>
    /// Sorteia se a criatura aparece desta vez, pela
    /// <see cref="DefinicaoDeCriatura.ChanceDeAparecer"/>.
    /// </summary>
    bool Aparece(DefinicaoDeCriatura definicao);

    /// <summary>
    /// Número de 0 (inclusive) a 1 (exclusive) para os outros sorteios do grupo (nível e
    /// tamanho). Os testes trocam por um valor conhecido.
    /// </summary>
    double Proximo() => Random.Shared.NextDouble();
}

/// <summary>Sorteio de verdade, com as regras de <see cref="RegrasCriaturas"/>.</summary>
public sealed class SorteadorDeCriaturas(Random aleatorio) : ISorteadorDeCriaturas
{
    // Random.Shared é seguro entre threads; um Random próprio não é, então só ele trava.
    private readonly object _trava = new();

    public SorteadorDeCriaturas()
        : this(Random.Shared) { }

    public CriaturaSorteada Sortear(DefinicaoDeCriatura definicao)
    {
        // Chefes não gastam sorteio: a sequência de um Random com semente não depende deles.
        var variante = RegrasCriaturas.PodeSerFortalecida(definicao.Categoria)
            ? RegrasCriaturas.SortearVariante(definicao.Categoria, ProximoSorteio())
            : VarianteDeCriatura.Normal;

        return new CriaturaSorteada(
            definicao,
            variante,
            RegrasCriaturas.VidaMaxima(definicao.VidaBase, variante),
            RegrasCriaturas.Dano(definicao.DanoBase, variante),
            RegrasCriaturas.Experiencia(definicao.ExperienciaBase, variante),
            RegrasCriaturas.MultiplicadorDeRecompensa(variante),
            RegrasCriaturas.Escala(variante),
            definicao.Nivel
        );
    }

    public bool Aparece(DefinicaoDeCriatura definicao) =>
        RegrasCriaturas.Aparece(definicao.ChanceDeAparecer, ProximoSorteio());

    public double Proximo() => ProximoSorteio();

    private double ProximoSorteio()
    {
        lock (_trava)
        {
            return aleatorio.NextDouble();
        }
    }
}
