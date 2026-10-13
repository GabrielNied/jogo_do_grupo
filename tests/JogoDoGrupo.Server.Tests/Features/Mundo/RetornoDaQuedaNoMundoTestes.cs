using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

/// <summary>
/// Servidor completo (posição real do Mundo) com tempos curtos de queda para os testes não
/// esperarem 45 s.
/// </summary>
public sealed class ServidorMundoQueda : IDisposable
{
    public ServidorDeTeste Real { get; } = new();
    public WebApplicationFactory<Program> Falso { get; }
    public int Porta { get; }

    public ServidorMundoQueda()
    {
        Falso = Real.WithWebHostBuilder(b =>
            b.ConfigureServices(s =>
            {
                s.RemoveAll<ConfigQueda>();
                s.AddSingleton(ServidorDeQueda.Config);
            })
        );
        Porta = Falso.Services.GetRequiredService<ServidorTempoReal>().Porta;
    }

    public void Dispose()
    {
        Falso.Dispose();
        Real.Dispose();
    }
}

public class RetornoDaQuedaNoMundoTestes(ServidorMundoQueda servidor)
    : IClassFixture<ServidorMundoQueda>
{
    private static readonly PosicaoNaCidade Nascimento = ValidadorDeMovimento.Nascimento;

    private async Task<Jogador> NovoJogadorNaCidadeAsync()
    {
        var http = await ServidorDeTeste.CriarClienteLogadoEmAsync(servidor.Falso);
        var resposta = await ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        var personagem = await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta);

        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
            servidor.Porta,
            http.DefaultRequestHeaders.Authorization!.Parameter!,
            personagem.Id
        );
        Assert.True(resultado.Aceita, resultado.Codigo);

        var jogador = new Jogador(http, cliente, personagem.Id);
        jogador.Cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await jogador.Cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        return jogador;
    }

    private void ColocarLonge(Jogador j)
    {
        var teleporte = servidor.Falso.Services.GetRequiredService<ITeleporteDeTeste>();
        var (aceito, texto) = teleporte.Teleportar(j.Id, 50f, 0f, Nascimento.Z);
        Assert.True(aceito, texto);
        j.Cliente.Enviar(
            MundoMessageIds.ConfirmarPosicaoForcada,
            new ConfirmarPosicaoForcada { Sequencia = 1 }
        );
    }

    private void Derrubar(Jogador j) =>
        servidor
            .Falso.Services.GetRequiredService<EventosDoServidor>()
            .Publicar(new GolpeDeInimigoAcertou(1, 1, j.Id, 100_000, AreasDoMundo.Cidade));

    private static async Task<EstadoDeQueda> EsperarFaseAsync(Jogador j, FaseDeQueda fase)
    {
        while (true)
        {
            var estado = await j.Cliente.ReceberAsync<EstadoDeQueda>(
                CombateMessageIds.EstadoDeQueda
            );
            if (estado.Estado == fase && estado.PersonagemId == j.Id)
            {
                return estado;
            }
        }
    }

    private static void Voltar(Jogador j) =>
        j.Cliente.Enviar(CombateMessageIds.VoltarParaACidade, new VoltarParaACidade());

    [Fact]
    public async Task Voltar_da_queda_reposiciona_no_nascimento_da_cidade()
    {
        using var j = await NovoJogadorNaCidadeAsync();
        ColocarLonge(j);

        var posicoes = servidor.Falso.Services.GetRequiredService<IPosicoesDoMundo>();
        Assert.True(posicoes.TentarObter(j.Id, out var longe));
        Assert.Equal(50f, longe.X);

        Derrubar(j);
        await EsperarFaseAsync(j, FaseDeQueda.Caido);

        await Task.Delay(ServidorDeQueda.Config.MinimoParaVoltarMs + 100);
        Voltar(j);

        await EsperarFaseAsync(j, FaseDeQueda.Retorno);
        var forcada = await j.Cliente.ReceberAsync<PosicaoForcada>(MundoMessageIds.PosicaoForcada);
        Assert.Equal(Nascimento.X, forcada.X);
        Assert.Equal(Nascimento.Y, forcada.Y);
        Assert.Equal(Nascimento.Z, forcada.Z);
        Assert.Equal(Nascimento.RotacaoY, forcada.RotacaoY);

        Assert.True(posicoes.TentarObter(j.Id, out var noHub));
        Assert.Equal(Nascimento.X, noHub.X);
        Assert.Equal(Nascimento.Y, noHub.Y);
        Assert.Equal(Nascimento.Z, noHub.Z);
        Assert.Equal(Nascimento.RotacaoY, noHub.RotacaoY);
    }

    [Fact]
    public async Task Fim_do_tempo_caído_também_reposiciona_no_nascimento()
    {
        using var j = await NovoJogadorNaCidadeAsync();
        ColocarLonge(j);
        Derrubar(j);
        await EsperarFaseAsync(j, FaseDeQueda.Caido);

        await EsperarFaseAsync(j, FaseDeQueda.Retorno);
        var forcada = await j.Cliente.ReceberAsync<PosicaoForcada>(MundoMessageIds.PosicaoForcada);
        Assert.Equal(Nascimento.Z, forcada.Z);

        var posicoes = servidor.Falso.Services.GetRequiredService<IPosicoesDoMundo>();
        Assert.True(posicoes.TentarObter(j.Id, out var pos));
        Assert.Equal(Nascimento.X, pos.X);
        Assert.Equal(Nascimento.Z, pos.Z);
    }
}
