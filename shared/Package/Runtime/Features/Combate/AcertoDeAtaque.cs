namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Um alvo atingido por um golpe, já com o dano que o servidor aplicou. Vem dentro de
    /// <see cref="AtaqueAcertou.Acertos"/>.
    /// </summary>
    [System.Serializable]
    public class AcertoDeAtaque
    {
        /// <summary>Id do alvo de combate, dado pelo servidor (não é id de personagem).</summary>
        public long AlvoId = 0;

        public int Dano = 0;
        public int VidaRestante = 0;
        public int VidaMaxima = 0;
    }
}
