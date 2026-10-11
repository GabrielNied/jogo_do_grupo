using System.Net;
using System.Net.Sockets;
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
