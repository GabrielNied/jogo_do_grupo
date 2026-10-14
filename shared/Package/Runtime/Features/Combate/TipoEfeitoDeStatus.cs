namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Os seis efeitos de status básicos da fatia jogável (#171, combate.md).
    /// </summary>
    public enum TipoEfeitoDeStatus
    {
        /// <summary>−30% de velocidade; não acumula; reaplicar renova a duração.</summary>
        Lentidao = 1,

        /// <summary>Não age; no máximo 2 s; depois 6 s de imunidade a atordoar.</summary>
        Atordoado = 2,

        /// <summary>Dano por segundo; até 3 acúmulos.</summary>
        Sangramento = 3,

        /// <summary>Dano por segundo; até 3 acúmulos.</summary>
        Queimadura = 4,

        /// <summary>+15% de dano recebido; não acumula; reaplicar renova a duração.</summary>
        Vulneravel = 5,

        /// <summary>+15% de dano causado; não acumula; reaplicar renova a duração.</summary>
        Fortalecido = 6,
    }
}
