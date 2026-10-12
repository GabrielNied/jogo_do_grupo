namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Retrato de um inimigo vivo no mundo. Vem em <see cref="InimigosDaArea.Inimigos"/> e em
    /// <see cref="InimigoApareceu.Inimigo"/>.
    /// </summary>
    [System.Serializable]
    public class InimigoNoMundo
    {
        /// <summary>
        /// Id do inimigo, dado pelo servidor. É o mesmo <c>AlvoId</c> que o Combate manda em
        /// <c>AcertoDeAtaque</c> quando um golpe o acerta.
        /// </summary>
        public long InimigoId = 0;

        /// <summary>Id da ficha em <see cref="CatalogoCriaturas"/> (nome, nível, categoria).</summary>
        public int CriaturaId = 0;

        public VarianteDeCriatura Variante = VarianteDeCriatura.Normal;

        /// <summary>
        /// Nível deste inimigo, sorteado na faixa do ponto onde ele nasce. Conta em vida, dano e
        /// experiência (a ficha em <see cref="CatalogoCriaturas"/> tem o nível de referência).
        /// </summary>
        public int Nivel = 0;

        public float X = 0f;
        public float Y = 0f;
        public float Z = 0f;

        /// <summary>Para onde ele olha, em graus (0 aponta para +Z e cresce para +X).</summary>
        public float RotacaoY = 0f;

        public int Vida = 0;
        public int VidaMaxima = 0;

        public EstadoDeInimigo Estado = EstadoDeInimigo.Repouso;

        /// <summary>
        /// Personagem que ele persegue ou ataca; <see cref="RegrasInimigo.SemAlvo"/> se nenhum.
        /// </summary>
        public long AlvoId = 0;
    }
}
