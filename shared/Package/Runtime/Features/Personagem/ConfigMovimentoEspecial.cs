namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Valores de calibração de natação, queda e escalada (#349). Client e servidor usam os
    /// mesmos números; quem decide é o servidor (docs/features/personagem.md).
    /// </summary>
    public static class ConfigMovimentoEspecial
    {
        /// <summary>Profundidade mínima da água (m) para o personagem nadar em vez de andar.</summary>
        public const float ProfundidadeMinimaParaNadar = 1.3f;

        /// <summary>Profundidade (m) a partir da qual cair na água não machuca.</summary>
        public const float ProfundidadeQueAmortece = 2f;

        /// <summary>Quanto os pés ficam abaixo da superfície (m) ao boiar.</summary>
        public const float AfundamentoAoBoiar = 0.9f;

        /// <summary>Os pés precisam estar pelo menos isto (m) abaixo da superfície para contar como nadando.</summary>
        public const float FolgaDaSuperficie = 0.2f;

        /// <summary>Velocidade horizontal a nado (m/s).</summary>
        public const float VelocidadeNado = 3f;

        /// <summary>Stamina gasta por segundo nadando.</summary>
        public const int StaminaDoNadoPorSegundo = 4;

        /// <summary>Stamina gasta por segundo escalando.</summary>
        public const int StaminaDaEscaladaPorSegundo = 6;

        /// <summary>Fração da velocidade (0 a 1) de quem nada sem stamina.</summary>
        public const double FatorSemStamina = 0.5;

        /// <summary>Intervalo (ms) entre cobranças de stamina de natação e escalada.</summary>
        public const long IntervaloDeCobrancaMs = 1000;

        /// <summary>Queda até esta altura (m) não machuca. O pulo chega a 1,25 m.</summary>
        public const float AlturaSegura = 4f;

        /// <summary>Dano por metro de queda acima da <see cref="AlturaSegura"/>.</summary>
        public const int DanoPorMetroDeQueda = 40;

        /// <summary>Teto do dano de uma queda (60% da vida base de 500).</summary>
        public const int DanoMaximoDeQueda = 300;

        /// <summary>Velocidade de subir e descer na via de escalada (m/s).</summary>
        public const float VelocidadeDeEscalada = 2.5f;

        /// <summary>Velocidade lateral na via de escalada (m/s).</summary>
        public const float VelocidadeLateralDeEscalada = 1.5f;

        /// <summary>Folga (m) além do volume da via em que ainda vale como "na via".</summary>
        public const float MargemDaVia = 0.5f;
    }
}
