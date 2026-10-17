using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Core.Canais;

/// <summary>
/// Gerencia os canais lógicos do mundo persistente, balanceamento de entrada e capacidade (ADR 0045).
/// </summary>
public interface IGerenciadorDeCanais
{
    int QuantidadeCanais { get; }
    int CapacidadePorCanal { get; }

    int? ObterCanalDoPersonagem(long personagemId);
    int OcupacaoDoCanal(int canal);
    IReadOnlyList<InfoCanal> ObterStatusDosCanais();

    int? EscolherCanalDeEntrada(long personagemId, IEnumerable<long>? membrosDoGrupo = null);

    bool RegistrarEntrada(long personagemId, int canal);
    void RegistrarSaida(long personagemId);

    ResultadoTrocaDeCanal TentarTrocarCanal(long personagemId, int canalDestino, bool emCombate);
}
