namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Servidor → client: confirma a mudança de canal ou o canal atual de entrada.</summary>
    public sealed class CanalAlterado
    {
        public int Canal { get; set; }
        public int TotalCanais { get; set; }
    }
}
