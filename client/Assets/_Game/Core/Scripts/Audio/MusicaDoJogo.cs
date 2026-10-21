namespace JogoDoGrupo.Client.Core.Audio
{
    /// <summary>
    /// A faixa de música do momento (docs/features/interface.md, Áudio): uma por vez, trocada
    /// com transição cruzada. Fora do mundo toca sempre a <see cref="Abertura"/>.
    /// </summary>
    public enum MusicaDoJogo
    {
        Nenhuma,

        /// <summary>Login, seleção e criação de personagem.</summary>
        Abertura,

        /// <summary>A cidade e o terreno em volta (o padrão dentro do mundo).</summary>
        Cidade,

        /// <summary>Entrada e salões da dungeon.</summary>
        Dungeon,

        /// <summary>Luta do chefe.</summary>
        Chefe,

        /// <summary>Toque curto ao vencer o chefe, uma vez; depois volta para a dungeon.</summary>
        Vitoria,
    }
}
