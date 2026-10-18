namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>
    /// Client → Servidor: jogador solicita duelo 1v1 com outro personagem próximo.
    /// </summary>
    [System.Serializable]
    public class SolicitarDuelo
    {
        public long AlvoPersonagemId;
    }
}
