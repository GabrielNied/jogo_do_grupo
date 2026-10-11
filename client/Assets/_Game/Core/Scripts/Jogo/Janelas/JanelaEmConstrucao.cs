using JogoDoGrupo.Client.Core.UI;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Jogo.Janelas
{
    /// <summary>
    /// Janela de uma funcionalidade que ainda não tem conteúdo: reserva o atalho e avisa a
    /// pessoa do que vai aparecer ali. Troque por uma <see cref="JanelaDoJogo"/> própria
    /// quando a funcionalidade existir.
    /// </summary>
    public sealed class JanelaEmConstrucao : JanelaDoJogo
    {
        /// <summary>Aviso mostrado acima do texto da funcionalidade.</summary>
        public const string Aviso = "Em construção";

        private readonly string _texto;

        /// <param name="texto">O que essa janela vai mostrar quando estiver pronta.</param>
        public JanelaEmConstrucao(string texto)
        {
            _texto = texto;
        }

        protected override void Montar(VisualElement conteudo)
        {
            var aviso = new Label(Aviso);
            aviso.AddToClassList(EstiloCore.Mensagem);
            aviso.AddToClassList(EstiloCore.MensagemAviso);
            conteudo.Add(aviso);

            if (!string.IsNullOrEmpty(_texto))
            {
                var texto = new Label(_texto);
                texto.AddToClassList(EstiloCore.Texto);
                conteudo.Add(texto);
            }
        }
    }
}
