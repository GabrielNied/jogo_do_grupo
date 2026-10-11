namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Client do ajudante pede para começar a levantar um aliado caído (a tecla de interagir foi
    /// pressionada perto dele). O servidor confere alcance e estado; a resposta é um
    /// <see cref="EstadoDeQueda"/> ou uma <see cref="AcaoRecusada"/>.
    /// Id: <see cref="CombateMessageIds.ComecarALevantar"/>.
    /// </summary>
    [System.Serializable]
    public class ComecarALevantar
    {
        /// <summary>O personagem caído que será levantado.</summary>
        public long AlvoId = 0;
    }
}
