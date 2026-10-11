using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo.Controle
{
    /// <summary>
    /// Cálculo do movimento em 3ª pessoa, sem componente do Unity (testável no EditMode):
    /// direção a partir do input e da câmera, velocidade de andar e correr, aceleração e gravidade.
    /// O <see cref="ControleTerceiraPessoa"/> só lê o input e aplica o resultado.
    /// </summary>
    public sealed class MovimentoTerceiraPessoa
    {
        /// <summary>Abaixo deste tamanho o input conta como parado (folga do analógico).</summary>
        public const float ZonaMorta = 0.1f;

        /// <summary>Velocidade vertical mantida no chão, para o CharacterController grudar em rampas.</summary>
        public const float VelocidadeGrudarNoChao = -2f;

        public const float VelocidadeAndarPadrao = 2f;

        /// <summary>
        /// Correr padrão, em m/s. O servidor recusa deslocamento acima disso
        /// (<c>ValidadorDeMovimento.VelocidadeMaxima</c>), mesmo que o client corra mais.
        /// </summary>
        public const float VelocidadeCorrerPadrao = 5.5f;

        public const float AceleracaoPadrao = 20f;
        public const float GravidadePadrao = 20f;
        public const float VelocidadeMaximaQuedaPadrao = 50f;
        public const float AlturaPuloPadrao = 1.25f;
        public const float VelocidadeVerticalVooPadrao = 6f;

        private bool _voando;

        /// <summary>Velocidade de andar, em m/s.</summary>
        public float VelocidadeAndar { get; set; } = VelocidadeAndarPadrao;

        /// <summary>Velocidade de correr (Shift), em m/s.</summary>
        public float VelocidadeCorrer { get; set; } = VelocidadeCorrerPadrao;

        /// <summary>Quanto a velocidade muda por segundo ao acelerar ou frear, em m/s².</summary>
        public float Aceleracao { get; set; } = AceleracaoPadrao;

        /// <summary>Gravidade, em m/s² (positivo, puxa para baixo).</summary>
        public float Gravidade { get; set; } = GravidadePadrao;

        /// <summary>Velocidade máxima de queda, em m/s.</summary>
        public float VelocidadeMaximaQueda { get; set; } = VelocidadeMaximaQuedaPadrao;

        /// <summary>Altura do pulo (Espaço), em metros, com a <see cref="Gravidade"/> atual.</summary>
        public float AlturaPulo { get; set; } = AlturaPuloPadrao;

        /// <summary>Velocidade de subir e descer voando, em m/s.</summary>
        public float VelocidadeVerticalVoo { get; set; } = VelocidadeVerticalVooPadrao;

        /// <summary>
        /// Se o voo de desenvolvimento pode ser ligado: só no editor. Num build o
        /// <see cref="Voando"/> fica sempre falso.
        /// </summary>
        public static bool VooDisponivel => Application.isEditor;

        /// <summary>
        /// Voo de desenvolvimento, para olhar o mapa: sem gravidade, sem pulo, sobe e desce
        /// com <see cref="VelocidadeVerticalVoo"/>. Não é regra de jogo; fora do editor ignora
        /// o valor pedido (ver <see cref="VooDisponivel"/>).
        /// </summary>
        public bool Voando
        {
            get => _voando && VooDisponivel;
            set => _voando = value;
        }

        /// <summary>
        /// Velocidade vertical inicial do pulo, em m/s: <c>sqrt(2 * gravidade * altura)</c>,
        /// a que alcança a <see cref="AlturaPulo"/> com a <see cref="Gravidade"/>.
        /// </summary>
        public float VelocidadeDoPulo =>
            Gravidade <= 0f || AlturaPulo <= 0f ? 0f : Mathf.Sqrt(2f * Gravidade * AlturaPulo);

        /// <summary>
        /// Altura máxima, em metros, que um pulo com a <see cref="VelocidadeDoPulo"/> alcança
        /// com a <see cref="Gravidade"/> (sem teto e sem a queda máxima): <c>v² / (2 * g)</c>.
        /// </summary>
        public float AlturaEstimadaDoPulo =>
            Gravidade <= 0f ? 0f : VelocidadeDoPulo * VelocidadeDoPulo / (2f * Gravidade);

        /// <summary>
        /// Direção unitária no plano XZ a partir do input (x = lado, y = frente) e do ângulo
        /// horizontal da câmera em graus: "frente" é para onde a câmera olha.
        /// Input dentro da <see cref="ZonaMorta"/> devolve zero. O quanto andar vem de
        /// <see cref="VelocidadeAlvo"/>.
        /// </summary>
        public static Vector3 DirecaoNoMundo(Vector2 entrada, float anguloCamera)
        {
            if (Intensidade(entrada) <= 0f)
            {
                return Vector3.zero;
            }

            var local = new Vector3(entrada.x, 0f, entrada.y).normalized;
            return Quaternion.Euler(0f, anguloCamera, 0f) * local;
        }

        /// <summary>
        /// Tamanho do input entre 0 e 1 (diagonal do teclado não anda mais rápido);
        /// zero dentro da <see cref="ZonaMorta"/>.
        /// </summary>
        public static float Intensidade(Vector2 entrada)
        {
            var tamanho = Mathf.Min(entrada.magnitude, 1f);
            return tamanho < ZonaMorta ? 0f : tamanho;
        }

        /// <summary>
        /// Velocidade horizontal desejada, em m/s: andar ou correr vezes a intensidade do input.
        /// Correr sem input continua parado.
        /// </summary>
        public float VelocidadeAlvo(Vector2 entrada, bool correndo)
        {
            var maxima = correndo ? VelocidadeCorrer : VelocidadeAndar;
            return maxima * Intensidade(entrada);
        }

        /// <summary>Aproxima a velocidade atual da alvo, limitada pela <see cref="Aceleracao"/>.</summary>
        public float Acelerar(float atual, float alvo, float deltaTempo)
        {
            return Mathf.MoveTowards(atual, alvo, Aceleracao * Mathf.Max(0f, deltaTempo));
        }

        /// <summary>
        /// Nova velocidade vertical: no chão (sem subir) gruda com
        /// <see cref="VelocidadeGrudarNoChao"/>; no ar soma a gravidade até a queda máxima.
        /// Com o voo ligado (<see cref="Voando"/>), ignora a gravidade: <paramref name="subida"/> (-1 desce, 1 sobe)
        /// vezes a <see cref="VelocidadeVerticalVoo"/>.
        /// </summary>
        public float VelocidadeVertical(
            float atual,
            bool noChao,
            float deltaTempo,
            float subida = 0f
        )
        {
            if (Voando)
            {
                return Mathf.Clamp(subida, -1f, 1f) * VelocidadeVerticalVoo;
            }

            if (noChao && atual <= 0f)
            {
                return VelocidadeGrudarNoChao;
            }

            var nova = atual - Gravidade * Mathf.Max(0f, deltaTempo);
            return Mathf.Max(nova, -VelocidadeMaximaQueda);
        }

        /// <summary>
        /// Aplica o pulo: se foi pedido e o jogador está no chão sem estar subindo, a velocidade
        /// vertical vira a <see cref="VelocidadeDoPulo"/>. No ar (ou já subindo) devolve a
        /// velocidade atual sem somar nada: não há segundo pulo. Com o voo ligado não pula.
        /// </summary>
        public float Pular(float atual, bool noChao, bool pedido)
        {
            if (Voando || !pedido || !noChao || atual > 0f)
            {
                return atual;
            }

            return VelocidadeDoPulo;
        }
    }
}
