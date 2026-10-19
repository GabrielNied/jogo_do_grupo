namespace JogoDoGrupo.Shared.Features.Conquistas
{
    [System.Serializable]
    public class DadosDeConquistas
    {
        public int PontosTotais;
        public string TituloAtivo = "";
        public string[] TitulosDesbloqueados = new string[0];
        public ProgressoConquista[] Conquistas = new ProgressoConquista[0];
    }
}
