namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor avisa o caído, o ajudante e quem está na área que o estado de queda de um
    /// personagem mudou. É o estado inteiro, não a diferença.
    /// Id: <see cref="CombateMessageIds.EstadoDeQueda"/>.
    /// </summary>
    [System.Serializable]
    public class EstadoDeQueda
    {
        /// <summary>O personagem que caiu, está sendo levantado, levantou ou voltou.</summary>
        public long PersonagemId = 0;

        public FaseDeQueda Estado = FaseDeQueda.Vivo;

        /// <summary>
        /// Segundos (inteiros, para cima) até ele voltar sozinho à cidade. Zero fora de
        /// <see cref="FaseDeQueda.Caido"/> e <see cref="FaseDeQueda.Levantando"/>.
        /// </summary>
        public int SegundosRestantes = 0;

        /// <summary>Quem está levantando. Zero se ninguém.</summary>
        public long AjudanteId = 0;

        /// <summary>
        /// Quanto da interação já passou, em ms (de 0 a
        /// <see cref="RegrasQueda.TempoParaLevantarMs"/>). Zero se ninguém está levantando.
        /// </summary>
        public int ProgressoMs = 0;
    }
}
