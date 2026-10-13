namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>Tipos de nó da árvore. Hoje só pequeno e híbrido têm nós de teste.</summary>
    public enum TipoDeNo
    {
        /// <summary>Aumenta um atributo (principal ou secundário).</summary>
        Pequeno = 0,

        /// <summary>Liga dois caminhos vizinhos.</summary>
        Hibrido = 1,

        /// <summary>Especialização com bônus condicional pela skill (depende da pipeline do Combate).</summary>
        Grande = 2,

        /// <summary>Marco ou sinergia (custos e requisitos pendentes).</summary>
        Avancado = 3,
    }
}
