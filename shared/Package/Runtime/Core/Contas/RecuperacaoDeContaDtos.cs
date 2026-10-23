namespace JogoDoGrupo.Shared.Core.Contas
{
    /// <summary>Servidor responde (200) em <c>GET</c> <see cref="RotasContas.Dados"/>.</summary>
    [System.Serializable]
    public class DadosContaResponse
    {
        public string Usuario = "";

        /// <summary>E-mail de recuperação, ou vazio se a conta não tem.</summary>
        public string Email = "";
    }

    /// <summary>
    /// Client envia em <c>POST</c> <see cref="RotasContas.DefinirEmail"/> (com token). Pede a
    /// senha atual: o e-mail recupera a conta, então não pode mudar só com um token roubado.
    /// <see cref="Email"/> vazio tira o e-mail. Servidor responde 204, 400
    /// <c>SenhaAtualIncorreta</c> (conta como falha de login) ou <c>EmailInvalido</c>, 409
    /// <c>EmailJaEmUso</c> ou 429 <c>MuitasTentativas</c>.
    /// </summary>
    [System.Serializable]
    public class DefinirEmailRequest
    {
        public string SenhaAtual = "";
        public string Email = "";
    }

    /// <summary>
    /// Client envia em <c>POST</c> <see cref="RotasContas.PedirCodigoRecuperacao"/> (sem token).
    /// Servidor responde 202 sem corpo exista ou não a conta, e manda ao e-mail um código de
    /// <see cref="RegrasConta.CodigoRecuperacaoDigitos"/> dígitos válido por
    /// <see cref="RegrasConta.CodigoRecuperacaoMinutos"/> minutos (um pedido novo invalida o anterior).
    /// Acima do limite por endereço, 429 <c>MuitasTentativas</c>.
    /// </summary>
    [System.Serializable]
    public class PedirCodigoRecuperacaoRequest
    {
        public string Email = "";
    }

    /// <summary>
    /// Client envia em <c>POST</c> <see cref="RotasContas.RedefinirSenha"/> (sem token). Servidor
    /// responde 204 (senha trocada, sessões antigas encerradas; o client faz login), 400
    /// <c>CodigoRecuperacaoInvalido</c>, 400 <c>SenhaInvalida</c> ou 429 <c>MuitasTentativas</c>.
    /// </summary>
    [System.Serializable]
    public class RedefinirSenhaRequest
    {
        public string Email = "";
        public string Codigo = "";
        public string NovaSenha = "";
    }
}
