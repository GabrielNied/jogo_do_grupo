namespace JogoDoGrupo.Shared.Features.Npcs
{
    public sealed class DialogoRecusado
    {
        public int NpcId { get; set; }
        public MotivoRecusaDialogo Motivo { get; set; }

        public DialogoRecusado() { }

        public DialogoRecusado(int npcId, MotivoRecusaDialogo motivo)
        {
            NpcId = npcId;
            Motivo = motivo;
        }
    }
}
