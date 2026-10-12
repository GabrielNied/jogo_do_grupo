namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>Valores ajustáveis de como Combate guarda os recursos em memória.</summary>
public static class ConfigRecursos
{
    /// <summary>De quanto em quanto tempo o que mudou em memória vai para o banco.</summary>
    public const int IntervaloDeGravacaoSegundos = 5;

    /// <summary>
    /// Por quanto tempo os atributos finais lidos de Itens e Personagem valem. É também o atraso
    /// máximo para um equipamento trocado aparecer no golpe seguinte, enquanto Itens não publica
    /// um evento de troca.
    /// </summary>
    public const double ValidadeDosAtributosSegundos = 2;

    /// <summary>Quanto tempo um personagem sem uso fica em memória antes de ser descartado.</summary>
    public const int OciosoParaDescartarSegundos = 300;
}
