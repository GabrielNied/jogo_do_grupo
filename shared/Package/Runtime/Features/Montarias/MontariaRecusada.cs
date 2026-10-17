using System;

namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Notifica o client quando um pedido de montaria ou uso de item é recusado.
    /// </summary>
    [Serializable]
    public class MontariaRecusada
    {
        public string Codigo = "";
        public string Motivo = "";
    }
}
