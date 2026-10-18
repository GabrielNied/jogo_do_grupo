using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Pvp;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Features.Instancias;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Pvp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Xunit;
using PersonagemEntidade = JogoDoGrupo.Server.Features.Personagem.Personagem;

namespace JogoDoGrupo.Server.Tests.Features.Pvp;

public sealed record Jogador(ClienteTempoRealDeTeste Cliente, long PersonagemId, string Token)
    : IDisposable
{
    public void Dispose() => Cliente.Dispose();
}

public sealed class PvpFixture : IDisposable
{
    private static long _proximoPersonagem = 7000;
    private readonly ServidorDeTeste _base = new();

    public PvpFixture(Action<IServiceCollection>? configurarServicos = null)
    {
        Relogio = new FakeTimeProvider(DateTimeOffset.UtcNow);
        Fabrica = _base.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("TempoReal:TempoParaAutenticarMs", "3000");
            builder.UseSetting("TempoReal:TempoQuedaMs", "1500");
            builder.ConfigureServices(servicos =>
            {
                servicos.RemoveAll<TimeProvider>();
                servicos.AddSingleton<TimeProvider>(Relogio);
                servicos.AddSingleton<IVerificadorPersonagem>(Verificador);

                var existentes = servicos.Last(descritor =>
                    descritor.ServiceType == typeof(IPosicoesDoMundo)
                );
                var posicoesReais = (IPosicoesDoMundo)existentes.ImplementationInstance!;
                Posicoes = new PosicoesDeTeste(posicoesReais);
                servicos.RemoveAll<IPosicoesDoMundo>();
                servicos.AddSingleton<IPosicoesDoMundo>(Posicoes);

                configurarServicos?.Invoke(servicos);
            });
        });
        TempoReal = Fabrica.Services.GetRequiredService<ServidorTempoReal>();
        Servico = Fabrica.Services.GetRequiredService<ServicoDePvp>();
        Alvos = Fabrica.Services.GetRequiredService<AlvosDePvp>();
    }

    public WebApplicationFactory<Program> Fabrica { get; }
    public FakeTimeProvider Relogio { get; }
    public VerificadorFalso Verificador { get; } = new();
    public PosicoesDeTeste Posicoes { get; private set; } = null!;
    public ServidorTempoReal TempoReal { get; }
    public ServicoDePvp Servico { get; }
    public AlvosDePvp Alvos { get; }

    public async Task<Jogador> EntrarAsync(double x = 0, double z = 0)
    {
        using var http = await ServidorDeTeste.CriarClienteLogadoEmAsync(Fabrica);
        var token = http.DefaultRequestHeaders.Authorization!.Parameter!;
        var contaId = long.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Subject!);
        var personagemId = Interlocked.Increment(ref _proximoPersonagem);

        await using (var escopo = Fabrica.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            db.Set<PersonagemEntidade>()
                .Add(
                    new PersonagemEntidade
                    {
                        Id = personagemId,
                        ContaId = contaId,
                        Nome = $"Personagem_{personagemId}",
                        NomeNormalizado = $"personagem_{personagemId}",
                        Raca = Raca.Valdren,
                        Nivel = 1,
                        CriadoEm = DateTime.UtcNow,
                    }
                );
            await db.SaveChangesAsync();
        }

        Verificador.Permitir(contaId, personagemId);
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
            TempoReal.Porta,
            token,
            personagemId
        );
        Assert.True(resultado.Aceita, resultado.Codigo);
        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        Posicoes.Definir(
            personagemId,
            new PosicaoNoMundo(AreasDoMundo.Cidade, (float)x, 0f, (float)z, 0)
        );
        return new Jogador(cliente, personagemId, token);
    }

    public void Dispose()
    {
        Fabrica.Dispose();
        _base.Dispose();
    }
}

public sealed class PvpServidorTestes
{
    [Fact]
    public async Task Duelo_solicitacao_para_alvo_invalido_e_recusada()
    {
        using var f = new PvpFixture();
        using var jogador = await f.EntrarAsync(0, 0);

        // Desafiar a si mesmo
        jogador.Cliente.Enviar(
            PvpMessageIds.SolicitarDuelo,
            new SolicitarDuelo { AlvoPersonagemId = jogador.PersonagemId }
        );

        var recusa = await jogador.Cliente.ReceberAsync<DueloRecusado>(PvpMessageIds.DueloRecusado);
        Assert.Equal(CodigosErroPvp.AlvoInvalido, recusa.Motivo);
    }

