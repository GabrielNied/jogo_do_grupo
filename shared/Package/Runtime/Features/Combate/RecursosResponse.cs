namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor responde (200) a <c>GET</c> <see cref="RotasCombate.Recursos"/> com os recursos
    /// atuais e máximos do personagem e os atributos finais. O client só mostra.
    /// </summary>
    [System.Serializable]
    public class RecursosResponse
    {
        public int VidaAtual;
        public int VidaMaxima;
        public int StaminaAtual;
        public int StaminaMaxima;
        public int ManaAtual;
        public int ManaMaxima;
        public AtributosCombate Atributos = new AtributosCombate();

        /// <summary>Os nove secundários, na ordem de <see cref="RegrasSecundarios.Todos"/>, com a origem de cada parcela.</summary>
        public SecundarioCalculado[] Secundarios = new SecundarioCalculado[0];
    }
}
