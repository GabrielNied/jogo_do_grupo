using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Contas puras do nível do mob (valores em <see cref="ConfigNivelDeMobs"/>) e dos sorteios
/// de faixa (nível do mob, tamanho do grupo).
/// </summary>
public static class EscalaPorNivel
{
    /// <summary>Multiplicador de um atributo para o nível, em relação ao nível da ficha.</summary>
    public static double Multiplicador(int nivel, int nivelDaFicha, double porNivel) =>
        Math.Max(ConfigNivelDeMobs.MultiplicadorMinimo, 1.0 + (porNivel * (nivel - nivelDaFicha)));

    public static int Vida(int vida, int nivel, int nivelDaFicha) =>
        Escalar(vida, Multiplicador(nivel, nivelDaFicha, ConfigNivelDeMobs.VidaPorNivel));

    public static int Dano(int dano, int nivel, int nivelDaFicha) =>
        Escalar(dano, Multiplicador(nivel, nivelDaFicha, ConfigNivelDeMobs.DanoPorNivel));

    public static int Experiencia(int experiencia, int nivel, int nivelDaFicha) =>
        Escalar(
            experiencia,
            Multiplicador(nivel, nivelDaFicha, ConfigNivelDeMobs.ExperienciaPorNivel)
        );

    /// <summary>
    /// Aplica o nível à criatura sorteada. Só a comum escala; chefe e chefe raro ficam como
    /// estão (com o nível da ficha).
    /// </summary>
    public static CriaturaSorteada Aplicar(CriaturaSorteada criatura, int nivel)
    {
        var ficha = criatura.Definicao;
        if (ficha.Categoria != CategoriaDeCriatura.Comum)
        {
            return criatura with { Nivel = ficha.Nivel };
        }

        return criatura with
        {
            Nivel = nivel,
            VidaMaxima = Vida(criatura.VidaMaxima, nivel, ficha.Nivel),
            Dano = Dano(criatura.Dano, nivel, ficha.Nivel),
            Experiencia = Experiencia(criatura.Experiencia, nivel, ficha.Nivel),
        };
    }

    /// <summary>
    /// Inteiro de <paramref name="minimo"/> a <paramref name="maximo"/> (inclusive), com a
    /// mesma chance para cada um. <paramref name="sorteio"/> é de 0 (inclusive) a 1 (exclusive);
    /// fora disso é limitado.
    /// </summary>
    public static int Sortear(int minimo, int maximo, double sorteio)
    {
        if (maximo <= minimo)
        {
            return minimo;
        }

        var opcoes = maximo - minimo + 1;
        var escolhido = (int)Math.Floor(Math.Clamp(sorteio, 0.0, 1.0) * opcoes);
        return minimo + Math.Min(opcoes - 1, escolhido);
    }

    private static int Escalar(int valor, double multiplicador) =>
        (int)Math.Max(1, Math.Round(valor * multiplicador, MidpointRounding.AwayFromZero));
}
