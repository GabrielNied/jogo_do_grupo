namespace JogoDoGrupo.Shared.Features.Npcs
{
    [System.Serializable]
    public sealed class ExecutarOpcaoDeDialogo
    {
        public int NpcId;
        public string OpcaoId = string.Empty;

        public ExecutarOpcaoDeDialogo() { }

        public ExecutarOpcaoDeDialogo(int npcId, string opcaoId)
        {
            NpcId = npcId;
            OpcaoId = opcaoId;
        }
    }
}
