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

        /// <summary>Linha da criação: lista de raças à esquerda, cartão à direita.</summary>
        public const string EscolhaDeRaca = "personagem-escolha-raca";

        /// <summary>Coluna dos botões de raça.</summary>
        public const string ListaDeRacas = "personagem-lista-racas";

        /// <summary>Botão de uma raça (use junto com <see cref="Item"/>).</summary>
        public const string RacaBotao = "personagem-raca-botao";

        /// <summary>Cartão com a descrição da raça escolhida.</summary>
        public const string CartaoDaRaca = "personagem-cartao-raca";

        /// <summary>Linha de texto do cartão da raça.</summary>
        public const string CartaoTexto = "personagem-cartao-texto";

        /// <summary>Frase de seleção da raça, em destaque.</summary>
        public const string CartaoCitacao = "personagem-cartao-citacao";

        public const string HudBarraXp = "personagem-hud-barra-xp";
        public const string HudBarraXpRotulo = "personagem-hud-barra-xp-rotulo";
        public const string HudBarraXpTrilho = "personagem-hud-barra-xp-trilho";
        public const string HudBarraXpPreenchimento = "personagem-hud-barra-xp-preenchimento";

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
