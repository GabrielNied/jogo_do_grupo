using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.UI
{
    /// <summary>
    /// Nomes das classes USS do estilo base (<c>Core/UI/Resources/UI/EstiloCore.uss</c>).
    /// O Core aplica esse estilo na raiz da interface, então as telas das funcionalidades
    /// podem usar estas classes com <c>AddToClassList</c> sem carregar nada.
    /// Estilo próprio de uma funcionalidade vai num USS dela, adicionado em
    /// <c>Raiz.styleSheets</c> na tela.
    /// </summary>
    public static class EstiloCore
    {
        /// <summary>Caminho em <c>Resources</c> do USS base.</summary>
        public const string CaminhoEstilo = "UI/EstiloCore";

        /// <summary>Caminho em <c>Resources</c> do tema (TSS) do painel.</summary>
        public const string CaminhoTema = "UI/TemaPadrao";

        /// <summary>Elemento que contém as telas (o Core aplica).</summary>
        public const string RaizInterface = "raiz-interface";

        /// <summary>Raiz de cada tela: ocupa a tela inteira e centraliza o conteúdo (o Core aplica).</summary>
        public const string Tela = "tela";

        /// <summary>Como <see cref="Tela"/>, com véu escuro para um painel aberto por cima do jogo.</summary>
        public const string TelaSobreJogo = "tela-sobre-jogo";

        /// <summary>Caixa central com fundo, para formulários e listas.</summary>
        public const string Painel = "painel";

        /// <summary>Painel mais largo, para lista (use junto com <see cref="Painel"/>).</summary>
        public const string PainelLargo = "painel-largo";

        /// <summary>Texto grande de título.</summary>
        public const string Titulo = "titulo";

        /// <summary>Texto abaixo do título.</summary>
        public const string Subtitulo = "subtitulo";

        /// <summary>Texto comum, com quebra de linha.</summary>
        public const string Texto = "texto";

        /// <summary>Linha de item ganho (use junto com <see cref="Texto"/>).</summary>
        public const string TextoGanho = "texto-ganho";

        /// <summary>Linha dentro de uma <see cref="Lista"/>.</summary>
        public const string LinhaLista = "linha-lista";

        /// <summary>Campo de formulário (ex.: <c>TextField</c>), com o rótulo acima.</summary>
        public const string Campo = "campo";

        /// <summary>Linha de botões lado a lado.</summary>
        public const string LinhaBotoes = "linha-botoes";

        /// <summary>Botão padrão.</summary>
        public const string Botao = "botao";

        /// <summary>Destaque do botão da ação principal (use junto com <see cref="Botao"/>).</summary>
        public const string BotaoPrincipal = "botao-principal";

        /// <summary>Linha de mensagem para a pessoa (neutra).</summary>
        public const string Mensagem = "mensagem";

        /// <summary>Mensagem de erro (use junto com <see cref="Mensagem"/>).</summary>
        public const string MensagemErro = "mensagem-erro";

        /// <summary>Mensagem de aviso (use junto com <see cref="Mensagem"/>).</summary>
        public const string MensagemAviso = "mensagem-aviso";

        /// <summary>Fundo da barra de carregamento.</summary>
        public const string BarraCarregamento = "barra-carregamento";

        /// <summary>Parte preenchida da barra de carregamento (largura em porcentagem).</summary>
        public const string BarraCarregamentoPreenchimento = "barra-carregamento-preenchimento";

        /// <summary>Área rolável de linhas (inventário, lista longa).</summary>
        public const string Lista = "lista";

        /// <summary>Texto de ação no pé da tela, por cima do jogo (ex.: "E: abrir o baú").</summary>
        public const string Dica = "dica";

        /// <summary>
        /// Painel de uma janela do jogo aberta por atalho (use junto com <see cref="Painel"/>).
        /// </summary>
        public const string Janela = "janela";

        /// <summary>Linha do topo da janela: título e botão de fechar.</summary>
        public const string JanelaCabecalho = "janela-cabecalho";

        /// <summary>Título dentro do cabeçalho da janela (use junto com <see cref="Titulo"/>).</summary>
        public const string JanelaTitulo = "janela-titulo";

        /// <summary>Botão de fechar no canto da janela (use junto com <see cref="Botao"/>).</summary>
        public const string JanelaFechar = "janela-fechar";

        /// <summary>Área da janela que recebe o conteúdo da funcionalidade.</summary>
        public const string JanelaConteudo = "janela-conteudo";

        /// <summary>Dica de como fechar, no pé da janela (ex.: "I ou Esc: fechar").</summary>
        public const string JanelaRodape = "janela-rodape";

        /// <summary>Linha de "rótulo: valor" dentro de uma janela (ex.: "Nível  3").</summary>
        public const string LinhaDado = "linha-dado";

        /// <summary>Rótulo de uma <see cref="LinhaDado"/> (use junto com <see cref="Texto"/>).</summary>
        public const string RotuloDado = "rotulo-dado";

        /// <summary>Barra com as teclas das janelas, no canto inferior direito do jogo.</summary>
        public const string BarraAtalhos = "barra-atalhos";

        /// <summary>Um item da <see cref="BarraAtalhos"/>: a tecla e o nome da janela.</summary>
        public const string Atalho = "atalho";

        /// <summary>Destaque do <see cref="Atalho"/> da janela aberta.</summary>
        public const string AtalhoAtivo = "atalho-ativo";

        /// <summary>A tecla dentro de um <see cref="Atalho"/>.</summary>
        public const string AtalhoTecla = "atalho-tecla";

        /// <summary>O nome da janela dentro de um <see cref="Atalho"/>.</summary>
        public const string AtalhoNome = "atalho-nome";

        /// <summary>Raiz do HUD do jogo: ocupa a tela inteira e não recebe clique.</summary>
        public const string Hud = "hud";

        /// <summary>Uma região do HUD (use junto com a classe da região).</summary>
        public const string HudRegiao = "hud-regiao";

        /// <summary>Região do canto inferior esquerdo (barras do personagem).</summary>
        public const string HudInferiorEsquerda = "hud-inferior-esquerda";

        /// <summary>Região da lateral esquerda, do meio para cima (lista do grupo).</summary>
        public const string HudLateralEsquerda = "hud-lateral-esquerda";

        /// <summary>Região do topo, centro (barra do boss e do alvo).</summary>
        public const string HudTopoCentro = "hud-topo-centro";

        /// <summary>Região do centro, abaixo da barra do boss (aviso de ataque).</summary>
        public const string HudCentroAvisos = "hud-centro-avisos";

        /// <summary>Região da parte de baixo, centro (mensagens de sistema).</summary>
        public const string HudInferiorCentro = "hud-inferior-centro";

        /// <summary>Região do canto superior direito (minimapa).</summary>
        public const string HudSuperiorDireita = "hud-superior-direita";

        /// <summary>Lista das mensagens de sistema do HUD.</summary>
        public const string Avisos = "avisos";

        /// <summary>Uma linha das mensagens de sistema (use junto com a classe do tipo).</summary>
        public const string Aviso = "aviso";

        /// <summary>Mensagem de sistema do tipo informação (branco).</summary>
        public const string AvisoInformacao = "aviso-informacao";

        /// <summary>Mensagem de sistema do tipo aviso (amarelo).</summary>
        public const string AvisoAviso = "aviso-aviso";

        /// <summary>Mensagem de sistema do tipo erro (vermelho).</summary>
        public const string AvisoErro = "aviso-erro";

        /// <summary>
        /// A raiz da interface não pode capturar o clique: vazia, ela fica na frente dos
        /// painéis das funcionalidades e o botão Fechar não recebe o mouse. As telas
        /// filhas continuam capturando.
        /// </summary>
        public static void NaoBloquearCliques(VisualElement elemento)
        {
            if (elemento == null)
            {
                return;
            }

            elemento.pickingMode = PickingMode.Ignore;
        }
    }
}
