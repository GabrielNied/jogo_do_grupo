using System.Diagnostics;

namespace JogoDoGrupo.Robos;

/// <summary>
/// Sobe N robôs aos poucos, espera todos entrarem e mede pelo tempo pedido, com todos andando e
/// atacando. O console (<c>Program</c>) e o teste de integração usam esta mesma classe.
/// </summary>
public static class CargaDeRobos
{
    /// <param name="criarHttp">Um <see cref="HttpClient"/> novo por robô, com a URL da API.</param>
    /// <param name="lerTick">Lê as métricas do servidor no fim da medição (ou <c>null</c>).</param>
    /// <param name="progresso">Recebe uma linha de andamento (opcional).</param>
    public static async Task<RelatorioDeCarga> RodarAsync(
        OpcoesDeCarga opcoes,
        Func<HttpClient> criarHttp,
        Func<CancellationToken, Task<TickDoServidor?>>? lerTick,
        Action<string>? progresso,
        CancellationToken parada
    )
    {
        var metricas = new ColetorDeMetricas();
        var robos = new Robo[opcoes.Robos];
        using var fimDosRobos = CancellationTokenSource.CreateLinkedTokenSource(parada);
        var acoes = new List<Task>();
        var travaAcoes = new object();
        try
        {
            progresso?.Invoke($"Entrando {opcoes.Robos} robôs...");
            var entradas = Enumerable
                .Range(0, opcoes.Robos)
                .Select(async i =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(i / opcoes.EntradasPorSegundo), parada);
                    var robo = robos[i] = new Robo(
                        i,
                        $"{opcoes.Prefixo}_{i + 1:D4}",
                        opcoes,
                        metricas
                    );
                    if (await EntrarAsync(robo, criarHttp, metricas, parada))
                    {
                        lock (travaAcoes)
                        {
                            acoes.Add(robo.AgirAsync(fimDosRobos.Token));
                        }
                    }
                })
                .ToArray();
            await Task.WhenAll(entradas);

            var noMundo = robos.Count(r => r.NoMundo);
            progresso?.Invoke(
                $"{noMundo}/{opcoes.Robos} no mundo. Medindo por {opcoes.DuracaoSegundos} s..."
            );
            var inicio = Stopwatch.GetTimestamp();
            metricas.Medindo = true;
            await Task.Delay(TimeSpan.FromSeconds(opcoes.DuracaoSegundos), parada);
            metricas.Medindo = false;
            var segundos = Stopwatch.GetElapsedTime(inicio).TotalSeconds;

            // Lê com a carga ainda ligada: o resumo do servidor cobre só os últimos segundos.
            TickDoServidor? tick = null;
            if (lerTick != null)
            {
                try
                {
                    tick = await lerTick(parada);
                }
                catch (Exception e) when (e is HttpRequestException or InvalidOperationException)
                {
                    progresso?.Invoke($"Não deu para ler o tick do servidor: {e.Message}");
                }
            }

            return metricas.Relatorio(opcoes, noMundo, segundos, tick);
        }
        finally
        {
            await fimDosRobos.CancelAsync();
            Task[] pendentes;
            lock (travaAcoes)
            {
                pendentes = acoes.ToArray();
            }

            await Task.WhenAll(pendentes);
            foreach (var robo in robos)
            {
                if (robo != null)
                {
                    await robo.DisposeAsync();
                }
            }
        }
    }

    private static async Task<bool> EntrarAsync(
        Robo robo,
        Func<HttpClient> criarHttp,
        ColetorDeMetricas metricas,
        CancellationToken parada
    )
    {
        try
        {
            string token;
            using (var http = criarHttp())
            {
                token = await robo.PrepararContaAsync(http, parada);
            }

            await robo.EntrarAsync(token);
            return true;
        }
        catch (FalhaDoRobo e)
        {
            metricas.Erro(e.Tipo, e.Message);
        }
        catch (HttpRequestException e)
        {
            metricas.Erro(TipoDeErro.Api, e.Message);
        }

        return false;
    }
}
