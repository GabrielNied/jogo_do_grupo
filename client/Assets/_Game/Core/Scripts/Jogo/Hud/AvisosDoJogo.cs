namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Mensagens de sistema do HUD (parte de baixo, centro): o ponto do Core por onde as
    /// funcionalidades avisam a pessoa ("Ana entrou no grupo", "Inventário cheio", "Vigor
    /// insuficiente"), sem conhecer a interface umas das outras. Regras em
    /// <c>docs/features/interface.md</c>, seção "Mensagens de sistema".
    /// </summary>
    /// <remarks>
    /// Até 5 linhas, cada uma some depois de 6 s; a mesma mensagem (texto e tipo) repetida em
    /// menos de 2 s vira contador ("x2") e recomeça o tempo. O texto é o que a pessoa lê:
    /// quem publica traduz o código de erro do servidor. Só mostra; não decide nada.
    /// </remarks>
    public static class AvisosDoJogo
    {
        /// <summary>
        /// Mostra a mensagem. Fora do mundo (sem HUD) ou com texto vazio, não faz nada.
        /// </summary>
        public static void Mostrar(string texto, TipoDeAviso tipo = TipoDeAviso.Informacao)
        {
            Hud.Avisos?.Mostrar(texto, tipo);
        }
    }
}
