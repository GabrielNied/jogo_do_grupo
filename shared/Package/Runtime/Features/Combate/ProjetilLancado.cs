using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor avisa quem está na área que um projétil foi disparado no mundo.
    /// Id: <see cref="CombateMessageIds.ProjetilLancado"/> (6060).
    /// </summary>
    [Serializable]
    public class ProjetilLancado
    {
        public long ProjetilId;
        public long AtacanteId;
        public TipoDeProjetil TipoProjetil;
        public TipoDeAtaque TipoAtaque;
        public float OrigemX;
        public float OrigemY;
        public float OrigemZ;
        public float DirecaoX;
        public float DirecaoY;
        public float DirecaoZ;
        public float Velocidade;
        public float Raio;
        public float AlcanceMaximo;
    }
}
