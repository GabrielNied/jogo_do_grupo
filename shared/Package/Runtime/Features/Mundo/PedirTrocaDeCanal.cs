namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Client → servidor: solicita troca para um canal específico (ADR 0045).</summary>
    [System.Serializable]
    public sealed class PedirTrocaDeCanal
    {
        public int CanalDestino;
    }
}
