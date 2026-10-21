using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Controla o estado de combate de cada personagem: 5 segundos sem dar nem receber dano (docs/features/combate.md).
/// Implementa <see cref="IEstadoDeCombate"/> para consumo por outras funcionalidades (ADR 0016).
/// </summary>
public sealed class ServicoDeEstadoDeCombate : IEstadoDeCombate
{
    private readonly ConcurrentDictionary<long, long> _ultimoCombateMs = new();
    private readonly Func<long> _relogioMs;
    private Action<long>? _aoEntrarEmCombate;

    public ServicoDeEstadoDeCombate(Func<long>? relogioMs = null)
    {
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
    }

    public void DefinirCallbackEntrada(Action<long> callback) => _aoEntrarEmCombate = callback;

    public bool EmCombate(long personagemId)
    {
        if (!_ultimoCombateMs.TryGetValue(personagemId, out var ultimoMs))
        {
            return false;
        }

        var decorridoMs = _relogioMs() - ultimoMs;
        return decorridoMs >= 0
            && decorridoMs < ConfigRecuperacao.TempoParaSairDeCombateSegundos * 1000L;
    }

    public int SegundosRestantes(long personagemId)
    {
        if (!_ultimoCombateMs.TryGetValue(personagemId, out var ultimoMs))
        {
            return 0;
        }

        var decorridoMs = _relogioMs() - ultimoMs;
        var limiteMs = ConfigRecuperacao.TempoParaSairDeCombateSegundos * 1000L;
        if (decorridoMs >= limiteMs || decorridoMs < 0)
        {
            return 0;
        }

        return (int)Math.Ceiling((limiteMs - decorridoMs) / 1000.0);
    }

    public void RegistrarAtividade(long personagemId)
    {
        var estavaEmCombate = EmCombate(personagemId);
        _ultimoCombateMs[personagemId] = _relogioMs();
        if (!estavaEmCombate)
        {
            _aoEntrarEmCombate?.Invoke(personagemId);
        }
    }

    public void Limpar(long personagemId)
    {
        _ultimoCombateMs.TryRemove(personagemId, out _);
    }
}
