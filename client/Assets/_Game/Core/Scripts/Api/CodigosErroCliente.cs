namespace JogoDoGrupo.Client.Core.Api
{
    /// <summary>
    /// Códigos de erro criados pelo próprio client (não vêm do servidor), colocados em
    /// <see cref="RespostaApi.Erro"/> pelo <see cref="ClienteApi"/>.
    /// Os códigos do servidor ficam em <c>JogoDoGrupo.Shared.Core.Api.CodigosErro</c> e nos
    /// <c>CodigosErro&lt;X&gt;</c> de cada funcionalidade.
    /// </summary>
    public static class CodigosErroCliente
    {
        /// <summary>
        /// Não chegou resposta do servidor (fora do ar, sem internet, tempo esgotado).
        /// <see cref="RespostaApi.Status"/> é 0.
        /// </summary>
        public const string SemConexao = "SemConexao";

        /// <summary>
        /// O servidor respondeu, mas sem o corpo esperado: erro sem <c>ErroApi</c> válido,
        /// ou sucesso com JSON ausente ou inválido. Trate como erro genérico.
        /// </summary>
        public const string RespostaInvalida = "RespostaInvalida";
    }
}
