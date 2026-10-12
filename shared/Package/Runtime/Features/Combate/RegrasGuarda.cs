using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// A guarda segurada (como no New World): sem parry e sem janela de timing. O golpe custa
    /// vigor; com vigor para o custo ele zera, sem vigor a guarda quebra. Regra em
    /// docs/features/combate.md ("Guarda segurada"). O servidor decide; o client só informa que
    /// a guarda está segurada e usa a velocidade para andar mais devagar.
    /// </summary>
    public static class RegrasGuarda
    {
        // ── Balanceamento (proposta, a calibrar) ─────────────────────────────────

        /// <summary>Vigor gasto por ponto de dano do golpe bloqueado, antes da defesa do equipamento.</summary>
        public const int VigorPorDanoBloqueado = 1;

        /// <summary>Fração da velocidade de corrida permitida com a guarda segurada (0,4 = 40%).</summary>
        public const double VelocidadeComGuarda = 0.4;

        /// <summary>Segundos do limite de velocidade pedido ao Mundo cada vez que a guarda sobe.</summary>
        public const double LimiteDeVelocidadeSegundos = 30.0;

        // ── Regras ────────────────────────────────────────────────────────────────

        /// <summary>O vigor que um golpe de <paramref name="dano"/> custa a quem segura a guarda.</summary>
        public static int CustoDoBloqueio(int dano) =>
            (int)Math.Min(int.MaxValue, (long)Math.Max(0, dano) * VigorPorDanoBloqueado);

        /// <summary>Se a guarda segura o golpe: o vigor atual paga o custo inteiro.</summary>
        public static bool Segura(int vigorAtual, int dano) => vigorAtual >= CustoDoBloqueio(dano);
    }
}