    [Fact]
    public async Task Duelo_solicitacao_valida_envia_convite_ao_desafiado()
    {
        using var f = new PvpFixture();
        using var desafiante = await f.EntrarAsync(0, 0);
        using var desafiado = await f.EntrarAsync(2, 0);

        desafiante.Cliente.Enviar(
            PvpMessageIds.SolicitarDuelo,
            new SolicitarDuelo { AlvoPersonagemId = desafiado.PersonagemId }
        );

        var convite = await desafiado.Cliente.ReceberAsync<ConviteDeDueloRecebido>(
            PvpMessageIds.ConviteDeDueloRecebido
        );
        Assert.Equal(desafiante.PersonagemId, convite.DesafianteId);
        Assert.Equal(RegrasPvp.TempoExpiracaoConviteDueloSegundos, convite.SegundosParaExpirar);
    }

    [Fact]
    public async Task Duelo_aceito_inicia_combate_com_contagem_regressiva()
    {
        using var f = new PvpFixture();
        using var desafiante = await f.EntrarAsync(0, 0);
        using var desafiado = await f.EntrarAsync(2, 0);

        desafiante.Cliente.Enviar(
            PvpMessageIds.SolicitarDuelo,
            new SolicitarDuelo { AlvoPersonagemId = desafiado.PersonagemId }
        );
        await desafiado.Cliente.ReceberAsync<ConviteDeDueloRecebido>(
            PvpMessageIds.ConviteDeDueloRecebido
        );

        desafiado.Cliente.Enviar(
            PvpMessageIds.ResponderConviteDeDuelo,
            new ResponderConviteDeDuelo { DesafianteId = desafiante.PersonagemId, Aceito = true }
        );

        var inicioDesafiante = await desafiante.Cliente.ReceberAsync<DueloIniciado>(
            PvpMessageIds.DueloIniciado
        );
        var inicioDesafiado = await desafiado.Cliente.ReceberAsync<DueloIniciado>(
            PvpMessageIds.DueloIniciado
        );

        Assert.Equal(desafiante.PersonagemId, inicioDesafiante.DesafianteId);
        Assert.Equal(desafiado.PersonagemId, inicioDesafiante.DesafiadoId);
        Assert.Equal(1.0, inicioDesafiante.CentroX, 1);
        Assert.Equal(
            RegrasPvp.TempoContagemRegressivaDueloSegundos,
            inicioDesafiante.ContagemRegressivaSegundos
        );

        Assert.Equal(desafiante.PersonagemId, inicioDesafiado.DesafianteId);
        Assert.Equal(desafiado.PersonagemId, inicioDesafiado.DesafiadoId);
    }

    [Fact]
    public async Task Duelo_recusado_notifica_o_desafiante()
    {
        using var f = new PvpFixture();
        using var desafiante = await f.EntrarAsync(0, 0);
        using var desafiado = await f.EntrarAsync(2, 0);

        desafiante.Cliente.Enviar(
            PvpMessageIds.SolicitarDuelo,
            new SolicitarDuelo { AlvoPersonagemId = desafiado.PersonagemId }
        );
        await desafiado.Cliente.ReceberAsync<ConviteDeDueloRecebido>(
            PvpMessageIds.ConviteDeDueloRecebido
        );

        desafiado.Cliente.Enviar(
            PvpMessageIds.ResponderConviteDeDuelo,
            new ResponderConviteDeDuelo { DesafianteId = desafiante.PersonagemId, Aceito = false }
        );

        var recusa = await desafiante.Cliente.ReceberAsync<DueloRecusado>(
            PvpMessageIds.DueloRecusado
        );
        Assert.Equal(CodigosErroPvp.RecusadoPeloAlvo, recusa.Motivo);
        Assert.Equal(desafiado.PersonagemId, recusa.AlvoPersonagemId);
    }

