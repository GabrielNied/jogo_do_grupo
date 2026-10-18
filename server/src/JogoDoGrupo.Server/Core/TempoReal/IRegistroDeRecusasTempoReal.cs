using JogoDoGrupo.Shared.Core.Api;

namespace JogoDoGrupo.Server.Core.TempoReal;

/// <summary>
/// Contrato do monitor de recusas de mensagens de tempo real por conta para ferramentas de GM (#523, #211).
/// </summary>
public interface IRegistroDeRecusasTempoReal
{
    void Observar(ServidorTempoReal tempoReal);
    void RegistrarRecusa(long contaId, long personagemId, ushort mensagemId, MotivoRecusa motivo);
    RelatorioRecusasContaDto? Obter(long contaId);
    IReadOnlyList<RelatorioRecusasContaDto> ListarTodas();
    IReadOnlyList<RelatorioRecusasContaDto> ListarSuspeitas();
    bool Limpar(long contaId);
    int TotalContasComRecusas { get; }
    int TotalContasEmAlerta { get; }
}
