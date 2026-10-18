namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Elemento mágico ou afinidade da arma. Aplicável principalmente a cajados (Fogo, Gelo, Cura).
    /// Armas convencionais ou sem afinidade elemental usam <see cref="Nenhum"/>.
    /// </summary>
    public enum ElementoDaArma
    {
        Nenhum = 0,
        Fogo = 1,
        Gelo = 2,
        Cura = 3,
    }
}
