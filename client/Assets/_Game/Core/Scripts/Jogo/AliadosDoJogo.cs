using System;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Consulta se um personagem é aliado (está no mesmo grupo), sem referência direta entre
    /// Combate e Social. O Social registra a consulta ao inicializar ou mudar de grupo.
    /// </summary>
    public static class AliadosDoJogo
    {
        public static Func<long, bool> EhAliado { get; set; }

        public static bool ConsultarEhAliado(long personagemId) =>
            EhAliado != null && EhAliado(personagemId);
    }
}
