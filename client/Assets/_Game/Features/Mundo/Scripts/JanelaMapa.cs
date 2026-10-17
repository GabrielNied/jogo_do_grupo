using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Features.Mundo.Presenca;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo
{
    /// <summary>
    /// Mapa (tecla M) nas <see cref="JanelasDoJogo"/>. O conteúdo é a <see cref="JanelaDoMapa"/>.
    /// </summary>
    internal static class JanelaMapa
    {
        public const string Chave = "mundo.mapa";
        public const string Titulo = "Mapa";
        public const string Tecla = "M";
        public const int Ordem = 40;

        /// <summary>
        /// ~~Texto da janela em construção.~~ Substituído em 2026-10-11 pela
        /// <see cref="JanelaDoMapa"/>.
        /// </summary>
        public const string Texto = "O mapa do mundo, com a sua posição, vai aparecer aqui.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        internal static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(Chave, Titulo, Tecla, Ordem, CriarConteudo);
        }

        private static JanelaDoJogo CriarConteudo()
        {
            var area = PresencaNoMundo.Atual?.AreaAtual ?? RotulosDeArea.IdCidade;
            if (area.StartsWith(RotulosDeArea.PrefixoInstancia, System.StringComparison.Ordinal))
            {
                return new JanelaDoMapaDaDungeon();
            }

            return new JanelaDoMapa();
        }
    }
}
