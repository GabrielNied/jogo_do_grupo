using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Classes do USS próprio de Social (<c>UI/Resources/Social/EstiloSocial.uss</c>).
    /// O resto do visual usa as classes do <c>EstiloCore</c>.
    /// </summary>
    internal static class EstiloSocial
    {
        /// <summary>Caminho em <c>Resources</c> do USS de Social.</summary>
        public const string Caminho = "Social/EstiloSocial";

        /// <summary>Linha com o campo do nome e o botão Convidar.</summary>
        public const string Convidar = "social-convidar";

        /// <summary>Bloco da janela (convites, membros) com espaço embaixo.</summary>
        public const string Secao = "social-secao";

        /// <summary>Linha de um membro ou convite: nome à esquerda, botões à direita.</summary>
        public const string Linha = "social-linha";

        /// <summary>Nome na linha (ocupa o espaço que sobra).</summary>
        public const string Nome = "social-nome";

        /// <summary>Membro que lidera o grupo (use junto com <see cref="Nome"/>).</summary>
        public const string Lider = "social-lider";

        /// <summary>Membro offline (use junto com <see cref="Nome"/>).</summary>
        public const string Offline = "social-offline";

        /// <summary>Os botões de uma linha.</summary>
        public const string Acoes = "social-acoes";

        /// <summary>Botão pequeno dentro de uma linha (use junto com <c>EstiloCore.Botao</c>).</summary>
        public const string Botao = "social-botao";

        /// <summary>Espaço acima do botão Sair do grupo.</summary>
        public const string Rodape = "social-rodape";

        /// <summary>Aviso por cima do jogo (use junto com <c>EstiloCore.Dica</c>).</summary>
        public const string AvisoNaTela = "social-aviso-tela";

        /// <summary>Lista de membros no HUD da lateral esquerda.</summary>
        public const string ListaGrupoHud = "lista-grupo-hud";

        /// <summary>Linha de um aliado no HUD.</summary>
        public const string LinhaAliadoHud = "linha-aliado-hud";

        /// <summary>Aliado offline no HUD.</summary>
        public const string AliadoOffline = "aliado-offline";

        /// <summary>Vida crítica (&lt; 30%) com destaque visual.</summary>
        public const string VidaCritica = "vida-critica";

        /// <summary>Adiciona o USS de Social à raiz da interface.</summary>
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
                    $"[Social] Estilo nao encontrado em Resources/{Caminho}.uss; usando só o do Core."
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
