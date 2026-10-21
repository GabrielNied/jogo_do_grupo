namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>Pedido HTTP para redistribuir todos os pontos da árvore de atributos.</summary>
    [System.Serializable]
    public sealed class RedistribuirRequest
    {
        /// <summary>Versão da configuração que o client está visualizando.</summary>
        public int VersaoConfiguracao;
    }
}
