namespace JogoDoGrupo.Shared.Core.Contas
{
    /// <summary>Rotas HTTP de contas. Só as indicadas exigem token.</summary>
    public static class RotasContas
    {
        /// <summary>
        /// <c>POST</c> com token, corpo <see cref="TrocarSenhaRequest"/>, responde 200
        /// <see cref="LoginResponse"/> com o token novo.
        /// </summary>
        public const string TrocarSenha = "/api/contas/senha";

        /// <summary><c>POST</c>, corpo <see cref="RegistrarContaRequest"/>, responde 201 sem corpo.</summary>
        public const string Registrar = "/api/contas/registrar";

        /// <summary><c>POST</c>, corpo <see cref="LoginRequest"/>, responde 200 <see cref="LoginResponse"/>.</summary>
        public const string Login = "/api/contas/login";

        /// <summary><c>GET</c> com token, responde 200 <see cref="DadosContaResponse"/>.</summary>
        public const string Dados = "/api/contas/dados";

        /// <summary>
        /// <c>POST</c> com token, corpo <see cref="DefinirEmailRequest"/>, responde 204 sem corpo.
        /// </summary>
        public const string DefinirEmail = "/api/contas/email";

        /// <summary>
        /// <c>POST</c> sem token, corpo <see cref="PedirCodigoRecuperacaoRequest"/>, responde 202
        /// sem corpo exista ou não o e-mail.
        /// </summary>
        public const string PedirCodigoRecuperacao = "/api/contas/recuperacao/pedir";

        /// <summary>
        /// <c>POST</c> sem token, corpo <see cref="RedefinirSenhaRequest"/>, responde 204 sem corpo.
        /// </summary>
        public const string RedefinirSenha = "/api/contas/recuperacao/redefinir";
    }
}
