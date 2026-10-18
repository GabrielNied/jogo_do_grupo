namespace JogoDoGrupo.Shared.Core.Moderacao
{
    /// <summary>
    /// Resposta do servidor ao recebimento de uma denúncia.
    /// Id: <see cref="TempoReal.TempoRealIds.ResultadoDenuncia"/>.
    /// </summary>
    [System.Serializable]
    public class ResultadoDenuncia
    {
        public bool Sucesso = false;
        public string Mensagem = "";
    }
}
