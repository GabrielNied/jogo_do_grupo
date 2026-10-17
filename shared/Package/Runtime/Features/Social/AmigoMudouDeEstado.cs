namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor avisa que um amigo entrou no jogo, saiu ou trocou de personagem. O client troca
    /// só essa linha da lista. Id: <see cref="SocialMessageIds.AmigoMudouDeEstado"/>.
    /// </summary>
    [System.Serializable]
    public class AmigoMudouDeEstado
    {
        /// <summary>O mesmo valor de <see cref="AmigoDaLista.AmigoId"/>.</summary>
        public long AmigoId = 0;

        public bool Online = false;

        /// <summary>Nome do personagem que o amigo está usando agora; vazio se offline.</summary>
        public string PersonagemOnline = "";

        /// <summary>Área resumida quando online (#904).</summary>
        public string Area = "";
    }
}
