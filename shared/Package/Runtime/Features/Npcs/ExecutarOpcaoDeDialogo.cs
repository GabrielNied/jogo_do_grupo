namespace JogoDoGrupo.Shared.Features.Npcs
{
    public sealed class ExecutarOpcaoDeDialogo
    {
        public int NpcId { get; set; }
        public string OpcaoId { get; set; } = string.Empty;

        public ExecutarOpcaoDeDialogo() { }

        public ExecutarOpcaoDeDialogo(int npcId, string opcaoId)
        {
            NpcId = npcId;
            OpcaoId = opcaoId;
        }
    }
}
