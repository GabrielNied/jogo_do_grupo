namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Vida, stamina e mana atuais de um personagem (uma linha por personagem, criada na primeira
/// consulta, já cheia). Os máximos não são guardados: saem dos atributos a cada consulta.
/// </summary>
public sealed class RecursosPersonagem
{
    /// <summary>Personagem dono (<c>Personagens.Id</c>); também é a chave.</summary>
    public long PersonagemId { get; set; }

    public int Vida { get; set; }
    public int Stamina { get; set; }
    public int Mana { get; set; }

    /// <summary>Instante (UTC) do último gasto de stamina. Antes de qualquer gasto, a época.</summary>
    public DateTime UltimoGastoStaminaEm { get; set; } = DateTime.UnixEpoch;

    /// <summary>Instante (UTC) do último gasto de mana.</summary>
    public DateTime UltimoGastoManaEm { get; set; } = DateTime.UnixEpoch;

    /// <summary>
    /// Até quando a regeneração da stamina já foi paga. Só avança pelo tempo que virou pontos
    /// inteiros, para consultas frequentes não perderem a fração.
    /// </summary>
    public DateTime StaminaContadaAte { get; set; }

    /// <summary>Até quando a regeneração da mana já foi paga.</summary>
    public DateTime ManaContadaAte { get; set; }
}
