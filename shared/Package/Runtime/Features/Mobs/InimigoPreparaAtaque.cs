namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Servidor avisa quem está na área que um inimigo começou o aviso de um golpe. A área do
    /// golpe é um círculo no chão, fixo desde o aviso: quem estiver dentro dele quando o
    /// preparo acabar leva o dano; quem saiu a tempo não leva.
    /// Id: <see cref="MobsMessageIds.InimigoPreparaAtaque"/>.
    /// </summary>
    [System.Serializable]
    public class InimigoPreparaAtaque
    {
        public long InimigoId = 0;

        /// <summary>Milissegundos entre este aviso e o golpe.</summary>
        public int PreparoMs = 0;

        /// <summary>Centro da área do golpe, no plano do chão.</summary>
        public float CentroX = 0f;

        /// <summary>Centro da área do golpe, no plano do chão.</summary>
        public float CentroZ = 0f;

        /// <summary>Raio da área do golpe, em metros.</summary>
        public float Raio = 0f;

        /// <summary>Para onde o golpe vai, em graus (0 aponta para +Z e cresce para +X).</summary>
        public float DirecaoY = 0f;
    }
}
