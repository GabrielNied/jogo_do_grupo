namespace JogoDoGrupo.Shared.Features.Npcs
{
    [System.Serializable]
    public sealed class FalarComNpc
    {
        public int NpcId;

        public FalarComNpc() { }

        public FalarComNpc(int npcId)
        {
            NpcId = npcId;
        }
    }
}
