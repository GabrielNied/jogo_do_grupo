namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>Um amigo em <see cref="ListaDeAmigos.Amigos"/>.</summary>
    [System.Serializable]
    public class AmigoDaLista
    {
        /// <summary>Gerado pelo servidor para esta amizade; não é o id da conta.</summary>
        public long AmigoId = 0;

        /// <summary>Nome do personagem pelo qual a amizade foi feita; não muda.</summary>
        public string Nome = "";

        /// <summary>Verdadeiro se algum personagem da conta amiga está no jogo agora.</summary>
        public bool Online = false;

        /// <summary>Nome do personagem que o amigo está usando agora; vazio se offline.</summary>
        public string PersonagemOnline = "";

        /// <summary>Área resumida quando online (Cidade, Campo, Dungeon); vazio se offline (#904).</summary>
        public string Area = "";
    }
}
