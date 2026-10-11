using JogoDoGrupo.Server.Contratos.Mundo;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Infra;

/// <summary>
/// Espera o servidor processar um movimento. O servidor não devolve mais a posição ao dono
/// (ADR 0024), então o teste consulta a posição dele por <see cref="IPosicoesDoMundo"/>.
/// </summary>
public static class PosicaoNoServidor
{
    public static async Task<PosicaoNoMundo> EsperarAsync(
        IServiceProvider servicos,
        long personagemId,
        Func<PosicaoNoMundo, bool> condicao,
        int limiteMs = 3000
    )
    {
        var posicoes = servicos.GetRequiredService<IPosicoesDoMundo>();
        var fim = Environment.TickCount64 + limiteMs;
        while (true)
        {
            if (posicoes.TentarObter(personagemId, out var posicao) && condicao(posicao))
            {
                return posicao;
            }

            if (Environment.TickCount64 > fim)
            {
                throw new TimeoutException(
                    $"A posição do personagem {personagemId} não chegou ao esperado em {limiteMs} ms."
                );
            }

            await Task.Delay(10);
        }
    }
}
