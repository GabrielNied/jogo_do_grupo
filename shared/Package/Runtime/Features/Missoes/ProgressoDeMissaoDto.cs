namespace JogoDoGrupo.Shared.Features.Missoes
{
    [System.Serializable]
    public class ProgressoDeMissaoDto
    {
        public int MissaoId;
        public EstadoDaMissao Estado;
        public ProgressoDeObjetivo[] Objetivos = System.Array.Empty<ProgressoDeObjetivo>();
        public bool Acompanhada;
    }
}
