using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Missoes
{
    /// <summary>
    /// Registro de missões (tecla L) nas <see cref="JanelasDoJogo"/>. As missões ainda não
    /// existem no jogo: por enquanto a janela reserva o atalho e avisa que está em construção.
    /// Troque a fábrica por uma <see cref="JanelaDoJogo"/> própria quando houver missões.
    /// </summary>
    internal static class JanelaMissoes
    {
        public const string Chave = "missoes.registro";
        public const string Titulo = "Missões";
        public const string Tecla = "L";
        public const int Ordem = 30;

        public const string Texto = TextosDoLivro.DiarioMissoes;
        public const string TextoEmConstrucao =
            "As missões aceitas e o progresso de cada uma vão aparecer aqui.";

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
