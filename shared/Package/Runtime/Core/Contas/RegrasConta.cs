#nullable enable

using JogoDoGrupo.Shared.Core.Api;

namespace JogoDoGrupo.Shared.Core.Contas
{
    /// <summary>
    /// Regras de formato de usuário e senha. O client usa para avisar antes de enviar;
    /// o servidor valida de novo no registro e responde 400 com o código retornado.
    /// O usuário é único sem diferenciar maiúsculas ("Ana" e "ana" são a mesma conta), o login
    /// compara sem diferenciar maiúsculas e o servidor guarda o usuário como foi digitado no registro.
    /// Essas checagens são só do servidor.
    /// </summary>
    public static class RegrasConta
    {
        public const int UsuarioMinimo = 3;
        public const int UsuarioMaximo = 20;
        public const int SenhaMinimo = 8;
        public const int SenhaMaximo = 64;

        /// <summary>
        /// Usuário com <see cref="UsuarioMinimo"/> a <see cref="UsuarioMaximo"/> caracteres,
        /// só letras sem acento (a-z, A-Z), números (0-9) e "_".
        /// </summary>
        /// <returns><see cref="CodigosErro.UsuarioInvalido"/> ou <c>null</c> se válido.</returns>
        public static string? ValidarUsuario(string? usuario)
        {
            if (usuario == null || usuario.Length < UsuarioMinimo || usuario.Length > UsuarioMaximo)
            {
                return CodigosErro.UsuarioInvalido;
            }

            foreach (var c in usuario)
            {
                var valido =
                    (c >= 'a' && c <= 'z')
                    || (c >= 'A' && c <= 'Z')
                    || (c >= '0' && c <= '9')
                    || c == '_';
                if (!valido)
                {
                    return CodigosErro.UsuarioInvalido;
                }
            }

            return null;
        }

        /// <summary>Senha com <see cref="SenhaMinimo"/> a <see cref="SenhaMaximo"/> caracteres.</summary>
        /// <returns><see cref="CodigosErro.SenhaInvalida"/> ou <c>null</c> se válida.</returns>
        public static string? ValidarSenha(string? senha)
        {
            if (senha == null || senha.Length < SenhaMinimo || senha.Length > SenhaMaximo)
            {
                return CodigosErro.SenhaInvalida;
            }

            return null;
        }
    }
}
