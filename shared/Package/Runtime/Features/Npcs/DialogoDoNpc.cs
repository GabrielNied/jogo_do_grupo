using System;

namespace JogoDoGrupo.Shared.Features.Npcs
{
    public sealed class DialogoDoNpc
    {
        public int NpcId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public PapelNpc Papel { get; set; }
        public string Fala { get; set; } = string.Empty;
        public OpcaoDeDialogo[] Opcoes { get; set; } = Array.Empty<OpcaoDeDialogo>();

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
