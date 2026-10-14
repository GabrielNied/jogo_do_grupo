using System.Net;
using System.Net.Sockets;
using System.Threading;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Core.TempoReal;

public class HostTempoRealTestes
{
    [Fact]
    public void Sobe_junto_com_o_app_na_porta_configurada()
    {
        using var servidor = new ServidorDeTeste();

        var tempoReal = servidor.Services.GetRequiredService<ServidorTempoReal>();

        Assert.InRange(tempoReal.Porta, 1, ushort.MaxValue);
        Assert.Empty(tempoReal.Sessoes);
    }

    [Fact]
    public async Task Parar_duas_vezes_ao_mesmo_tempo_nao_falha()
    {
        using var servidor = new ServidorDeTeste();
        var tempoReal = servidor.Services.GetRequiredService<ServidorTempoReal>();

        await Task.WhenAll(
            Enumerable.Range(0, 4).Select(_ => Task.Run(() => tempoReal.StopAsync(default)))
        );
    }

    [Fact]
    public async Task NoLaco_executa_no_laco_do_servidor()
    {
        using var servidor = new ServidorDeTeste();
        var tempoReal = servidor.Services.GetRequiredService<ServidorTempoReal>();
        var threadDoTeste = Thread.CurrentThread.ManagedThreadId;
        var concluido = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        tempoReal.NoLaco(() =>
        {
            concluido.TrySetResult(Thread.CurrentThread.ManagedThreadId);
        });

        var threadDoLoop = await concluido.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotEqual(threadDoTeste, threadDoLoop);
    }

    [Fact]
    public async Task AoPasso_chama_a_acao_no_laco_em_intervalo_fixo()
    {
        using var servidor = new ServidorDeTeste();
        var tempoReal = servidor.Services.GetRequiredService<ServidorTempoReal>();
        var chamadas = 0;
        var concluido = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        tempoReal.AoPasso(
            TimeSpan.FromMilliseconds(20),
            _ =>
            {
                if (Interlocked.Increment(ref chamadas) >= 2)
                {
                    concluido.TrySetResult(true);
                }
            }
        );

        await concluido.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(chamadas >= 2);
    }

    [Fact]
    public async Task TentarObterSessao_encontra_a_sessao_ativa()
    {
        using var fixture = new TempoRealFixture();
        var (cliente, contaId, personagemId) = await fixture.EntrarAsync();

        try
        {
            Assert.True(fixture.TempoReal.TentarObterSessao(personagemId, out var sessao));
            Assert.NotNull(sessao);
            Assert.Same(sessao, fixture.SessaoDaConta(contaId));
        }
        finally
        {
            cliente.Sair();
        }
    }

    [Fact]
    public async Task TentarObterSessao_nao_encontra_apos_desconectar()
    {
        using var fixture = new TempoRealFixture();
        var (cliente, _, personagemId) = await fixture.EntrarAsync();

        cliente.Sair();

        await TempoRealFixture.EsperarAsync(
            () => !fixture.TempoReal.TentarObterSessao(personagemId, out _),
            "sessão do personagem continuar indexada após desconectar"
        );

        Assert.False(fixture.TempoReal.TentarObterSessao(personagemId, out _));
    }

    [Fact]
    public void Porta_ocupada_impede_o_servidor_de_subir()
    {
        using var ocupante = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var porta = ((IPEndPoint)ocupante.Client.LocalEndPoint!).Port;
        using var servidor = new ServidorDeTeste();
        using var fabrica = servidor.WithWebHostBuilder(builder =>
            builder.UseSetting("TempoReal:Porta", $"{porta}")
        );

        var erro = Assert.ThrowsAny<Exception>(() => fabrica.CreateClient());

        Assert.Contains($"porta UDP {porta}", erro.ToString());
    }
}
