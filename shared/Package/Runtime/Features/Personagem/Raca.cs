namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Raça do personagem. Trafega como número no JSON. 0 não é raça válida
    /// (o servidor responde 400 <c>RacaInvalida</c>). Os valores já gravados no banco não mudam:
    /// 1 era o Humano provisório e agora é o Valdren (humanos da fronteira).
    /// </summary>
    public enum Raca
    {
        Valdren = 1,
        Aurideo = 2,
        Granitico = 3,
        Cinereo = 4,
        Silvano = 5,
    }
}
