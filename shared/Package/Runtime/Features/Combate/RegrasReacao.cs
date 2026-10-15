namespace JogoDoGrupo.Shared.Features.Combate
{
    using JogoDoGrupo.Shared.Features.Mobs;

    /// <summary>
    /// A reação ao acerto: um golpe confirmado pelo servidor interrompe a ação comum de quem o
    /// levou e o deixa sem esquivar nem atacar por um instante. Regra em
    /// docs/features/combate.md ("Reação ao acerto"). O servidor decide; o client usa os mesmos
    /// números para segurar a própria próxima ação.
    /// </summary>
    public static class RegrasReacao
    {
        // ── Balanceamento (proposta, a calibrar) ─────────────────────────────────

        /// <summary>
        /// Milissegundos em que quem foi interrompido não começa esquiva nem ataque. A pausa
        /// visual de 70 ms cabe dentro dela.
        /// </summary>
        public const int AtordoamentoNoAcertoMs = 300;

        /// <summary>
        /// Milissegundos, depois do atordoamento, em que um acerto entra mas não interrompe nem
        /// começa outra reação (o combo não prende).
        /// </summary>
        public const int FolgaDepoisDoAcertoMs = 600;

        /// <summary>
        /// Milissegundos em que o client segura a pose de quem bateu e de quem levou, quando o
        /// servidor confirma o acerto. Só apresentação: o relógio do servidor não para.
        /// </summary>
        public const int PausaVisualNoAcertoMs = 70;

        // ── Regras ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Se um acerto em <paramref name="agoraMs"/> começa uma reação: não pode cair dentro do
        /// atordoamento em curso nem da folga que vem depois dele (<paramref name="folgaAteMs"/>).
        /// </summary>
        public static bool PodeReagir(long agoraMs, long folgaAteMs) => agoraMs >= folgaAteMs;

        /// <summary>Quando o atordoamento de uma reação que começou em <paramref name="inicioMs"/> acaba.</summary>
        public static long FimDoAtordoamentoMs(long inicioMs) => inicioMs + AtordoamentoNoAcertoMs;

        /// <summary>Quando a folga de uma reação que começou em <paramref name="inicioMs"/> acaba.</summary>
        public static long FimDaFolgaMs(long inicioMs) =>
            inicioMs + AtordoamentoNoAcertoMs + FolgaDepoisDoAcertoMs;

        /// <summary>
        /// Se um ataque de inimigo interrompe a ação e atordoa o jogador.
        /// Golpes comuns (<see cref="TipoDeAtaqueDeInimigo.Golpe"/>) não atordoam nem interrompem o jogador;
        /// golpes especiais (investida, pisada, raios, etc.) interrompem (#795).
        /// </summary>
        public static bool InterrompeJogador(TipoDeAtaqueDeInimigo tipo) =>
            tipo != TipoDeAtaqueDeInimigo.Golpe;
    }
}
