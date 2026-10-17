using System;

namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Marcas de jogabilidade pintadas no mapa além do bioma.</summary>
    [Flags]
    public enum AreaEspecialDoMapa : byte
    {
        Nenhuma = 0,
        Agua = 1,
        Lava = 2,
        Pantano = 4,
        Cidade = 8,
    }
}
