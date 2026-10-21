using System;

namespace JogoDoGrupo.Shared.Features.Npcs
{
    [System.Serializable]
    public sealed class DialogoDoNpc
    {
        public int NpcId;
        public string Nome = string.Empty;
        public PapelNpc Papel;
        public string Fala = string.Empty;
        public OpcaoDeDialogo[] Opcoes = Array.Empty<OpcaoDeDialogo>();

        public DialogoDoNpc() { }

        public DialogoDoNpc(
            int npcId,
            string nome,
            PapelNpc papel,
            string fala,
            OpcaoDeDialogo[] opcoes
        )
        {
            NpcId = npcId;
            Nome = nome;
            Papel = papel;
            Fala = fala;
            Opcoes = opcoes ?? Array.Empty<OpcaoDeDialogo>();
        }
    }
}
