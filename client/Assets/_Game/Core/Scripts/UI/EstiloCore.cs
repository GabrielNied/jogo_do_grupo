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

        /// <summary>Caixa central com fundo, para formulários e listas.</summary>
        public const string Painel = "painel";

        /// <summary>Texto grande de título.</summary>
        public const string Titulo = "titulo";

        /// <summary>Texto abaixo do título.</summary>
        public const string Subtitulo = "subtitulo";

        /// <summary>Texto comum, com quebra de linha.</summary>
        public const string Texto = "texto";

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
    }
}
