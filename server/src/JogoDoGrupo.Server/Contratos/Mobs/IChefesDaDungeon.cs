namespace JogoDoGrupo.Server.Contratos.Mobs;

/// <summary>
/// Oferecido por Mobs a quem monta uma dungeon (Instâncias): põe o boss no salão final e o tira
/// quando a instância fecha. Sem regra aqui: a luta (ataques, janela exposta, morte e
/// <see cref="InimigoDerrotado"/>) é toda de Mobs. Seguro entre threads.
/// </summary>
public interface IChefesDaDungeon
{
    /// <summary>
    /// Põe um boss novo, de vida cheia, na área, com a origem em (<paramref name="x"/>,
    /// <paramref name="z"/>) no plano do chão. Já existindo um boss na área, ele é trocado por
    /// este (reinício). Devolve o <c>InimigoId</c> do boss, o mesmo <c>AlvoId</c> do Combate.
    /// </summary>
    long Criar(string areaId, double x, double z);

    /// <summary>Tira o boss da área, vivo ou morto. Falso se a área não tinha boss.</summary>
    bool Remover(string areaId);
}
