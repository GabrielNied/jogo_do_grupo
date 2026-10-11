namespace JogoDoGrupo.Shared.Core.TempoReal
{
    /// <summary>
    /// Client envia ao servidor logo após conectar pelo UDP para autenticar a sessão.
    /// O JWT do login (<see cref="Token"/>) identifica o usuário; <see cref="PersonagemId"/>
    /// indica qual personagem está entrando.
    /// Id: <see cref="TempoRealIds.AutenticarTempoReal"/>.
    /// </summary>
    [System.Serializable]
    public class AutenticarTempoReal
    {
        public string Token = "";
        public long PersonagemId = 0;
    }
}
