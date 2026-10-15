namespace JogoDoGrupo.Client.Features.Desenvolvimento.Relato
{
    /// <summary>Valores ajustáveis do report (F9 ou J, regra 17).</summary>
    internal static class ConfigRelato
    {
        /// <summary>Quantos segundos antes do report têm imagem (folhas e <c>quadros/</c>).</summary>
        public const float SegundosGuardados = 3f;

        /// <summary>
        /// Quantos segundos antes do report a linha do tempo cobre (texto é barato; a causa de
        /// um bug costuma vir antes dos últimos segundos com imagem).
        /// </summary>
        public const float SegundosDaLinhaDoTempo = 15f;

        /// <summary>Avisos e erros do console guardados para o <c>console.txt</c>.</summary>
        public const int LinhasDoConsole = 40;

        /// <summary>Linhas da pilha de cada erro do console que entram no report.</summary>
        public const int LinhasDaPilha = 4;

        /// <summary>Tempo máximo, em segundos, do envio do report como issue.</summary>
        public const int SegundosDoEnvio = 120;

        /// <summary>Teto de quadros gravados por segundo (relógio real, ignora o hit-stop).</summary>
        public const int QuadrosPorSegundo = 60;

        /// <summary>Tamanho de cada quadro guardado, em pixels.</summary>
        public const int LarguraDoQuadro = 320;

        public const int AlturaDoQuadro = 180;

        /// <summary>
        /// Recorte ampliado da folha de impacto: fração da tela em largura e altura e o centro,
        /// em fração da tela (o jogador fica um pouco abaixo do meio na câmera de 3ª pessoa).
        /// </summary>
        public const float FracaoDoZoom = 0.5f;

        public const float CentroXDoZoom = 0.5f;

        public const float CentroYDoZoomDoTopo = 0.55f;

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
