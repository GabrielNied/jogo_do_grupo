using JogoDoGrupo.Client.Core.Jogo.Janelas;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Janela do grupo (tecla O), registrada nas <see cref="JanelasDoJogo"/>: convidar por nome,
    /// responder aos convites recebidos e ver a lista do grupo. Só envia as intenções e mostra o
    /// que o servidor mandou (<see cref="EstadoDoGrupo"/>). A moldura, o atalho, o Esc e o
    /// cursor são do Core.
    /// </summary>
    internal sealed class JanelaGrupo : JanelaDoJogo
    {
        public const string Chave = "social.grupo";
        public const string Titulo = "Grupo";
        public const string Tecla = "O";
        public const int Ordem = 50;

        private PainelGrupo _painel;

        public override bool MolduraSocial => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        internal static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(Chave, Titulo, Tecla, Ordem, () => new JanelaGrupo());
        }

        protected override void Montar(VisualElement conteudo)
        {
            _painel = new PainelGrupo(conteudo, GrupoDoClient.Estado, DefinirTitulo);
        }

        protected override void AoFechar()
        {
            _painel?.Dispose();
            _painel = null;
        }
    }
}
