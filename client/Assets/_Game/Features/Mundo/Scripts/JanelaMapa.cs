using JogoDoGrupo.Client.Core.Jogo.Janelas;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo
{
    /// <summary>
    /// Mapa (tecla M) nas <see cref="JanelasDoJogo"/>. O mapa-múndi ainda é uma decisão do
    /// grupo (issue #92): por enquanto a janela reserva o atalho e avisa que está em construção.
    /// Troque a fábrica por uma <see cref="JanelaDoJogo"/> própria quando o mapa existir.
    /// </summary>
    internal static class JanelaMapa
    {
        public const string Chave = "mundo.mapa";
        public const string Titulo = "Mapa";
        public const string Tecla = "M";
        public const int Ordem = 40;

        public const string Texto = "O mapa do mundo, com a sua posição, vai aparecer aqui.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        internal static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(
                Chave,
                Titulo,
                Tecla,
                Ordem,
                () => new JanelaEmConstrucao(Texto)
            );
        }
    }
}
