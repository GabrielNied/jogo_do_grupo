namespace JogoDoGrupo.Shared.Features.Npcs
{
    [System.Serializable]
    public sealed class DialogoRecusado
    {
        public int NpcId;
        public MotivoRecusaDialogo Motivo;

        public DialogoRecusado() { }

        public DialogoRecusado(int npcId, MotivoRecusaDialogo motivo)
        {
            NpcId = npcId;
            Motivo = motivo;
        }
    }
}
