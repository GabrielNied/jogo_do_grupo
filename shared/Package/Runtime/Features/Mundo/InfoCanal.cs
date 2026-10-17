namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Informações e estado de lotação de um canal do mundo.</summary>
    public sealed class InfoCanal
    {
        public int Canal { get; set; }
        public int Jogadores { get; set; }
        public int Capacidade { get; set; }
    }
}
