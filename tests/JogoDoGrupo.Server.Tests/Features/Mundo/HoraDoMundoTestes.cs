using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Mundo;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

/// <summary>
/// Integração do dia e noite: client UDP de verdade contra o servidor. O relógio é o real,
/// então as horas são comparadas com a do servidor, com folga; a conta em si é testada com o
/// tempo controlado em <see cref="RelogioDoMundoTestes"/>.
/// </summary>
public class HoraDoMundoTestes(TempoRealFixture f) : IClassFixture<TempoRealFixture>
{
    // Folga em horas do mundo: 0,2 h são 10 s reais, bem mais que a espera de um teste.
    private const float Folga = 0.2f;

    private RelogioDoMundo Relogio => f.Fabrica.Services.GetRequiredService<RelogioDoMundo>();

    private async Task<ClienteTempoRealDeTeste> NaCidadeAsync()
    {
        var (cliente, _, _) = await f.EntrarAsync();
        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        return cliente;
    }

    private static Task<HoraDoMundo> HoraAsync(ClienteTempoRealDeTeste cliente) =>
        cliente.ReceberAsync<HoraDoMundo>(MundoMessageIds.HoraDoMundo);

    private static int Horas(ClienteTempoRealDeTeste cliente) =>
        cliente.IdsRecebidos.Count(id => id == MundoMessageIds.HoraDoMundo);

    // Diferença entre duas horas levando em conta a volta da meia-noite.
    private static float Distancia(float a, float b)
    {
        var d = Math.Abs(a - b);
        return Math.Min(d, RegrasDiaENoite.HorasPorDia - d);
    }

    [Fact]
    public async Task Quem_entra_recebe_a_hora_que_esta_valendo_no_servidor()
    {
        using var ana = await NaCidadeAsync();

        var hora = await HoraAsync(ana);

        Assert.InRange(hora.Hora, 0f, 23.9999f);
        Assert.True(Distancia(Relogio.Hora(), hora.Hora) < Folga);
        Assert.Equal(RegrasDiaENoite.DuracaoDoDiaSegundos, hora.DuracaoDoDiaSegundos);
    }

    [Fact]
    public async Task Outra_pessoa_entrar_nao_recomeca_o_ciclo()
    {
        using var ana = await NaCidadeAsync();
        var daAna = await HoraAsync(ana);

        using var beto = await NaCidadeAsync();
        var doBeto = await HoraAsync(beto);

        Assert.True(Distancia(daAna.Hora, doBeto.Hora) < Folga);
        Assert.True(Distancia(Relogio.Hora(), doBeto.Hora) < Folga);
    }

    [Fact]
    public async Task Sincronia_manda_a_mesma_hora_a_todos_os_presentes()
    {
        using var ana = await NaCidadeAsync();
        using var beto = await NaCidadeAsync();
        await HoraAsync(ana);
        await HoraAsync(beto);

        f.Fabrica.Services.GetRequiredService<ServicoDaHoraDoMundo>().Sincronizar();

        var daAna = await HoraAsync(ana);
        var doBeto = await HoraAsync(beto);
        Assert.Equal(daAna.Hora, doBeto.Hora);
        Assert.True(Distancia(Relogio.Hora(), daAna.Hora) < Folga);
    }

    [Fact]
    public async Task Quem_nao_entrou_na_area_nao_recebe_a_hora()
    {
        var (fora, _, _) = await f.EntrarAsync();
        using var _fora = fora;
        using var ana = await NaCidadeAsync();
        await HoraAsync(ana);

        f.Fabrica.Services.GetRequiredService<ServicoDaHoraDoMundo>().Sincronizar();

        await HoraAsync(ana);
        Assert.Equal(0, Horas(fora));
    }

    [Fact]
    public async Task Quem_saiu_da_area_para_de_receber()
    {
        using var ana = await NaCidadeAsync();
        using var beto = await NaCidadeAsync();
        await HoraAsync(ana);
        await HoraAsync(beto);
        beto.Sair();
        await ana.ReceberAsync<JogadorSaiu>(MundoMessageIds.JogadorSaiu);

        var erro = Record.Exception(() =>
            f.Fabrica.Services.GetRequiredService<ServicoDaHoraDoMundo>().Sincronizar()
        );

        Assert.Null(erro);
        await HoraAsync(ana);
    }
}
