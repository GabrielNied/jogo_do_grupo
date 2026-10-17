using System.Collections.Concurrent;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Rastreia personagens cujos atributos do equipamento mudaram e precisam ser relidos.
/// Thread-safe e sem trava, consumido na próxima leitura de atributos (ADR 0016).
/// </summary>
internal sealed class InvalidadorDeEquipamento
{
    private readonly ConcurrentDictionary<long, byte> _invalidados = new();

    public void Invalidar(long personagemId) => _invalidados[personagemId] = 0;

    public bool ConsumirSeInvalido(long personagemId) =>
        _invalidados.TryRemove(personagemId, out _);
}
