namespace JogoDoGrupo.Client.Core.UI
{
    /// <summary>
    /// Chaves das telas do Core na <see cref="Navegacao"/>.
    /// As funcionalidades usam chaves no formato <c>"&lt;funcionalidade&gt;.&lt;tela&gt;"</c>
    /// (ex.: <c>"personagem.selecao"</c>), declaradas no próprio assembly.
    /// </summary>
    public static class TelasCore
    {
        /// <summary>Abertura/carregamento: primeira tela do jogo; segue sozinha para o login.</summary>
        public const string Abertura = "core.abertura";

        /// <summary>Login e registro de conta.</summary>
        public const string Login = "core.login";

        /// <summary>
        /// Aberta depois do login quando nenhuma funcionalidade definiu a tela seguinte
        /// (<see cref="Navegacao.DefinirTelaAposLogin"/>) ou a chave definida não foi registrada.
        /// </summary>
        public const string SemTelaAposLogin = "core.sem-tela-apos-login";
    }
}
