namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Nível, experiência e pontos da árvore de um personagem. Resposta de <c>GET</c>
    /// <see cref="RotasPersonagem.Progresso"/> (o client pede ao entrar no mundo). Depois disso as
    /// mudanças de experiência chegam por <see cref="ExperienciaGanha"/>.
    /// </summary>
    [System.Serializable]
    public class ProgressoResponse
    {
        public int Nivel = 1;

        /// <summary>Experiência acumulada dentro do nível atual.</summary>
        public long XpAtual = 0;

        /// <summary>Experiência que o nível atual pede para subir.</summary>
        public long XpDoNivel = 0;

        /// <summary>Pontos da árvore de atributos ainda não gastos (gastam em Talentos).</summary>
        public int PontosLivres = 0;
    }
}
