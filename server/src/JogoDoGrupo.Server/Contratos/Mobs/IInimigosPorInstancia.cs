namespace JogoDoGrupo.Server.Contratos.Mobs;

/// <summary>
/// Um grupo de inimigos comuns que uma instância pede: a criatura (id do catálogo), o centro no
/// plano do chão, quantos são e a faixa de nível. Sem respawn.
/// </summary>
public sealed record GrupoDeInstancia(
    int CriaturaId,
    double X,
    double Z,
    int Quantidade,
    int NivelMinimo,
    int NivelMaximo,
    double MultiplicadorVida = 1.0,
    double MultiplicadorDano = 1.0
);

/// <summary>
/// Oferecido por Mobs a quem monta uma dungeon (Instâncias): põe os grupos de inimigos comuns de
/// uma corrida, que <b>não renascem</b>, e os tira quando a instância fecha. A IA, o dano e a
/// morte (<see cref="InimigoDerrotado"/>) são de Mobs. Seguro entre threads.
/// </summary>
public interface IInimigosPorInstancia
{
    /// <summary>
    /// Põe os grupos na área. Já havendo grupos na área, eles são trocados por estes (reinício).
    /// </summary>
    void Criar(string areaId, IReadOnlyList<GrupoDeInstancia> grupos);

    /// <summary>Tira todos os grupos da área, vivos ou mortos. Falso se a área não tinha.</summary>
    bool Remover(string areaId);
}
