namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>
    /// Configuração da árvore, toda por dados. A <see cref="Versao"/> sobe a cada mudança que
    /// invalida o que o jogador já comprou; o servidor confere a versão em toda compra.
    /// </summary>
    [System.Serializable]
    public class ConfiguracaoDaArvore
    {
        public int Versao = 1;
        public NoDaArvore[] Nos = new NoDaArvore[0];
    }
}
