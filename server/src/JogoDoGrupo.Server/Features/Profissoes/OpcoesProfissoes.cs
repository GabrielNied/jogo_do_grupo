namespace JogoDoGrupo.Server.Features.Profissoes;

/// <summary>
/// Ajustes de Profissões lidos da configuração (<c>Profissoes</c>). O padrão exige proximidade;
/// os testes de integração desligam para não precisar simular posição.
/// </summary>
public sealed class OpcoesProfissoes
{
    /// <summary>Confere a distância pela posição aceita pelo Mundo.</summary>
    public bool ExigirProximidade { get; set; } = true;
}
