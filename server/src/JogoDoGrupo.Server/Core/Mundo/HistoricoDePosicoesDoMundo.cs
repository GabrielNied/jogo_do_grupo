using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Core.Mundo;

/// <inheritdoc cref="IHistoricoDePosicoesDoMundo"/>
public sealed class HistoricoDePosicoesDoMundo : IHistoricoDePosicoesDoMundo
{
    private readonly ConcurrentDictionary<long, List<Amostra>> _porEntidade = new();

    public void Registrar(long entidadeId, PosicaoNoMundo posicao, long instanteMs)
    {
        var lista = _porEntidade.GetOrAdd(entidadeId, _ => []);
        lock (lista)
        {
            lista.Add(new Amostra(instanteMs, posicao));
            var limite = RegrasCompensacaoDeLag.LimiteDeRetencaoMs(instanteMs);
            lista.RemoveAll(a => a.InstanteMs < limite);
        }
    }

    public bool TentarObterEm(long entidadeId, long instanteMs, out PosicaoNoMundo posicao)
    {
        posicao = default;
        if (!_porEntidade.TryGetValue(entidadeId, out var lista))
        {
            return false;
        }

        lock (lista)
        {
            Amostra? escolhida = null;
            foreach (var amostra in lista)
            {
                if (amostra.InstanteMs <= instanteMs)
                {
                    escolhida = amostra;
                }
            }

            if (escolhida == null)
            {
                return false;
            }

            posicao = escolhida.Posicao;
            return true;
        }
    }

    public int QuantidadeDeAmostras(long entidadeId)
    {
        if (!_porEntidade.TryGetValue(entidadeId, out var lista))
        {
            return 0;
        }

        lock (lista)
        {
            return lista.Count;
        }
    }

    private sealed record Amostra(long InstanteMs, PosicaoNoMundo Posicao);
}
