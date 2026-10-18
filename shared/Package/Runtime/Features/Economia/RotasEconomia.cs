namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Rotas HTTP de Economia (saldo, loja e reparo). Exigem autenticação e personagem válido.
    /// </summary>
    public static class RotasEconomia
    {
        public const string Saldo = "/api/personagens/{personagemId}/economia/saldo";
        public const string Loja = "/api/personagens/{personagemId}/economia/loja";
        public const string Comprar = "/api/personagens/{personagemId}/economia/loja/comprar";
        public const string Vender = "/api/personagens/{personagemId}/economia/loja/vender";
        public const string Reparar = "/api/personagens/{personagemId}/economia/reparar";
        public const string RepararTudo = "/api/personagens/{personagemId}/economia/reparar-tudo";

        public static string SaldoDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/economia/saldo";

        public static string LojaDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/economia/loja";

        public static string ComprarDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/economia/loja/comprar";

        public static string VenderDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/economia/loja/vender";

        public static string RepararDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/economia/reparar";

        public static string RepararTudoDe(long personagemId) =>
            "/api/personagens/" + personagemId + "/economia/reparar-tudo";
    }
}
