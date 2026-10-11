namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Um círculo no chão que um ataque telegrafado vai acertar. Fixo desde o aviso: quem estiver
    /// dentro dele quando o preparo acabar leva o dano. Vem em
    /// <see cref="InimigoPreparaAtaque.Areas"/>.
    /// </summary>
    [System.Serializable]
    public class AreaDeAtaque
    {
        /// <summary>Centro da área, no plano do chão.</summary>
        public float CentroX = 0f;

        /// <summary>Centro da área, no plano do chão.</summary>
        public float CentroZ = 0f;

        /// <summary>Raio da área, em metros.</summary>
        public float Raio = 0f;
    }
}
