namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Nível, experiência e pontos de atributo de um personagem. Resposta de <c>GET</c>
    /// <see cref="RotasPersonagem.Progresso"/> (o client pede ao entrar no mundo) e de
    /// <c>POST</c> <see cref="RotasPersonagem.Atributos"/>. Depois disso as mudanças de
    /// experiência chegam por <see cref="ExperienciaGanha"/>.
    /// </summary>
    [System.Serializable]
    public class ProgressoResponse
    {
        public int Nivel = 1;

        /// <summary>Experiência acumulada dentro do nível atual.</summary>
        public long XpAtual = 0;

        /// <summary>Experiência que o nível atual pede para subir.</summary>
        public long XpDoNivel = 0;

        /// <summary>Pontos de atributo ainda não distribuídos.</summary>
        public int PontosLivres = 0;

        /// <summary>Total de pontos que o jogador já distribuiu em cada atributo.</summary>
        public PontosDeAtributos Distribuidos = new PontosDeAtributos();
    }
}
