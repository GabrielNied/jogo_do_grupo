namespace JogoDoGrupo.Shared.Core.Contas
{
    /// <summary>
    /// Client envia em <c>POST</c> <see cref="RotasContas.Login"/> (sem token).
    /// Servidor responde 200 com <see cref="LoginResponse"/> ou 401 <c>CredenciaisInvalidas</c>
    /// (corpo <c>ErroApi</c>) para qualquer usuário ou senha errados, inclusive fora do formato.
    /// </summary>
    [System.Serializable]
    public class LoginRequest
    {
        public string Usuario = "";
        public string Senha = "";
    }
}
