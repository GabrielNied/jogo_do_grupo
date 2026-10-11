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
        public int VigorAtual;
        public int VigorMaximo;
        public int EnergiaAtual;
        public int EnergiaMaxima;
        public AtributosCombate Atributos = new AtributosCombate();
    }
}
