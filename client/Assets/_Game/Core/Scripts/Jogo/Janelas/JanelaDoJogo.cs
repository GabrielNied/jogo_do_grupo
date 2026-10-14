using System;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Jogo.Janelas
{
    /// <summary>
    /// Conteúdo de uma janela aberta por cima do jogo (personagem, inventário, missões, mapa),
    /// montado em C# com UI Toolkit. O Core cuida da moldura, do título, do botão de fechar,
    /// do atalho, do Esc e do cursor; a funcionalidade só monta o que vai dentro.
    /// Como a <see cref="JogoDoGrupo.Client.Core.UI.Tela"/>, uma instância nova é criada a
    /// cada abertura e descartada ao fechar.
    /// </summary>
    /// <example>
    /// <code>
    /// internal sealed class JanelaExemplo : JanelaDoJogo
    /// {
    ///     protected override void Montar(VisualElement conteudo)
    ///     {
    ///         var texto = new Label("Olá");
    ///         texto.AddToClassList(EstiloCore.Texto);
    ///         conteudo.Add(texto);
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class JanelaDoJogo
    {
        private Action<string> _definirTitulo;

        /// <summary>
        /// Área da janela que recebe o conteúdo, abaixo do título. Disponível a partir de
        /// <see cref="Montar"/>.
        /// </summary>
        public VisualElement Conteudo { get; private set; }

        /// <summary>
        /// Se a janela ainda está aberta. Confira depois de cada <c>await</c> antes de mexer na
        /// interface: a janela pode ter sido fechada enquanto esperava o servidor.
        /// </summary>
        public bool Aberta { get; private set; }

        /// <summary>Solicita folhas e calçada na moldura, com espaço reservado à decoração.</summary>
        public virtual bool MolduraSocial => false;

        /// <summary>Ocupa a tela e usa apenas os controles da própria janela sobre o conteúdo.</summary>
        public virtual bool TelaInteira => false;

        /// <summary>Cria os elementos dentro de <paramref name="conteudo"/>. Chamado uma vez.</summary>
        protected abstract void Montar(VisualElement conteudo);

        /// <summary>Chamado logo depois de <see cref="Montar"/> (ex.: pedir dados ao servidor).</summary>
        protected virtual void AoAbrir() { }

        /// <summary>Chamado quando a janela fecha, antes de sair da tela.</summary>
        protected virtual void AoFechar() { }

        /// <summary>
        /// Troca o título da moldura (ex.: "Inventário (3/20)"). Sem efeito com a janela fechada.
        /// </summary>
        protected void DefinirTitulo(string titulo)
        {
            if (Aberta)
            {
                _definirTitulo?.Invoke(titulo);
            }
        }

        internal void Abrir(VisualElement conteudo, Action<string> definirTitulo)
        {
            Conteudo = conteudo;
            _definirTitulo = definirTitulo;
            Aberta = true;
            Montar(conteudo);

            // Montar pode ter fechado a janela; nesse caso não chama AoAbrir.
            if (Aberta)
            {
                AoAbrir();
            }
        }

        internal void Fechar()
        {
            if (!Aberta)
            {
                return;
            }

            Aberta = false;
            _definirTitulo = null;
            AoFechar();
        }
    }
}
