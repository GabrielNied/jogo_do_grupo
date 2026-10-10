namespace JogoDoGrupo.Shared.Core.Api
{
    /// <summary>
    /// Códigos de erro que o servidor coloca em <see cref="ErroApi.Codigo"/>.
    /// O status HTTP de cada um está indicado ao lado.
    /// </summary>
    public static class CodigosErro
    {
        /// <summary>400: usuário fora das regras de <c>RegrasConta</c> (só no registro).</summary>
        public const string UsuarioInvalido = "UsuarioInvalido";

        /// <summary>400: senha fora das regras de <c>RegrasConta</c> (só no registro).</summary>
        public const string SenhaInvalida = "SenhaInvalida";

        /// <summary>409: já existe conta com esse usuário.</summary>
        public const string UsuarioJaExiste = "UsuarioJaExiste";

        /// <summary>401: usuário ou senha errados no login.</summary>
        public const string CredenciaisInvalidas = "CredenciaisInvalidas";

        /// <summary>401: rota protegida chamada sem token, com token inválido ou expirado.</summary>
        public const string NaoAutenticado = "NaoAutenticado";

        /// <summary>400: nome de personagem fora das regras de <c>RegrasPersonagem</c>.</summary>
        public const string NomeInvalido = "NomeInvalido";

        /// <summary>409: já existe personagem com esse nome (sem diferenciar maiúsculas).</summary>
        public const string NomeJaExiste = "NomeJaExiste";

        /// <summary>409: a conta já tem o máximo de personagens.</summary>
        public const string LimitePersonagens = "LimitePersonagens";

        /// <summary>400: valor que não existe no enum <c>Raca</c>.</summary>
        public const string RacaInvalida = "RacaInvalida";
    }
}
