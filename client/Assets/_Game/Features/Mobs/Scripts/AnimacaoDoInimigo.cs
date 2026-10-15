using JogoDoGrupo.Shared.Features.Mobs;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// Decide, quadro a quadro, se o modelo anda e se dispara o golpe, a partir do estado do
    /// servidor e da velocidade suavizada da pose. Lógica pura: o Animator fica no apresentador.
    /// </summary>
    internal sealed class AnimacaoDoInimigo
    {
        /// <summary>Velocidade suavizada (m/s) acima da qual um inimigo parado passa a andar.</summary>
        public const float VelocidadeParaAndar = 0.5f;

        /// <summary>Velocidade suavizada (m/s) abaixo da qual quem andava para.</summary>
        public const float VelocidadeParaParar = 0.15f;

        /// <summary>Quão rápido a velocidade suavizada acompanha a medida (maior = mais rápido).</summary>
        public const float SuavizacaoDaVelocidade = 8f;

        /// <summary>
        /// Velocidade do Animator para a pose de contato cair no fim do preparo: o contato do
        /// clipe (<paramref name="contatoFracao"/> da duração, marcada por clipe em
        /// <see cref="ContatosDosClipes"/>) tem de levar <paramref name="preparoSegundos"/>. Sem
        /// clipe ou sem preparo, 1 (velocidade normal).
        /// </summary>
        public static float VelocidadeDoClipe(
            float clipeSegundos,
            float preparoSegundos,
            float contatoFracao
        )
        {
            if (clipeSegundos <= 0f || preparoSegundos <= 0f)
            {
                return 1f;
            }

            return clipeSegundos * contatoFracao / preparoSegundos;
        }

        private int _golpesVistos;

        /// <summary>Velocidade horizontal suavizada, em metros por segundo.</summary>
        public float Velocidade { get; private set; }

        /// <summary>Valor do parâmetro <c>Andando</c> do Animator.</summary>
        public bool Andando { get; private set; }

        /// <summary>Verdadeiro só no quadro em que um novo golpe começa (gatilho <c>Atacar</c>).</summary>
        public bool DisparouAtaque { get; private set; }

        /// <param name="golpesPreparados">Total de avisos de golpe recebidos; cada aumento dispara uma vez.</param>
        public void Atualizar(
            EstadoDeInimigo estado,
            float deslocamento,
            float segundos,
            int golpesPreparados
        )
        {
            DisparouAtaque = golpesPreparados > _golpesVistos;
            _golpesVistos = golpesPreparados;

            if (segundos > 0f)
            {
                var medida = deslocamento / segundos;
                var fator = 1f - Mathf.Exp(-SuavizacaoDaVelocidade * segundos);
                Velocidade = Mathf.Lerp(Velocidade, medida, fator);
            }

            switch (estado)
            {
                case EstadoDeInimigo.Alerta:
                case EstadoDeInimigo.Ataque:
                case EstadoDeInimigo.Morto:
                    // Parado de propósito (ou em golpe): ignora o resto de movimento da suavização.
                    Andando = false;
                    break;
                default:
                    // Perseguição, retorno e a caminhada da manada em repouso andam, mas não "andam no
                    // lugar" se algo os trava: só conta o movimento de verdade (histerese).
                    Andando = Velocidade > (Andando ? VelocidadeParaParar : VelocidadeParaAndar);
                    break;
            }
        }
    }
}
