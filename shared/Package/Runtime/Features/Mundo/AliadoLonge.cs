namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Um membro do grupo que está na cidade, mas fora do alcance de visão: só o ponto no mapa.</summary>
    [System.Serializable]
    public class AliadoLonge
    {
        public long PersonagemId = 0;
        public float X = 0f;
        public float Z = 0f;
    }
}
