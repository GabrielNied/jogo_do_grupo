namespace JogoDoGrupo.Shared.Core.Contas
{
    /// <summary>Rotas HTTP de contas. Nenhuma exige token.</summary>
    public static class RotasContas
    {
        /// <summary><c>POST</c>, corpo <see cref="RegistrarContaRequest"/>, responde 201 sem corpo.</summary>
        public const string Registrar = "/api/contas/registrar";

        /// <summary><c>POST</c>, corpo <see cref="LoginRequest"/>, responde 200 <see cref="LoginResponse"/>.</summary>
        public const string Login = "/api/contas/login";
    }
}
