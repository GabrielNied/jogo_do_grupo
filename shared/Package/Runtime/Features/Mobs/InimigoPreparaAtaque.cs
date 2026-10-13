namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Servidor avisa quem está na área que um inimigo começou o aviso de um golpe. No golpe comum
    /// (<see cref="TipoDeAtaqueDeInimigo.Golpe"/>) não há área no chão: <see cref="DirecaoY"/> é a
    /// direção travada, <see cref="Areas"/> vem vazio e <see cref="Raio"/> é zero; o dano sai no fim
    /// do preparo, num cone à frente do corpo. Nos ataques do boss a área é um círculo no chão,
    /// fixo desde o aviso: quem estiver dentro dele quando o preparo acabar leva o dano; quem
    /// saiu a tempo não leva. O ataque de um boss tem um
    /// <see cref="Tipo"/> e pode ter mais de uma área (<see cref="Areas"/>).
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

        /// <summary>Qual ataque é. O inimigo comum usa <see cref="TipoDeAtaqueDeInimigo.Golpe"/>.</summary>
        public TipoDeAtaqueDeInimigo Tipo = TipoDeAtaqueDeInimigo.Golpe;

        /// <summary>
        /// Todas as áreas do ataque (o golpe comum não tem: vem vazio). <see cref="CentroX"/>,
        /// <see cref="CentroZ"/> e <see cref="Raio"/> repetem a primeira (zero se não há). Nunca nulo.
        /// </summary>
        public AreaDeAtaque[] Areas = System.Array.Empty<AreaDeAtaque>();
    }
}
