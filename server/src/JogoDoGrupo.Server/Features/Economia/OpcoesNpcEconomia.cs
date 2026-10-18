namespace JogoDoGrupo.Server.Features.Economia;

/// <summary>
/// Ajustes dos NPCs de economia lidos da configuração (<c>Economia:Npc</c>). O padrão exige
/// proximidade; os testes de integração desligam para não precisar simular posição.
/// </summary>
public sealed class OpcoesNpcEconomia
{
    /// <summary>Confere a distância pela posição aceita pelo Mundo.</summary>
    public bool ExigirProximidade { get; set; } = true;
}
