namespace JogoDoGrupo.Shared.Features.Instancias
{
    /// <summary>Servidor informa por que recusou o pedido de entrada na dungeon.</summary>
    [System.Serializable]
    public class EntradaRecusada
    {
        /// <summary>Um dos códigos de <see cref="CodigosErroInstancias"/>.</summary>
        public string Codigo = string.Empty;
    }
}
