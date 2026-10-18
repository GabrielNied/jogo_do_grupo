namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>
    /// Servidor → Desafiante: o pedido de duelo foi recusado ou falhou.
    /// </summary>
    [System.Serializable]
    public class DueloRecusado
    {
        public long AlvoPersonagemId;
        public string Motivo = "";
    }
}
