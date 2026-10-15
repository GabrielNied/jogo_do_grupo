using System;

namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Como o grupo de inimigos cerca o alvo sem virar um bolo (issue #487): cada inimigo tem um
    /// posto num anel em volta do alvo, ninguém fica mais perto de outro que a separação mínima,
    /// e só alguns atacam ao mesmo tempo; os outros rondam num anel mais afastado. Regras puras
    /// no plano do chão; o servidor decide e o client só mostra. Não há desvio de obstáculo.
    /// </summary>
    public static class RegrasCerco
    {
        // ── Balanceamento (proposta, a calibrar) ─────────────────────────────────

        /// <summary>Quantos inimigos do grupo preparam ou recuperam ataque contra o mesmo alvo.</summary>
        public const int LimiteDeAtacantesPorAlvo = 2;

        /// <summary>Menor distância, em metros, entre dois inimigos: dois corpos lado a lado.</summary>
        public const double SeparacaoMinimaMetros = 2.0 * RegrasInimigo.RaioDoCorpoMetros;

        /// <summary>
        /// Raio do anel dos atacantes, em metros: dentro do alcance do cone do golpe, para pegar o
        /// alvo mesmo se um vizinho barrar o passo do preparo (#856).
        /// </summary>
        public const double RaioDoAnelMetros = RegrasInimigo.AlcanceDoGolpeMetros - 0.1;

        /// <summary>Raio do anel de quem ronda à espera de vaga, logo fora do anel dos atacantes.</summary>
        public const double RaioDaRondaMetros = RaioDoAnelMetros + SeparacaoMinimaMetros;

        /// <summary>Postos igualmente espaçados em cada anel (o maior grupo comum cabe num anel).</summary>
        public const int PostosPorAnel = RegrasInimigo.TamanhoMaximoDoGrupo;

        // ── Regras ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Se mais um inimigo pode começar a atacar o mesmo alvo, dado quantos já estão
        /// atacando (preparando o golpe ou se recuperando dele).
        /// </summary>
        public static bool PodeAtacar(int atacantesAgora) =>
            atacantesAgora < LimiteDeAtacantesPorAlvo;

        /// <summary>
        /// O posto do inimigo de número <paramref name="indice"/> (0, 1, 2...) em volta do
        /// alvo: o ângulo é fixo por inimigo, para ele não trocar de lugar quando outro morre.
        /// Quem ronda fica no anel de fora. Passado o primeiro anel (grupos maiores que
        /// <see cref="PostosPorAnel"/>), cada anel seguinte é uma separação mais afastado e gira
        /// meio posto, para ninguém ficar atrás de ninguém.
        /// </summary>
        public static void Posto(
            double alvoX,
            double alvoZ,
            int indice,
            bool ronda,
            out double postoX,
            out double postoZ
        )
        {
            int i = Math.Max(0, indice);
            int anel = i / PostosPorAnel;
            double passo = 360.0 / PostosPorAnel;
            double graus = (i % PostosPorAnel) * passo + (anel % 2) * passo / 2.0;
            double raio =
                (ronda ? RaioDaRondaMetros : RaioDoAnelMetros) + anel * SeparacaoMinimaMetros;
            double radianos = graus * Math.PI / 180.0;
            postoX = alvoX + Math.Sin(radianos) * raio;
            postoZ = alvoZ + Math.Cos(radianos) * raio;
        }

        /// <summary>
        /// Empurra o ponto para longe dos vizinhos que estão mais perto que
        /// <see cref="SeparacaoMinimaMetros"/>, o bastante para voltar à separação. Dois
        /// pontos no mesmo lugar são afastados ao longo de +X. O deslocamento resultante pode ser
        /// limitado por <paramref name="maxDeslocamentoMetros"/> para evitar saltos instantâneos.
        /// </summary>
        public static void Afastar(
            double x,
            double z,
            double[] vizinhosX,
            double[] vizinhosZ,
            out double novoX,
            out double novoZ,
            double maxDeslocamentoMetros = double.MaxValue
        )
        {
            novoX = x;
            novoZ = z;
            if (vizinhosX == null || vizinhosZ == null)
            {
                return;
            }
            int quantidade = Math.Min(vizinhosX.Length, vizinhosZ.Length);
            for (int i = 0; i < quantidade; i++)
            {
                double dx = novoX - vizinhosX[i];
                double dz = novoZ - vizinhosZ[i];
                double distancia = Math.Sqrt(dx * dx + dz * dz);
                if (distancia >= SeparacaoMinimaMetros)
                {
                    continue;
                }
                if (distancia <= 0.0)
                {
                    novoX += SeparacaoMinimaMetros;
                    continue;
                }
                double falta = SeparacaoMinimaMetros - distancia;
                novoX += dx / distancia * falta;
                novoZ += dz / distancia * falta;
            }

            if (maxDeslocamentoMetros < double.MaxValue)
            {
                double desX = novoX - x;
                double desZ = novoZ - z;
                double distTotal = Math.Sqrt(desX * desX + desZ * desZ);
                if (distTotal > maxDeslocamentoMetros && distTotal > 0.0)
                {
                    novoX = x + (desX / distTotal) * maxDeslocamentoMetros;
                    novoZ = z + (desZ / distTotal) * maxDeslocamentoMetros;
                }
            }
        }
    }
}
