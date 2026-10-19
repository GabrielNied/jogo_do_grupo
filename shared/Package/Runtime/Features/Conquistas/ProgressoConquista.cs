namespace JogoDoGrupo.Shared.Features.Conquistas
{
    [System.Serializable]
    public class ProgressoConquista
    {
        public int ConquistaId;
        public int ProgressoAtual;
        public int Meta;
        public bool Concluida;
        public long ConcluidaEmUnix;
    }
}
