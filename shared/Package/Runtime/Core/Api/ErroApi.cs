namespace JogoDoGrupo.Shared.Core.Api
{
    /// <summary>
    /// Corpo de toda resposta de erro da API HTTP (status 4xx).
    /// Enviado pelo servidor; o client lê <see cref="Codigo"/> para decidir o que mostrar.
    /// </summary>
    [System.Serializable]
    public class ErroApi
    {
        /// <summary>
        /// Um dos valores de <see cref="CodigosErro"/> ou dos códigos da funcionalidade
        /// (ex.: <c>CodigosErroPersonagem</c>).
        /// </summary>
        public string Codigo = "";

        /// <summary>Texto para depuração e log. A UI do client não deve depender dele.</summary>
        public string Mensagem = "";
    }
}
