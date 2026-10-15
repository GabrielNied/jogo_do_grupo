using System;

namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Servidor → client (broadcast): estado de montaria de um personagem.
    /// Id: <see cref="MontariasMessageIds.EstadoMontaria"/>.
    /// </summary>
    [Serializable]
    public class EstadoMontaria
    {
        public long PersonagemId;
        public bool Montado;
        public int TipoMontaria;
    }
}
