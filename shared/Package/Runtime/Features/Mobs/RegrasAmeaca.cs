using System;

namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Regras e constantes puras do sistema de ameaça (agro) dos inimigos (docs/features/combate.md#ameaça-agro-169).
    /// O servidor decide o alvo com base na tabela de ameaça; o cliente vê apenas o AlvoId.
    /// </summary>
    public static class RegrasAmeaca
    {
        // ── Constantes de balanceamento ──────────────────────────────────────

        /// <summary>Ameaça gerada por ponto de dano real causado ao inimigo.</summary>
        public const double AmeacaPorDano = 1.0;

        /// <summary>Ameaça gerada por ponto de cura real, dividida entre os inimigos em combate com o curado.</summary>
        public const double AmeacaPorCura = 0.5;

        /// <summary>Multiplicador de ameaça para quem tem item de tank equipado (ex.: escudo).</summary>
        public const double MultiplicadorDeAmeacaTank = 3.0;

        /// <summary>Margem para troca de alvo se o desafiante estiver no alcance de ataque do inimigo (110%).</summary>
        public const double MargemDeTrocaPerto = 1.10;

        /// <summary>Margem para troca de alvo se o desafiante estiver fora do alcance de ataque do inimigo (130%).</summary>
        public const double MargemDeTrocaLonge = 1.30;

        /// <summary>Duração da fixação forçada de alvo ao provocar, em segundos.</summary>
        public const double DuracaoDaProvocacaoSegundos = 4.0;

        /// <summary>Duração da fixação forçada de alvo ao provocar, em milissegundos.</summary>
        public const long DuracaoDaProvocacaoMs = 4000;

        // ── Cálculos puros ───────────────────────────────────────────────────

        /// <summary>Calcula a ameaça gerada por dano real.</summary>
        public static double CalcularAmeacaDeDano(int danoReal, bool eTank = false)
        {
            if (danoReal <= 0)
            {
                return 0.0;
            }

            double baseAmeaca = danoReal * AmeacaPorDano;
            return eTank ? baseAmeaca * MultiplicadorDeAmeacaTank : baseAmeaca;
        }

        /// <summary>Calcula a ameaça gerada por cura real, dividida entre os inimigos em combate.</summary>
        public static double CalcularAmeacaDeCura(
            int curaReal,
            int inimigosEmCombate = 1,
            bool eTank = false
        )
        {
            if (curaReal <= 0)
            {
                return 0.0;
            }

            int divisao = Math.Max(1, inimigosEmCombate);
            double baseAmeaca = (curaReal * AmeacaPorCura) / divisao;
            return eTank ? baseAmeaca * MultiplicadorDeAmeacaTank : baseAmeaca;
        }

        /// <summary>
        /// Se a nova ameaça supera o limiar exigido para roubar o agro do alvo atual.
        /// </summary>
        public static bool SuperaMargem(double ameacaAtual, double novaAmeaca, bool estaNoAlcance)
        {
            double multiplicador = estaNoAlcance ? MargemDeTrocaPerto : MargemDeTrocaLonge;
            return novaAmeaca > ameacaAtual * multiplicador;
        }

        /// <summary>
        /// Escolhe o alvo de perseguição/ataque considerando ameaça, margem de troca e provocação.
        /// Se nenhum candidato tiver ameaça gerada, preserva a regra clássica de proximidade.
        /// </summary>
        public static long EscolherAlvo(
            long alvoAtual,
            long[] candidatos,
            double[] distanciasMetros,
            double[] ameacas,
            double alcanceDeAtaqueMetros,
            long provocadorId = 0,
            long agoraMs = 0,
            long provocacaoAteMs = 0
        )
        {
            if (candidatos == null || distanciasMetros == null || ameacas == null)
            {
                return RegrasInimigo.SemAlvo;
            }

            int quantidade = Math.Min(
                candidatos.Length,
                Math.Min(distanciasMetros.Length, ameacas.Length)
            );
            if (quantidade == 0)
            {
                return RegrasInimigo.SemAlvo;
            }

            // 1. Provocação ativa e válida
            if (provocadorId != RegrasInimigo.SemAlvo && agoraMs < provocacaoAteMs)
            {
                for (int i = 0; i < quantidade; i++)
                {
                    if (candidatos[i] == provocadorId)
                    {
                        return provocadorId;
                    }
                }
            }

            // 2. Verifica se alguém gerou ameaça positiva
            bool temAmeaca = false;
            for (int i = 0; i < quantidade; i++)
            {
                if (ameacas[i] > 0.0)
                {
                    temAmeaca = true;
                    break;
                }
            }

            if (!temAmeaca)
            {
                return RegrasInimigo.EscolherAlvo(alvoAtual, candidatos, distanciasMetros);
            }

            // 3. Localiza alvo atual entre os candidatos
            int indiceAlvoAtual = -1;
            for (int i = 0; i < quantidade; i++)
            {
                if (candidatos[i] == alvoAtual && alvoAtual != RegrasInimigo.SemAlvo)
                {
                    indiceAlvoAtual = i;
                    break;
                }
            }

            // 4. Se o alvo atual é válido e tem ameaça > 0, precisa superar a margem (110% perto, 130% longe)
            if (indiceAlvoAtual >= 0 && ameacas[indiceAlvoAtual] > 0.0)
            {
                double ameacaAtual = ameacas[indiceAlvoAtual];
                long melhorDesafiante = RegrasInimigo.SemAlvo;
                double maiorAmeacaDesafiante = -1.0;

                for (int i = 0; i < quantidade; i++)
                {
                    long candidato = candidatos[i];
                    if (candidato == RegrasInimigo.SemAlvo || candidato == alvoAtual)
                    {
                        continue;
                    }

                    double ameacaCandidato = ameacas[i];
                    double distancia = distanciasMetros[i];
                    bool perto = distancia <= alcanceDeAtaqueMetros;

                    if (SuperaMargem(ameacaAtual, ameacaCandidato, perto))
                    {
                        if (
                            ameacaCandidato > maiorAmeacaDesafiante
                            || (
                                ameacaCandidato == maiorAmeacaDesafiante
                                && (
                                    melhorDesafiante == RegrasInimigo.SemAlvo
                                    || candidato < melhorDesafiante
                                )
                            )
                        )
                        {
                            melhorDesafiante = candidato;
                            maiorAmeacaDesafiante = ameacaCandidato;
                        }
                    }
                }

                if (melhorDesafiante != RegrasInimigo.SemAlvo)
                {
                    return melhorDesafiante;
                }

                return alvoAtual;
            }

            // 5. Sem alvo atual válido com ameaça: o maior valor de ameaça vence (empate: menor id)
            long escolhido = RegrasInimigo.SemAlvo;
            double maiorAmeaca = -1.0;

            for (int i = 0; i < quantidade; i++)
            {
                long candidato = candidatos[i];
                if (candidato == RegrasInimigo.SemAlvo)
                {
                    continue;
                }

                double ameaca = ameacas[i];
                if (
                    escolhido == RegrasInimigo.SemAlvo
                    || ameaca > maiorAmeaca
                    || (ameaca == maiorAmeaca && candidato < escolhido)
                )
                {
                    escolhido = candidato;
                    maiorAmeaca = ameaca;
                }
            }

            return escolhido;
        }
    }
}
