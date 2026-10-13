using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// A guarda segurada (como no New World): sem parry e sem janela de timing. O golpe custa
    /// stamina; com stamina para o custo ele zera, sem stamina a guarda quebra. Regra em
    /// docs/features/combate.md ("Guarda segurada"). O servidor decide; o client só informa que
    /// a guarda está segurada e usa a velocidade para andar mais devagar.
    /// </summary>
    public static class RegrasGuarda
    {
        // ── Balanceamento (proposta, a calibrar) ─────────────────────────────────

        /// <summary>Stamina gasta por ponto de dano do golpe bloqueado, antes da defesa do equipamento.</summary>
        public const int StaminaPorDanoBloqueado = 1;

        /// <summary>Fração da velocidade de corrida permitida com a guarda segurada (0,4 = 40%).</summary>
        public const double VelocidadeComGuarda = 0.4;

        /// <summary>Segundos do limite de velocidade pedido ao Mundo cada vez que a guarda sobe.</summary>
        public const double LimiteDeVelocidadeSegundos = 30.0;

        // ── Regras ────────────────────────────────────────────────────────────────

        /// <summary>A stamina que um golpe de <paramref name="dano"/> custa a quem segura a guarda.</summary>
        public static int CustoDoBloqueio(int dano) =>
            (int)Math.Min(int.MaxValue, (long)Math.Max(0, dano) * StaminaPorDanoBloqueado);

        /// <summary>Se a guarda segura o golpe: a stamina atual paga o custo inteiro.</summary>
        public static bool Segura(int staminaAtual, int dano) =>
            staminaAtual >= CustoDoBloqueio(dano);
    }
}
