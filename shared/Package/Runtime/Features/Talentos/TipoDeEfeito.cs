namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>O que um efeito de nó altera.</summary>
    public enum TipoDeEfeito
    {
        /// <summary>Soma ao atributo principal escolhido. Alimenta os atributos de Combate.</summary>
        AtributoPrincipal = 0,

        /// <summary>
        /// Soma a um atributo secundário (<see cref="EfeitoDeNo.Chave"/>). O validador aceita,
        /// mas o servidor ainda não aplica: espera a pipeline de secundários do Combate.
        /// </summary>
        AtributoSecundario = 1,
    }
}
