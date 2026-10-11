namespace JogoDoGrupo.Shared.Features.Desenvolvimento
{
    /// <summary>
    /// Servidor conta o que o comando de teste fez. O client só mostra.
    /// Id: <see cref="DesenvolvimentoMessageIds.ResultadoDeComandoDeTeste"/>.
    /// </summary>
    [System.Serializable]
    public class ResultadoDeComandoDeTeste
    {
        /// <summary>Verdadeiro quando o comando rodou; falso quando o texto foi recusado.</summary>
        public bool Aceito = false;

        /// <summary>Frase para a pessoa. Nunca nula.</summary>
        public string Texto = "";
    }
}
