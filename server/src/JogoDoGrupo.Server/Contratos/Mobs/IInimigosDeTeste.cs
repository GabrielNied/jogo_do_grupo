namespace JogoDoGrupo.Server.Contratos.Mobs;

/// <summary>
/// Ferramentas de inimigos da tela de teste de Desenvolvimento: escolher a espécie e matar os
/// da área sem dar experiência nem loot. Só essa tela chama.
/// </summary>
public interface IInimigosDeTeste
{
    (bool Aceito, string Texto) SpawnarEspecie(long personagemId, int especieId, int quantidade);

    (bool Aceito, string Texto) MatarDaArea(long personagemId);
}
