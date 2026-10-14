namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Códigos de erro de Personagem que o servidor coloca em <c>ErroApi.Codigo</c>.
    /// Os códigos de conta e os genéricos ficam em <c>CodigosErro</c> (Core).
    /// O status HTTP de cada um está indicado ao lado.
    /// </summary>
    public static class CodigosErroPersonagem
    {
        /// <summary>400: nome de personagem fora das regras de <see cref="RegrasPersonagem"/>.</summary>
        public const string NomeInvalido = "NomeInvalido";

        /// <summary>409: já existe personagem com esse nome (sem diferenciar maiúsculas).</summary>
        public const string NomeJaExiste = "NomeJaExiste";

        /// <summary>409: a conta já tem <see cref="RegrasPersonagem.MaximoPorConta"/> personagens.</summary>
        public const string LimitePersonagens = "LimitePersonagens";

        /// <summary>400: valor que não existe no enum <see cref="Raca"/>.</summary>
        public const string RacaInvalida = "RacaInvalida";

        /// <summary>404: o personagem não existe ou não é da conta do token.</summary>
        public const string PersonagemNaoEncontrado = "PersonagemNaoEncontrado";

        /// <summary>400: a confirmação do nome não confere com o nome do personagem.</summary>
        public const string ConfirmacaoDeNomeIncorreta = "ConfirmacaoDeNomeIncorreta";

        /// <summary>400: tom de pele, cabelo ou cor de cabelo fora dos limites permitidos.</summary>
        public const string AparenciaInvalida = "AparenciaInvalida";
    }
}
