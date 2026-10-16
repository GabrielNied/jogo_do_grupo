using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor avisa ao dono que a defesa de equipamento foi insuficiente para bloquear completamente o impacto
    /// do golpe. Id: <see cref="CombateMessageIds.DefesaInsuficiente"/>.
    /// </summary>
    [Serializable]
    public class DefesaInsuficiente
    {
        public long PersonagemId = 0;
        public int DanoResidual = 0;
        public int DanoOriginal = 0;
    }
}
