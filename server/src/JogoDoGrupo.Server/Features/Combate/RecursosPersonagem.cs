namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Vida, vigor e energia atuais de um personagem (uma linha por personagem, criada na primeira
/// consulta, já cheia). Os máximos não são guardados: saem dos atributos a cada consulta.
/// </summary>
public sealed class RecursosPersonagem
{
    /// <summary>Personagem dono (<c>Personagens.Id</c>); também é a chave.</summary>
    public long PersonagemId { get; set; }

    public int Vida { get; set; }
    public int Vigor { get; set; }
    public int Energia { get; set; }

    /// <summary>Instante (UTC) do último gasto de vigor. Antes de qualquer gasto, a época.</summary>
    public DateTime UltimoGastoVigorEm { get; set; } = DateTime.UnixEpoch;

    /// <summary>Instante (UTC) do último gasto de energia.</summary>
    public DateTime UltimoGastoEnergiaEm { get; set; } = DateTime.UnixEpoch;

    /// <summary>
    /// Até quando a regeneração do vigor já foi paga. Só avança pelo tempo que virou pontos
    /// inteiros, para consultas frequentes não perderem a fração.
    /// </summary>
    public DateTime VigorContadoAte { get; set; }

    /// <summary>Até quando a regeneração da energia já foi paga.</summary>
    public DateTime EnergiaContadaAte { get; set; }
}
