namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Servidor → client: confirma a mudança de canal ou o canal atual de entrada.</summary>
    [System.Serializable]
    public sealed class CanalAlterado
    {
        public int Canal;
        public int TotalCanais;
    }
}
