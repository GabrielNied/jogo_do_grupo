namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Códigos de erro de Itens que o servidor coloca em <c>ErroApi.Codigo</c>. Os códigos de
    /// conta e os genéricos (inclusive <c>NaoAutenticado</c>, 401) ficam em <c>CodigosErro</c>
    /// (Core). O status HTTP de cada um está indicado ao lado.
    /// </summary>
    public static class CodigosErroItens
    {
        /// <summary>
        /// 409: os itens sorteados não cabem nos <see cref="RegrasItens.EspacosInventario"/>
        /// espaços. O servidor não grava nada nesse caso.
        /// </summary>
        public const string InventarioCheio = "InventarioCheio";

        /// <summary>
        /// 404: não existe personagem com esse id, ou ele é de outra conta (os dois casos
        /// respondem igual). Nem Core nem Personagem tinham um código para isso em 2026-10-11.
        /// </summary>
        public const string PersonagemNaoEncontrado = "PersonagemNaoEncontrado";
    }
}
