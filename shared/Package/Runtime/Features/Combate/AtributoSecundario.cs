namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Os nove atributos secundários (docs/features/personagem.md, "Atributos secundários"). Cada
    /// um é calculado pela <see cref="PipelineDeModificadores"/>.
    /// </summary>
    public enum AtributoSecundario
    {
        RegeneracaoVida,
        RegeneracaoStamina,
        RegeneracaoMana,
        ChanceCritica,
        DanoCritico,
        VelocidadeAtaqueFisico,
        VelocidadeAtaqueMagico,
        PenetracaoFisica,
        PenetracaoMagica,
    }

    /// <summary>De onde vem uma parcela de um secundário.</summary>
    public enum FonteDeModificador
    {
        Base,
        Personagem,
        Arvore,
        Equipamento,
        Efeito,
        Alvo,
    }

    /// <summary>
    /// Soma acrescenta o valor; Multiplica aplica <c>(1 + soma dos bônus do grupo)</c>, então
    /// <c>0,10</c> é +10%.
    /// </summary>
    public enum OperacaoDeModificador
    {
        Soma,
        Multiplica,
    }
}
