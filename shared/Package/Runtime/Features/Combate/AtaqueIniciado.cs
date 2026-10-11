namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor avisa quem está na área que um ataque começou o preparo, para tocar a animação.
    /// O golpe só vale depois de <see cref="PreparoMs"/>; o resultado vem em
    /// <see cref="AtaqueAcertou"/> (que não chega se o golpe não pegou ninguém).
    /// Id: <see cref="CombateMessageIds.AtaqueIniciado"/>.
    /// </summary>
    [System.Serializable]
    public class AtaqueIniciado
    {
        public long AtacanteId = 0;
        public TipoDeAtaque Tipo = TipoDeAtaque.Leve;

        /// <summary>Direção do golpe, em graus (mesma convenção de <see cref="IntencaoDeAtaque.DirecaoY"/>).</summary>
        public float DirecaoY = 0f;

        /// <summary>Milissegundos entre o começo do ataque e o momento de acerto.</summary>
        public int PreparoMs = 0;
    }
}
