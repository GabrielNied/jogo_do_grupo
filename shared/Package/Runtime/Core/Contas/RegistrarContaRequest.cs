namespace JogoDoGrupo.Shared.Core.Contas
{
    /// <summary>
    /// Client envia em <c>POST</c> <see cref="RotasContas.Registrar"/> (sem token).
    /// Servidor responde 201 sem corpo (o client faz login em seguida),
    /// 400 <c>UsuarioInvalido</c> ou <c>SenhaInvalida</c> (regras de <see cref="RegrasConta"/>)
    /// ou 409 <c>UsuarioJaExiste</c>, com corpo <c>ErroApi</c> nos erros.
    /// </summary>
    [System.Serializable]
    public class RegistrarContaRequest
    {
        public string Usuario = "";
        public string Senha = "";
    }
}
