namespace JogoDoGrupo.Shared.Core.Moderacao
{
    /// <summary>
    /// Pedido de execução de comando administrativo de GM (via chat ou interface).
    /// Id: <see cref="TempoReal.TempoRealIds.PedirComandoGm"/>.
    /// </summary>
    [System.Serializable]
    public class PedirComandoGm
    {
        public string Comando = "";
        public string Argumentos = "";
    }
}
