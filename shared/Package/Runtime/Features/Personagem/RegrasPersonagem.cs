#nullable enable

using JogoDoGrupo.Shared.Core.Api;

namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Regras de criação de personagem. O client usa <see cref="ValidarNome"/> para avisar antes
    /// de enviar; o servidor valida de novo e checa sozinho a unicidade do nome e o limite.
    /// </summary>
    public static class RegrasPersonagem
    {
        public const int MaximoPorConta = 4;
        public const int NomeMinimo = 3;
        public const int NomeMaximo = 16;

        /// <summary>
        /// Nome com <see cref="NomeMinimo"/> a <see cref="NomeMaximo"/> caracteres, todos letras
        /// (<see cref="char.IsLetter(char)"/>): sem espaços, números nem símbolos.
        /// </summary>
        /// <returns><see cref="CodigosErro.NomeInvalido"/> ou <c>null</c> se válido.</returns>
        public static string? ValidarNome(string? nome)
        {
            if (nome == null || nome.Length < NomeMinimo || nome.Length > NomeMaximo)
            {
                return CodigosErro.NomeInvalido;
            }

            foreach (var c in nome)
            {
                if (!char.IsLetter(c))
                {
                    return CodigosErro.NomeInvalido;
                }
            }

            return null;
        }
    }
}
