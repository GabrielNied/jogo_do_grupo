namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Quanto o nível do mob conta em vida, dano e experiência (proposta, a calibrar). O nível de
/// referência é o da ficha da criatura (<c>DefinicaoDeCriatura.Nivel</c>): nele os valores são
/// os da ficha; cada nível acima soma o percentual abaixo (e cada nível abaixo tira). Vale só
/// para a criatura comum: chefes têm força fixa. A variante fortalecida já está nos valores
/// que chegam, e o nível multiplica por cima.
/// </summary>
public static class ConfigNivelDeMobs
{
    /// <summary>Vida a mais (ou a menos) por nível de diferença para a ficha.</summary>
    public const double VidaPorNivel = 0.15;

    /// <summary>Dano a mais (ou a menos) por nível de diferença para a ficha.</summary>
    public const double DanoPorNivel = 0.10;

    /// <summary>Experiência a mais (ou a menos) por nível de diferença para a ficha.</summary>
    public const double ExperienciaPorNivel = 0.20;

    /// <summary>Piso do multiplicador: um mob de nível baixo nunca vale menos que isso.</summary>
    public const double MultiplicadorMinimo = 0.4;
}