    [Fact]
    public async Task Duelo_atingir_zero_de_vida_encerra_com_vitoria_por_dano()
    {
        using var f = new PvpFixture();
        using var desafiante = await f.EntrarAsync(0, 0);
        using var desafiado = await f.EntrarAsync(2, 0);

        desafiante.Cliente.Enviar(
            PvpMessageIds.SolicitarDuelo,
            new SolicitarDuelo { AlvoPersonagemId = desafiado.PersonagemId }
        );
        await desafiado.Cliente.ReceberAsync<ConviteDeDueloRecebido>(
            PvpMessageIds.ConviteDeDueloRecebido
        );

        desafiado.Cliente.Enviar(
            PvpMessageIds.ResponderConviteDeDuelo,
            new ResponderConviteDeDuelo { DesafianteId = desafiante.PersonagemId, Aceito = true }
        );
        await desafiante.Cliente.ReceberAsync<DueloIniciado>(PvpMessageIds.DueloIniciado);
        await desafiado.Cliente.ReceberAsync<DueloIniciado>(PvpMessageIds.DueloIniciado);

        // Avança o relógio para liberar o combate após a contagem de 3s
        f.Relogio.Advance(TimeSpan.FromSeconds(3.5));

        // Aplica dano fatal no desafiado
        var alvoId = AlvosDePvp.ParaAlvoId(desafiado.PersonagemId);
        var aplicou = f.Alvos.TentarAplicarDano(
            alvoId,
            1000,
            desafiante.PersonagemId,
            out var resultado
        );
        Assert.True(aplicou);
        Assert.Equal(0, resultado.VidaRestante);

        var fimDesafiante = await desafiante.Cliente.ReceberAsync<DueloEncerrado>(
            PvpMessageIds.DueloEncerrado
        );
        var fimDesafiado = await desafiado.Cliente.ReceberAsync<DueloEncerrado>(
            PvpMessageIds.DueloEncerrado
        );

        Assert.Equal(desafiante.PersonagemId, fimDesafiante.VencedorId);
        Assert.Equal(desafiado.PersonagemId, fimDesafiante.PerdedorId);
        Assert.Equal(MotivoDueloEncerramento.VitoriaPorDano, fimDesafiante.Motivo);

        Assert.Equal(desafiante.PersonagemId, fimDesafiado.VencedorId);
        Assert.Equal(desafiado.PersonagemId, fimDesafiado.PerdedorId);
    }

    [Fact]
    public async Task Arena_fila_e_inicio_de_partida_3v3()
    {
        using var f = new PvpFixture();
        var jogadores = new Jogador[6];
        for (var i = 0; i < 6; i++)
        {
            jogadores[i] = await f.EntrarAsync(i * 2, 0);
        }

        try
        {
            // Os 5 primeiros entram na fila
            for (var i = 0; i < 5; i++)
            {
                jogadores[i]
                    .Cliente.Enviar(PvpMessageIds.EntrarNaFilaDeArena, new EntrarNaFilaDeArena());
                var status = await jogadores[i]
                    .Cliente.ReceberAsync<StatusFilaDeArena>(PvpMessageIds.StatusFilaDeArena);
                Assert.True(status.NaFila);
            }

            Assert.Equal(5, f.Servico.JogadoresNaFilaArenaCount);

            // O 6º jogador entra: dispara o início da partida de arena 3v3
            jogadores[5]
                .Cliente.Enviar(PvpMessageIds.EntrarNaFilaDeArena, new EntrarNaFilaDeArena());

            for (var i = 0; i < 6; i++)
            {
                var inicio = await jogadores[i]
                    .Cliente.ReceberAsync<PartidaDeArenaIniciada>(
                        PvpMessageIds.PartidaDeArenaIniciada
                    );
                Assert.NotEmpty(inicio.IdPartida);
                Assert.Equal(3, inicio.TimeAzul.Length);
                Assert.Equal(3, inicio.TimeVermelho.Length);
                Assert.Equal(RegrasPvp.TempoPreparoArenaSegundos, inicio.TempoPreparoSegundos);
            }

            Assert.Equal(1, f.Servico.PartidasArenaAtivasCount);
        }
        finally
        {
            foreach (var j in jogadores)
            {
                j.Dispose();
            }
        }
    }

    [Fact]
    public async Task Alternar_bandeira_pvp_notifica_cliente()
    {
        using var f = new PvpFixture();
        using var jogador = await f.EntrarAsync(0, 0);

        jogador.Cliente.Enviar(
            PvpMessageIds.AlternarBandeiraPvp,
            new AlternarBandeiraPvp { Ativar = true }
        );

        var alterada = await jogador.Cliente.ReceberAsync<BandeiraPvpAlterada>(
            PvpMessageIds.BandeiraPvpAlterada
        );
        Assert.Equal(jogador.PersonagemId, alterada.PersonagemId);
        Assert.True(alterada.Ativa);
        Assert.True(f.Servico.TemBandeiraAtiva(jogador.PersonagemId));
    }
}
