using System;

namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor → client: aviso de que o pedido de inspeção não pôde ser atendido.
    /// Id: <see cref="SocialMessageIds.InspecaoRecusada"/>.
    /// </summary>
    [Serializable]
    public class InspecaoRecusada
    {
        public long PersonagemId;
        public CodigoErroInspecao Motivo;
    }
}
