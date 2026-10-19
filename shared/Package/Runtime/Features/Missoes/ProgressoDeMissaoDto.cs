namespace JogoDoGrupo.Shared.Features.Missoes
{
    public class ProgressoDeMissaoDto
    {
        public int MissaoId { get; set; }
        public EstadoDaMissao Estado { get; set; }
        public ProgressoDeObjetivo[] Objetivos { get; set; } =
            System.Array.Empty<ProgressoDeObjetivo>();
        public bool Acompanhada { get; set; }
    }
}
