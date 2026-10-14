using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// Classes do USS próprio do inventário (<c>UI/Resources/Itens/EstiloItens.uss</c>). O resto do
    /// visual usa as classes do <c>EstiloCore</c> e os tokens Lumora.
    /// </summary>
    public static class EstiloItens
    {
        public const string Caminho = "Itens/EstiloItens";

        public const string PainelLargo = "itens-painel-largo";
        public const string Corpo = "itens-corpo";
        public const string Coluna = "itens-coluna";
        public const string CabecalhoSecao = "itens-cabecalho-secao";
        public const string TituloSecao = "itens-titulo-secao";
        public const string Contador = "itens-contador";
        public const string ContadorCheio = "itens-contador-cheio";
        public const string Slot = "item-slot";
        public const string SlotVazio = "item-slot-vazio";
        public const string SlotValido = "item-slot-valido";
        public const string SlotInvalido = "item-slot-invalido";
        public const string SlotAlvo = "item-slot-alvo";
        public const string SlotPendente = "item-slot-pendente";
        public const string SlotDoador = "item-slot-doador";
        public const string SlotFavorito = "item-slot-favorito";
        public const string SlotLixo = "item-slot-lixo";
        public const string SlotSelecionado = "item-slot-selecionado";
        public const string Icone = "item-icone";
        public const string Sigla = "item-sigla";
        public const string SeloNovo = "item-selo-novo";
        public const string SeloFavorito = "item-selo-favorito";
        public const string SeloLixo = "item-selo-lixo";
        public const string Abas = "itens-abas";
        public const string Aba = "itens-aba";
        public const string AbaAtiva = "itens-aba-ativa";
        public const string Grade = "itens-grade";
        public const string Paperdoll = "itens-paperdoll";
        public const string PaperdollLinha = "itens-paperdoll-linha";
        public const string PaperdollVazio = "itens-paperdoll-vazio";
        public const string Conjuntos = "itens-conjuntos";
        public const string Conjunto = "itens-conjunto";
        public const string ConjuntoRotulo = "itens-conjunto-rotulo";
        public const string Resumo = "itens-resumo";
        public const string ResumoTitulo = "itens-resumo-titulo";
        public const string ResumoLinha = "itens-resumo-linha";
        public const string ResumoBonus = "itens-resumo-bonus";
        public const string Confirmacao = "itens-confirmacao";
        public const string RodapeBotoes = "itens-rodape-botoes";
        public const string Fantasma = "item-fantasma";
        public const string Tooltip = "item-tooltip";
        public const string TooltipNome = "item-tooltip-nome";
        public const string TooltipLinha = "item-tooltip-linha";
        public const string TooltipComparacao = "item-tooltip-comparacao";

        /// <summary>Letra do tier no canto da casa.</summary>
        public const string LetraTier = "item-tier-letra";

        /// <summary>Classe da casa por tier: cor da borda, do ícone e da letra.</summary>
        public static string Tier(TierItem tier) => "item-tier-" + (int)tier;

        /// <summary>Classe do nome do item no tooltip, na cor do tier.</summary>
        public static string NomeTier(TierItem tier) => "item-nome-tier-" + (int)tier;

        public static string DoEstilo(EstiloLinha estilo)
        {
            switch (estilo)
            {
                case EstiloLinha.Fixo:
                    return "item-tooltip-fixo";
                case EstiloLinha.Sorteado:
                    return "item-tooltip-sorteado";
                case EstiloLinha.Ganho:
                    return "item-tooltip-ganho";
                case EstiloLinha.Perda:
                    return "item-tooltip-perda";
                case EstiloLinha.Suave:
                    return "item-tooltip-suave";
                default:
                    return null;
            }
        }

        /// <summary>Adiciona o USS do inventário à raiz da interface (uma vez).</summary>
        public static void Aplicar(VisualElement raiz)
        {
            var estilo = Resources.Load<StyleSheet>(Caminho);
            if (estilo == null)
            {
                Debug.LogWarning($"[Itens] Estilo não encontrado em Resources/{Caminho}.uss.");
                return;
            }

            if (!raiz.styleSheets.Contains(estilo))
                raiz.styleSheets.Add(estilo);
        }
    }
}
