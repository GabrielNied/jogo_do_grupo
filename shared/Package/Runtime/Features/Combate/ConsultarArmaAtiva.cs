namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Client → servidor: pede confirmação de qual arma está no conjunto ativo.
    /// Resposta em <see cref="ArmaAtivaInformada"/>.
    /// Id: <see cref="CombateMessageIds.ConsultarArmaAtiva"/>.
    /// </summary>
    [System.Serializable]
    public class ConsultarArmaAtiva { }
}
