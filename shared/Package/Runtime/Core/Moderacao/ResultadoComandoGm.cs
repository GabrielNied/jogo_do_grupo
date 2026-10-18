namespace JogoDoGrupo.Shared.Core.Moderacao
{
    /// <summary>
    /// Resposta do servidor à tentativa de execução de comando GM.
    /// Id: <see cref="TempoReal.TempoRealIds.ResultadoComandoGm"/>.
    /// </summary>
    [System.Serializable]
    public class ResultadoComandoGm
    {
        public bool Sucesso = false;
        public string Mensagem = "";
    }
}
