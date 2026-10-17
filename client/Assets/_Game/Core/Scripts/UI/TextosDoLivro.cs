namespace JogoDoGrupo.Client.Core.UI
{
    /// <summary>
    /// Textos canônicos de interface aprovados a partir do livro de Asterra (issue #352).
    /// </summary>
    public static class TextosDoLivro
    {
        /// <summary>Caído: orientação inicial do jogador caído.</summary>
        public const string CaidoAjuda = "Você caiu. Aguarde ajuda ou retorne a um ponto seguro.";

        /// <summary>Caído: aviso exibido após ser levantado por um aliado.</summary>
        public const string Levantado =
            "Levantado: recupere a vida antes de voltar à linha de frente.";

        /// <summary>Mira: orientação de mira e posicionamento fora de perigo.</summary>
        public const string AjudaMira =
            "Mire na direção do perigo. Mova-se para fora da área de ataque.";

        /// <summary>Defesa: aviso padrão de bloqueio parcial de impacto.</summary>
        public const string DefesaInsuficiente =
            "Defesa insuficiente para bloquear completamente este impacto.";

        /// <summary>Atributos: orientação de distribuição sem menção a raça (raça é visual).</summary>
        public const string AtributosOrientacao =
            "Distribua seus pontos. Seus equipamentos dão possibilidades.";

        /// <summary>Mundo: lema acolhedor na tela de carregamento/entrada no mundo.</summary>
        public const string EntradaNoMundo =
            "Entre, encontre seus amigos e continue de onde parou.";

        /// <summary>Diário: texto motivacional na janela de missões/expedições.</summary>
        public const string DiarioMissoes =
            "Cada saída pode render uma peça melhor, uma combinação nova ou uma história para contar.";
    }
}
