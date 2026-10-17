namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Climas possíveis do mundo (issue #902, docs/features/mundo.md).
    /// O servidor sorteia o clima e todos na área recebem o mesmo.
    /// </summary>
    public enum TipoDeClima : byte
    {
        /// <summary>Tempo limpo padrão; sem penalidades de visão ou movimento.</summary>
        Limpo = 0,

        /// <summary>Chuva moderada; reduz o alcance de visão em campo, mata e colinas.</summary>
        Chuva = 1,

        /// <summary>Neblina densa; fecha o alcance de visão significativamente no pântano, mata e colinas.</summary>
        Neblina = 2,

        /// <summary>Nevasca intensa; fecha a visão e reduz o movimento nas montanhas de neve.</summary>
        Nevasca = 3,
    }
}
