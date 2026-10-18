namespace JogoDoGrupo.Shared.Core.Moderacao
{
    /// <summary>
    /// Pedido de denúncia de outro jogador (comportamento, trapaça, ofensa, etc.).
    /// Id: <see cref="TempoReal.TempoRealIds.DenunciarJogador"/>.
    /// </summary>
    [System.Serializable]
    public class DenunciarJogador
    {
        public string AlvoNome = "";
        public string Motivo = "";
        public string Detalhes = "";
    }
}
