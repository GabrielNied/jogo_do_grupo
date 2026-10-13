using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// A conta do passo que o inimigo comum dá durante o preparo do golpe: quanto do passo de
/// <see cref="RegrasInimigo.PassoDoGolpeMetros"/> cabe na direção travada. Pura, sem estado.
/// </summary>
public static class PassoDoGolpe
{
    /// <summary>
    /// Até onde o passo vai, em metros (de 0 a <see cref="RegrasInimigo.PassoDoGolpeMetros"/>),
    /// conferindo o caminho de <see cref="ConfigInimigosDoCampo.SubpassoDoGolpeMetros"/> em
    /// <see cref="ConfigInimigosDoCampo.SubpassoDoGolpeMetros"/>. <paramref name="podeIr"/>
    /// recebe o ponto de onde sai e o ponto aonde quer chegar (x, z, x, z).
    /// </summary>
    public static double Livre(
        double x,
        double z,
        double direcaoYGraus,
        Func<double, double, double, double, bool> podeIr
    )
    {
        var radianos = direcaoYGraus * Math.PI / 180.0;
        var dx = Math.Sin(radianos);
        var dz = Math.Cos(radianos);
        var livre = 0.0;
        for (
            var andado = ConfigInimigosDoCampo.SubpassoDoGolpeMetros;
            andado <= RegrasInimigo.PassoDoGolpeMetros + 1e-9;
            andado += ConfigInimigosDoCampo.SubpassoDoGolpeMetros
        )
        {
            if (!podeIr(x + dx * livre, z + dz * livre, x + dx * andado, z + dz * andado))
            {
                break;
            }

            livre = andado;
        }

        return livre;
    }

    /// <summary>
    /// Se ir de um ponto a outro não leva o inimigo a menos de
    /// <see cref="RegrasCerco.SeparacaoMinimaMetros"/> de outro corpo. Quem já está mais perto
    /// que isso só não pode se aproximar mais (senão ficaria preso).
    /// </summary>
    public static bool RespeitaVizinho(
        double deX,
        double deZ,
        double x,
        double z,
        double vizinhoX,
        double vizinhoZ
    )
    {
        var depois = RegrasInimigo.Distancia(x, z, vizinhoX, vizinhoZ);
        var antes = RegrasInimigo.Distancia(deX, deZ, vizinhoX, vizinhoZ);
        return depois >= RegrasCerco.SeparacaoMinimaMetros || depois >= antes;
    }
}
