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

        /// <summary>Aliado em outra área (longe) no HUD (#351).</summary>
        public const string AliadoLonge = "aliado-longe";

        /// <summary>Vida crítica (&lt; 30%) com destaque visual.</summary>
        public const string VidaCritica = "vida-critica";

        /// <summary>Camada overlay de marcadores táticos.</summary>
        public const string MarcadorCamada = "social-marcador-camada";

        /// <summary>Elemento visual do marcador tático de alvo.</summary>
        public const string MarcadorTatico = "social-marcador-tatico";

        /// <summary>Estilo de foco primário (Caveira).</summary>
        public const string MarcadorCaveira = "marcador-caveira";

        /// <summary>Estilo de foco secundário (Cruz).</summary>
        public const string MarcadorCruz = "marcador-cruz";

        /// <summary>Estilo de foco do tank (Escudo).</summary>
        public const string MarcadorEscudo = "marcador-escudo";

        /// <summary>Estilo de controle de grupo (Lua).</summary>
        public const string MarcadorLua = "marcador-lua";

        /// <summary>Estilo de marcador especial (Estrela).</summary>
        public const string MarcadorEstrela = "marcador-estrela";
        /// <summary>Camada invisível de tela cheia que abriga todos os balões de fala.</summary>
        public const string BalaoCamada = "social-balao-camada";

        /// <summary>Contêiner visual de um balão de fala individual.</summary>
        public const string BalaoFala = "social-balao-fala";

        /// <summary>Rótulo com o nome do autor da mensagem no balão.</summary>
        public const string BalaoAutor = "social-balao-autor";

        /// <summary>Rótulo com o texto da mensagem no balão.</summary>
        public const string BalaoTexto = "social-balao-texto";

        /// <summary>Ponta triangular voltada para a cabeça do avatar.</summary>
        public const string BalaoPonta = "social-balao-ponta";

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
