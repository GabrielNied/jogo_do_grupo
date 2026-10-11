namespace JogoDoGrupo.Shared.Core.Api
{
    /// <summary>
    /// Códigos de erro de conta e genéricos que o servidor coloca em <see cref="ErroApi.Codigo"/>.
    /// Cada funcionalidade define os próprios em <c>Features/&lt;X&gt;/CodigosErro&lt;X&gt;</c>
    /// (ex.: <c>CodigosErroPersonagem</c>). O status HTTP de cada um está indicado ao lado.
    /// </summary>
    public static class CodigosErro
    {
        /// <summary>400: usuário fora das regras de <c>RegrasConta</c> (só no registro).</summary>
        public const string UsuarioInvalido = "UsuarioInvalido";

        /// <summary>400: senha fora das regras de <c>RegrasConta</c> (só no registro).</summary>
        public const string SenhaInvalida = "SenhaInvalida";

        /// <summary>
        /// 409: já existe conta com esse usuário, comparando sem diferenciar maiúsculas
        /// ("Ana" bloqueia "ana").
        /// </summary>
        public const string UsuarioJaExiste = "UsuarioJaExiste";

        /// <summary>
        /// 401: usuário ou senha errados no login. O usuário é comparado sem diferenciar
        /// maiúsculas; a senha, exatamente.
        /// </summary>
        public const string CredenciaisInvalidas = "CredenciaisInvalidas";

        /// <summary>
        /// 401: rota protegida chamada sem token, com token inválido ou expirado. O servidor
        /// escreve <see cref="ErroApi"/> também no 401 gerado pela autenticação JWT
        /// (ex.: em <c>JwtBearerEvents.OnChallenge</c>). O client trata qualquer 401 em rota
        /// protegida como <c>NaoAutenticado</c>, mesmo sem corpo.
        /// </summary>
        public const string NaoAutenticado = "NaoAutenticado";

        /// <summary>400: corpo ausente ou JSON malformado, em qualquer rota.</summary>
        public const string RequisicaoInvalida = "RequisicaoInvalida";

        /// <summary>403: usuário autenticado sem permissão para a rota ou o recurso.</summary>
        public const string AcessoNegado = "AcessoNegado";

        /// <summary>404: a rota não existe.</summary>
        public const string RotaNaoEncontrada = "RotaNaoEncontrada";

        /// <summary>405: a rota existe, mas não aceita esse método HTTP.</summary>
        public const string MetodoNaoPermitido = "MetodoNaoPermitido";

        /// <summary>503: banco ou servidor ocupado; o client pode tentar de novo.</summary>
        public const string ServidorOcupado = "ServidorOcupado";

        /// <summary>500: falha inesperada no servidor.</summary>
        public const string ErroInterno = "ErroInterno";
    }
}
