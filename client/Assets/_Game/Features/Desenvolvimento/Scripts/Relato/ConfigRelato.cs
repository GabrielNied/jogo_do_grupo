namespace JogoDoGrupo.Client.Features.Desenvolvimento.Relato
{
    /// <summary>Valores ajustáveis do relato F9 (regra 17).</summary>
    internal static class ConfigRelato
    {
        /// <summary>Quantos segundos antes do F9 o relato guarda.</summary>
        public const float SegundosGuardados = 3f;

        /// <summary>Teto de quadros gravados por segundo (relógio real, ignora o hit-stop).</summary>
        public const int QuadrosPorSegundo = 60;

        /// <summary>Tamanho de cada quadro guardado, em pixels.</summary>
        public const int LarguraDoQuadro = 320;

        public const int AlturaDoQuadro = 180;

        /// <summary>Quadros em cada folha (grade de colunas × linhas).</summary>
        public const int ColunasDaFolha = 4;

        public const int LinhasDaFolha = 3;

        public const int QuadrosPorFolha = ColunasDaFolha * LinhasDaFolha;

        /// <summary>
        /// Na folha de impacto, quantos quadros vêm antes do acerto: o resto mostra a reação.
        /// </summary>
        public const int QuadrosAntesDoAcerto = 3;

        /// <summary>Distância máxima, em metros, do Animator vigiado além do jogador.</summary>
        public const float RaioDoAlvo = 10f;

        /// <summary>De quanto em quanto tempo, em segundos, procura o Animator mais perto.</summary>
        public const float IntervaloDaBuscaDoAlvo = 0.25f;

        /// <summary>
        /// Solavanco da câmera, em metros por quadro², acima do qual conta como tremor (o
        /// movimento suave da câmera fica bem abaixo disso).
        /// </summary>
        public const float LimiarDoTremor = 0.008f;

        /// <summary>Mudança de <c>Animator.speed</c> abaixo disso não entra na linha do tempo.</summary>
        public const float LimiarDaVelocidade = 0.01f;

        /// <summary>
        /// Mensagem que chega mais vezes que isso na janela (posição, por exemplo) vira só um
        /// total no fim da linha do tempo.
        /// </summary>
        public const int MensagensFrequentes = 12;

        /// <summary>Tamanho máximo do JSON de cada mensagem na linha do tempo.</summary>
        public const int TamanhoDoJson = 160;

        /// <summary>Escala dos números desenhados em cada quadro das folhas.</summary>
        public const int EscalaDoNumero = 3;
    }
}
