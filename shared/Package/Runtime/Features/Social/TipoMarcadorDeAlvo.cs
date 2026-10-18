namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Tipos de marcadores táticos de grupo para coordenação em combate e dungeons.
    /// </summary>
    public enum TipoMarcadorDeAlvo : byte
    {
        /// <summary>Nenhum marcador atribuído (usado também para desmarcar).</summary>
        Nenhum = 0,

        /// <summary>Caveira (Foco primário de dano / Kill target).</summary>
        Caveira = 1,

        /// <summary>Cruz / X (Foco secundário de dano).</summary>
        Cruz = 2,

        /// <summary>Escudo (Alvo prioritário do tank / Ameaça).</summary>
        Escudo = 3,

        /// <summary>Lua (Alvo para controle de grupo / CC / Atordoar).</summary>
        Lua = 4,

        /// <summary>Estrela (Alvo utilitário / Foco especial).</summary>
        Estrela = 5,
    }
}
