namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Regras puras da troca de arma (docs/features/combate.md, "Duas armas"). A troca não tem
    /// recarga própria: só ocupa o personagem por <see cref="DuracaoDaTrocaSegundos"/>, e nesse
    /// tempo golpe, esquiva e guarda são recusados. A recarga das skills é de cada arma e não
    /// muda com a troca. O servidor decide; o client usa para antecipar o bloqueio.
    /// </summary>
    public static class RegrasTrocaDeArma
    {
        /// <summary>Segundos em que o personagem não golpeia, esquiva nem guarda ao trocar.</summary>
        public const double DuracaoDaTrocaSegundos = 0.4;

        /// <summary>Duração em milissegundos, como vai em <see cref="TrocaDeArmaIniciada.DuracaoMs"/>.</summary>
        public static int DuracaoMs => (int)System.Math.Round(DuracaoDaTrocaSegundos * 1000.0);

        /// <summary>O conjunto que passa a valer ao trocar: 1 vira 2 e 2 vira 1.</summary>
        public static int ConjuntoSeguinte(int conjuntoAtivo) => conjuntoAtivo == 1 ? 2 : 1;
    }
}
