namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>
    /// Estado da árvore de um personagem. Resposta de <c>GET</c> <see cref="RotasTalentos.Arvore"/>
    /// e de <c>POST</c> <see cref="RotasTalentos.Comprar"/>. Leva a configuração inteira
    /// (o client desenha a partir dela) e o que o personagem comprou.
    /// </summary>
    [System.Serializable]
    public class ArvoreResponse
    {
        public ConfiguracaoDaArvore Configuracao = new ConfiguracaoDaArvore();

        /// <summary>Pontos da árvore ainda não gastos (os mesmos <c>PontosLivres</c> do progresso).</summary>
        public int PontosLivres = 0;

        /// <summary>Pontos já pagos em nós (o que a redistribuição devolveria).</summary>
        public int PontosGastos = 0;

        public RankDoNo[] Ranks = new RankDoNo[0];

        /// <summary>Soma dos atributos principais que os nós comprados dão.</summary>
        public AtributosDaArvore Atributos = new AtributosDaArvore();
    }
}
