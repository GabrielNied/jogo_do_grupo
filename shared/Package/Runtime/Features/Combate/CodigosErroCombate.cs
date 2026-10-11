namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Códigos de erro de Combate que o servidor coloca em <c>ErroApi.Codigo</c>. Os genéricos
    /// (inclusive <c>NaoAutenticado</c>, 401) ficam em <c>CodigosErro</c> (Core).
    /// </summary>
    public static class CodigosErroCombate
    {
        /// <summary>
        /// 404: não existe personagem com esse id, ou ele é de outra conta (os dois casos
        /// respondem igual). Mesmo valor de <c>CodigosErroItens.PersonagemNaoEncontrado</c>.
        /// </summary>
        public const string PersonagemNaoEncontrado = "PersonagemNaoEncontrado";
    }
}
