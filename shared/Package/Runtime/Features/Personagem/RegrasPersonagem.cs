#nullable enable

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
        /// Nome com <see cref="NomeMinimo"/> a <see cref="NomeMaximo"/> caracteres, só letras ASCII
        /// A-Z e a-z (equivale a <c>^[A-Za-z]{3,16}$</c>): sem acentos, espaços, números nem símbolos.
        /// Aceitar acentos é questão em aberto para o grupo (ver <c>docs/features/personagem.md</c>).
        /// </summary>
        /// <returns><see cref="CodigosErroPersonagem.NomeInvalido"/> ou <c>null</c> se válido.</returns>
        public static string? ValidarNome(string? nome)
        {
            if (nome == null || nome.Length < NomeMinimo || nome.Length > NomeMaximo)
            {
                return CodigosErroPersonagem.NomeInvalido;
            }

            foreach (var c in nome)
            {
                var letraAscii = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
                if (!letraAscii)
                {
                    return CodigosErroPersonagem.NomeInvalido;
                }
            }

            return null;
        }
    }
}
