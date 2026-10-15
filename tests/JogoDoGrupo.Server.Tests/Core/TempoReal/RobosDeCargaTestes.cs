using JogoDoGrupo.Robos;
using JogoDoGrupo.Server.Core.Observabilidade;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Core.TempoReal;

/// <summary>
/// Carga curta com os robôs de tools/JogoDoGrupo.Robos (#150): pega regressão de protocolo
/// (conta, autenticação, entrada, movimento, ataque e réplica) sem abrir o Unity. Os números de
/// carga de verdade saem do console, à mão; aqui só se confere que tudo funciona e é medido.
/// </summary>
public class RobosDeCargaTestes
{
    [Fact]
    public async Task Cinco_robos_entram_andam_atacam_e_sao_medidos_sem_erro()
    {
        using var servidor = new ServidorDeTeste();
        using (servidor.CreateClient()) { } // sobe o host
        var tempoReal = servidor.Services.GetRequiredService<ServidorTempoReal>();
        var metricasDoServidor = servidor.Services.GetRequiredService<MetricasDoServidor>();
        var opcoes = new OpcoesDeCarga
        {
            Robos = 5,
            DuracaoSegundos = 5,
            EntradasPorSegundo = 50,
            HostTempoReal = "127.0.0.1",
            PortaTempoReal = tempoReal.Porta,
            AtaqueACadaMs = 1000,
        };

        var relatorio = await CargaDeRobos.RodarAsync(
            opcoes,
            servidor.CreateClient,
            _ =>
            {
                var m = metricasDoServidor.Obter();
                return Task.FromResult<TickDoServidor?>(
                    new TickDoServidor
                    {
                        JogadoresOnline = m.JogadoresOnline,
                        ErrosNoUltimoMinuto = m.ErrosNoUltimoMinuto,
                        PassosPorSegundo = m.Passos.PassosPorSegundo,
                        DuracaoP50Ms = m.Passos.DuracaoP50Ms,
                        DuracaoP95Ms = m.Passos.DuracaoP95Ms,
                        DuracaoMaximaMs = m.Passos.DuracaoMaximaMs,
                    }
                );
            },
            null,
            CancellationToken.None
        );

        Assert.True(relatorio.TotalDeErros == 0, relatorio.Texto());
        Assert.Equal(5, relatorio.RobosNoMundo);
        Assert.True(relatorio.EnviadasPorSegundo > 0, relatorio.Texto());
        // Cada robô vê os outros quatro andando: a réplica precisa chegar.
        Assert.True(relatorio.ReplicasPorSegundo > 0, relatorio.Texto());
        Assert.True(relatorio.Latencia.Amostras > 0, relatorio.Texto());
        Assert.Equal(5, relatorio.Servidor!.JogadoresOnline);
        Assert.True(relatorio.Servidor.PassosPorSegundo > 0, relatorio.Texto());
    }
}
