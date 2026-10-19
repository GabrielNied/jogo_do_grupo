using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// O que muda na IA de uma espécie comum em relação ao inimigo da fatia (<c>RegrasInimigo</c>):
/// quanto ela enxerga, a velocidade da perseguição e o ritmo do golpe. O resto (alcance, área
/// do golpe, desistência) é igual para todas. Vida, dano e experiência são da ficha.
/// </summary>
public sealed record ComportamentoDeCriatura(
    double VisaoMetros,
    double VelocidadeDePerseguicaoMetrosPorSegundo,
    double PreparoSegundos,
    double RecuperacaoSegundos,
    bool EhDistancia = false,
    double DistanciaTaticaMinimaMetros = 10.0,
    double DistanciaTaticaMaximaMetros = 16.0,
    double DistanciaRecuoMetros = 4.0,
    double VelocidadeRecuoMetrosPorSegundo = 3.5
)
{
    /// <summary>O comportamento do Javali do Campo: os números de <c>RegrasInimigo</c>.</summary>
    public static readonly ComportamentoDeCriatura Padrao = new(
        RegrasInimigo.DistanciaDeVisaoMetros,
        RegrasInimigo.VelocidadeDePerseguicaoMetrosPorSegundo,
        RegrasInimigo.PreparoDoAtaqueSegundos,
        RegrasInimigo.RecuperacaoDoAtaqueSegundos
    );

    /// <summary>Lobo do Campo: enxerga longe, corre e golpeia rápido (pouca vida).</summary>
    public static readonly ComportamentoDeCriatura Lobo = new(16.0, 5.5, 0.7, 1.0);

    /// <summary>Tatu Rochoso: vê pouco e anda devagar, mas golpeia forte e demora a recuperar.</summary>
    public static readonly ComportamentoDeCriatura Tatu = new(8.0, 2.5, 1.4, 2.0);

    /// <summary>Arqueiro do Campo: mantém distância tática (10-16m), recua se aproximar a menos de 4m e dispara flechas telegrafadas.</summary>
    public static readonly ComportamentoDeCriatura Arqueiro = new(
        VisaoMetros: 18.0,
        VelocidadeDePerseguicaoMetrosPorSegundo: 4.0,
        PreparoSegundos: 1.0,
        RecuperacaoSegundos: 1.5,
        EhDistancia: true,
        DistanciaTaticaMinimaMetros: 10.0,
        DistanciaTaticaMaximaMetros: 16.0,
        DistanciaRecuoMetros: 4.0,
        VelocidadeRecuoMetrosPorSegundo: 3.5
    );

    /// <summary>Comportamento da criatura; as que não têm um próprio usam o padrão.</summary>
    public static ComportamentoDeCriatura Obter(int criaturaId) =>
        criaturaId switch
        {
            CatalogoCriaturas.LoboDoCampo => Lobo,
            CatalogoCriaturas.LoboDeGeada => Lobo,
            CatalogoCriaturas.CarapacaDeBrasa => Tatu,
            CatalogoCriaturas.TatuRochoso => Tatu,
            CatalogoCriaturas.ArqueiroDoCampo => Arqueiro,
            _ => Padrao,
        };

    public long PreparoMs => (long)Math.Round(PreparoSegundos * 1000.0);

    public long RecuperacaoMs => (long)Math.Round(RecuperacaoSegundos * 1000.0);

    /// <summary>Se enxerga um jogador a esta distância.</summary>
    public bool Ve(double distanciaMetros) =>
        distanciaMetros >= 0.0 && distanciaMetros <= VisaoMetros;
}
