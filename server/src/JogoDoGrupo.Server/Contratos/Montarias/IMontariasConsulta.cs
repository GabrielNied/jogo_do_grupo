namespace JogoDoGrupo.Server.Contratos.Montarias;

/// <summary>
/// Consulta se um personagem está montado (ADR 0016).
/// </summary>
public interface IMontariasConsulta
{
    /// <summary>Verdadeiro se o personagem está montado no momento.</summary>
    bool EstaMontado(long personagemId);

    /// <summary>Tipo da montaria se estiver montado, ou null se a pé.</summary>
    int? TipoDaMontaria(long personagemId);

    /// <summary>Tipo da montaria atualmente equipada pelo personagem.</summary>
    int ObterMontariaEquipada(long personagemId);

    /// <summary>Verifica se a conta possui a montaria indicada desbloqueada.</summary>
    bool PossuiMontaria(long contaId, int tipoMontaria);

    /// <summary>Retorna a coleção de montarias desbloqueadas da conta.</summary>
    IReadOnlyCollection<int> ObterColecao(long contaId);
}
