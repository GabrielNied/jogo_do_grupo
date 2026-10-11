namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Qual ataque telegrafado um inimigo está preparando, em <see cref="InimigoPreparaAtaque.Tipo"/>.
    /// Os valores são estáveis: nunca mude nem reaproveite um número já publicado.
    /// </summary>
    public enum TipoDeAtaqueDeInimigo
    {
        /// <summary>O golpe do inimigo comum: um círculo à frente dele.</summary>
        Golpe = 0,

        /// <summary>Boss: avança até o ponto marcado e acerta quem estiver nele.</summary>
        Investida = 1,

        /// <summary>Boss: pisada em volta dele, uma área grande centrada nele.</summary>
        Pisada = 2,

        /// <summary>Boss: raios que caem em pontos marcados no chão (um círculo por raio).</summary>
        Raios = 3,
    }
}
