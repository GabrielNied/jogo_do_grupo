namespace JogoDoGrupo.Shared.Features.Desenvolvimento
{
    /// <summary>
    /// Servidor conta o que a ação da tela de admin fez. O client só mostra.
    /// Id: <see cref="DesenvolvimentoMessageIds.ResultadoDeAcaoDeAdmin"/>.
    /// </summary>
    [System.Serializable]
    public class ResultadoDeAcaoDeAdmin
    {
        public AcaoDeAdmin Acao = AcaoDeAdmin.GerarItem;

        /// <summary>Verdadeiro quando a ação rodou; falso quando foi recusada.</summary>
        public bool Aceito = false;

        /// <summary>Frase para a pessoa. Nunca nula.</summary>
        public string Texto = "";
    }
}
