namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Raça do personagem. Trafega como número no JSON. 0 não é raça válida
    /// (o servidor responde 400 <c>RacaInvalida</c>). Humano é provisória.
    /// </summary>
    public enum Raca
    {
        Humano = 1,
    }
}
