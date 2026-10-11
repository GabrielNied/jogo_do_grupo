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
    }
}
