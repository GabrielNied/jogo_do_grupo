using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>Um ponto da rota de uma manada: o que é, onde fica e quanto tempo a manada espera nele.</summary>
public sealed record PontoDeRota(TipoDePontoDeRota Tipo, double X, double Z, double EsperaSegundos);

/// <summary>
/// A rota de uma manada (docs/features/mobs.md, "Manadas com rota"): uma volta fechada por
/// pontos (pasto, rio, repouso, retorno), o limite de perseguição e a velocidade de caminhada.
/// Todos os pontos ficam fora da zona urbana.
/// </summary>
public sealed record RotaDaManada(
    IReadOnlyList<PontoDeRota> Pontos,
    double LimiteDePerseguicaoMetros = RegrasManada.LimiteDePerseguicaoMetros,
    double VelocidadeMetrosPorSegundo = RegrasManada.VelocidadeDeRotaMetrosPorSegundo
)
{
    /// <summary>Espera padrão num ponto da rota, em segundos (a calibrar).</summary>
    public const double EsperaPadraoSegundos = 20.0;

    /// <summary>Valida a rota: pelo menos dois pontos, nenhum na zona urbana, números positivos.</summary>
    public RotaDaManada Validada()
    {
        if (Pontos == null || Pontos.Count < 2)
        {
            throw new ArgumentException("Uma rota precisa de pelo menos dois pontos.");
        }

        foreach (var ponto in Pontos)
        {
            if (RegrasManada.EstaNaZonaUrbana(ponto.X, ponto.Z))
            {
                throw new ArgumentException(
                    $"O ponto {ponto.Tipo} ({ponto.X}, {ponto.Z}) está na zona urbana."
                );
            }

            ArgumentOutOfRangeException.ThrowIfNegative(ponto.EsperaSegundos);
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(LimiteDePerseguicaoMetros);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(VelocidadeMetrosPorSegundo);
        return this;
    }
}
