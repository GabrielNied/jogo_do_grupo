namespace JogoDoGrupo.Shared.Core.Contas
{
    /// <summary>
    /// Client envia em <c>POST</c> <see cref="RotasContas.TrocarSenha"/> (com token).
    /// Servidor responde 200 com <see cref="LoginResponse"/> (token novo: os antigos deixam de
    /// valer em HTTP e no tempo real), 400 <c>SenhaAtualIncorreta</c> (conta como falha de login),
    /// 400 <c>SenhaInvalida</c> (senha nova fora de <see cref="RegrasConta"/>) ou 429
    /// <c>MuitasTentativas</c>, com corpo <c>ErroApi</c> nos erros.
    /// </summary>
    [System.Serializable]
    public class TrocarSenhaRequest
    {
        public string SenhaAtual = "";
        public string NovaSenha = "";
    }
}
