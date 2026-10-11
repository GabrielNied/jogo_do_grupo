using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Classes do USS próprio de Personagem (<c>UI/Resources/Personagem/EstiloPersonagem.uss</c>).
    /// O resto do visual usa as classes do <c>EstiloCore</c>.
    /// </summary>
    internal static class EstiloPersonagem
    {
        /// <summary>Caminho em <c>Resources</c> do USS de Personagem.</summary>
        public const string Caminho = "Personagem/EstiloPersonagem";

        /// <summary>Painel um pouco mais largo para a lista.</summary>
        public const string PainelSelecao = "personagem-painel-selecao";

        /// <summary>Lista de personagens.</summary>
        public const string Lista = "personagem-lista";

        /// <summary>Um personagem da lista (botão).</summary>
        public const string Item = "personagem-item";

        /// <summary>Personagem selecionado (use junto com <see cref="Item"/>).</summary>
        public const string ItemSelecionado = "personagem-item-selecionado";

        /// <summary>Nome no item da lista.</summary>
        public const string ItemNome = "personagem-item-nome";

        /// <summary>Raça e nível no item da lista.</summary>
        public const string ItemDetalhes = "personagem-item-detalhes";

        /// <summary>Grupo de opções de raça na criação.</summary>
        public const string Racas = "personagem-racas";

        /// <summary>Adiciona o USS de Personagem à raiz da tela.</summary>
        public static void Aplicar(VisualElement raiz)
        {
            var estilo = Resources.Load<StyleSheet>(Caminho);
            if (estilo != null)
            {
                raiz.styleSheets.Add(estilo);
            }
            else
            {
                Debug.LogWarning(
                    $"[Personagem] Estilo não encontrado em Resources/{Caminho}.uss; usando só o do Core."
                );
            }
        }

        /// <summary>Mostra ou esconde o elemento (escondido, não ocupa espaço).</summary>
        public static void Mostrar(VisualElement elemento, bool visivel)
        {
            elemento.style.display = visivel ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
