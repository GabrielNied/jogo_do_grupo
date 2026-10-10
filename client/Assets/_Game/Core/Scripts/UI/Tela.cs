using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.UI
{
    /// <summary>
    /// Uma tela do client, montada em C# com UI Toolkit.
    /// A <see cref="Navegacao"/> cria uma instância nova cada vez que a tela é aberta
    /// (pela fábrica passada em <see cref="Navegacao.Registrar"/>) e a descarta ao fechar,
    /// então o estado da tela começa limpo a cada abertura.
    /// </summary>
    /// <example>
    /// <code>
    /// public class TelaExemplo : Tela
    /// {
    ///     protected override void Montar(VisualElement raiz)
    ///     {
    ///         var painel = new VisualElement();
    ///         painel.AddToClassList(EstiloCore.Painel);
    ///         painel.Add(new Label("Olá"));
    ///         raiz.Add(painel);
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class Tela
    {
        /// <summary>
        /// Elemento raiz da tela, já dentro do painel e com a classe <see cref="EstiloCore.Tela"/>.
        /// Fica disponível a partir de <see cref="Montar"/>.
        /// </summary>
        public VisualElement Raiz { get; private set; }

        /// <summary>
        /// Se a tela ainda está aberta. Confira depois de cada <c>await</c> antes de mexer na
        /// interface ou navegar: a tela pode ter sido trocada enquanto esperava (ex.: sessão
        /// expirada levou para o login).
        /// </summary>
        public bool Aberta { get; private set; }

        /// <summary>
        /// Cria os elementos da tela dentro de <paramref name="raiz"/>. Chamado uma vez, ao abrir.
        /// </summary>
        protected abstract void Montar(VisualElement raiz);

        /// <summary>
        /// Chamado logo depois de <see cref="Montar"/> (ex.: pedir dados ao servidor, dar foco
        /// num campo). Pode navegar para outra tela.
        /// </summary>
        protected virtual void AoAbrir() { }

        /// <summary>Chamado quando a tela sai de cena, antes de ser removida do painel.</summary>
        protected virtual void AoFechar() { }

        internal void Abrir(VisualElement raiz)
        {
            Raiz = raiz;
            Aberta = true;
            Montar(raiz);

            // Montar pode ter navegado para outra tela; nesse caso não chama AoAbrir.
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
            AoFechar();
        }
    }
}
