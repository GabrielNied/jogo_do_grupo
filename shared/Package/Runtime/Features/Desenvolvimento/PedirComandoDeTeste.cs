namespace JogoDoGrupo.Shared.Features.Desenvolvimento
{
    /// <summary>
    /// Client pede um comando de teste (experiência, teleporte, spawn). O servidor só executa
    /// com <c>Desenvolvimento:PermitirComandos</c>; sem isso, ignora e não responde.
    /// Id: <see cref="DesenvolvimentoMessageIds.PedirComandoDeTeste"/>.
    /// </summary>
    [System.Serializable]
    public class PedirComandoDeTeste
    {
        /// <summary>Texto do comando, como a pessoa digitou. No máximo <see cref="ComandosDeTeste.TextoMaximo"/>.</summary>
        public string Texto = "";
    }
}
