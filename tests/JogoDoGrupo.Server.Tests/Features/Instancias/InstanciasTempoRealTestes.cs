using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Instancias;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Instancias;
using JogoDoGrupo.Shared.Features.Mundo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using LayoutDungeon = JogoDoGrupo.Shared.Features.Instancias.LayoutDaMinaDoReflexo;

namespace JogoDoGrupo.Server.Tests.Features.Instancias;

public sealed class InstanciasFixture : IDisposable
{
    private static long _proximoPersonagem = 9000;
    private readonly ServidorDeTeste _base = new();

    public InstanciasFixture()
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
                servicos.RemoveAll<IConsultaDeGrupo>();
                servicos.AddSingleton<IConsultaDeGrupo>(Grupos);

                var existentes = servicos.Last(descritor =>
                    descritor.ServiceType == typeof(IPosicoesDoMundo)
                );
                var posicoesReais = (IPosicoesDoMundo)existentes.ImplementationInstance!;
                Posicoes = new PosicoesDeTeste(posicoesReais);
                servicos.RemoveAll<IPosicoesDoMundo>();
                servicos.AddSingleton<IPosicoesDoMundo>(Posicoes);
                servicos.RemoveAll<IEstadoDeQueda>();
                servicos.AddSingleton<IEstadoDeQueda>(Queda);
            });
        });
        TempoReal = Fabrica.Services.GetRequiredService<ServidorTempoReal>();
        Instancias = Fabrica.Services.GetRequiredService<ServicoDeInstancias>();
    }

    public WebApplicationFactory<Program> Fabrica { get; }
    public FakeTimeProvider Relogio { get; }
    public VerificadorFalso Verificador { get; } = new();
    public ConsultaDeGrupoFalsa Grupos { get; } = new();
    public QuedaDeTeste Queda { get; } = new();
    public PosicoesDeTeste Posicoes { get; private set; } = null!;
    public ServidorTempoReal TempoReal { get; }
    public ServicoDeInstancias Instancias { get; }

    public sealed record Jogador(ClienteTempoRealDeTeste Cliente, long PersonagemId, string Token)
        : IDisposable
    {
        public void Dispose() => Cliente.Dispose();
    }

    public async Task<Jogador> EntrarAsync()
    {
        using var http = await ServidorDeTeste.CriarClienteLogadoEmAsync(Fabrica);
        var token = http.DefaultRequestHeaders.Authorization!.Parameter!;
        var contaId = long.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Subject!);
        var personagemId = Interlocked.Increment(ref _proximoPersonagem);
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
            new PosicaoNoMundo(
                AreasDoMundo.Cidade,
                LayoutDungeon.EntradaX,
                LayoutDungeon.EntradaY,
                LayoutDungeon.EntradaZ,
                0
            )
        );
        return new Jogador(cliente, personagemId, token);
    }

    public async Task<Jogador> ReconectarAsync(Jogador jogador)
    {
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
            TempoReal.Porta,
            jogador.Token,
            jogador.PersonagemId
        );
        Assert.True(resultado.Aceita, resultado.Codigo);
        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        return jogador with { Cliente = cliente };
    }

    public static async Task EsperarAsync(Func<bool> condicao)
    {
        var limite = DateTime.UtcNow + ClienteTempoRealDeTeste.Espera;
        while (!condicao())
        {
            Assert.True(DateTime.UtcNow < limite, "Tempo esgotado esperando condição do servidor.");
            await Task.Delay(20);
        }
    }

    public void Dispose()
    {
        Fabrica.Dispose();
        _base.Dispose();
    }
}

public sealed class ConsultaDeGrupoFalsa : IConsultaDeGrupo
{
    private readonly ConcurrentDictionary<long, GrupoAtual> _porPersonagem = new();

    public void Definir(long grupoId, long liderId, params long[] membros)
    {
        var grupo = new GrupoAtual(grupoId, liderId, Array.AsReadOnly(membros));
        foreach (var membro in membros)
        {
            _porPersonagem[membro] = grupo;
        }
    }

    public GrupoAtual? Consultar(long personagemId) =>
        _porPersonagem.TryGetValue(personagemId, out var grupo) ? grupo : null;
}

public sealed class PosicoesDeTeste(IPosicoesDoMundo origem) : IPosicoesDoMundo
{
    private readonly ConcurrentDictionary<long, PosicaoNoMundo> _sobreposicoes = new();

    public void Definir(long personagemId, PosicaoNoMundo posicao) =>
        _sobreposicoes[personagemId] = posicao;

    public void Remover(long personagemId) => _sobreposicoes.TryRemove(personagemId, out _);

    // A sobreposição só vale enquanto o personagem está na cidade: dentro de uma instância a
    // posição é a real, senão o teste acharia que ele saiu da dungeon.
    public bool TentarObter(long personagemId, out PosicaoNoMundo posicao)
    {
        if (
            origem.TentarObter(personagemId, out var real)
            && real.Area.StartsWith("instancia:", StringComparison.Ordinal)
        )
        {
            posicao = real;
            return true;
        }

        return _sobreposicoes.TryGetValue(personagemId, out posicao)
            || origem.TentarObter(personagemId, out posicao);
    }

    public IReadOnlyList<long> PersonagensNaArea(string area) => origem.PersonagensNaArea(area);
}

public sealed class InstanciasTempoRealTestes
{
    [Fact]
    public async Task Jogador_solo_inicia_corrida_isolada_e_vitoria_o_devolve_a_cidade()
    {
        using var f = new InstanciasFixture();
        using var jogador = await f.EntrarAsync();
        jogador.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());

        var inicio = await jogador.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );
        Assert.Equal(AreasDoMundo.Instancia(inicio.IdInstancia), inicio.Area);
        Assert.True(
            f.Fabrica.Services.GetRequiredService<IPosicoesDoMundo>()
                .TentarObter(jogador.PersonagemId, out var dentro)
        );
        Assert.Equal(inicio.Area, dentro.Area);

        f.Instancias.Encerrar(inicio.IdInstancia, ResultadoDungeon.Vitoria);

        var encerrada = await jogador.Cliente.ReceberAsync<DungeonEncerrada>(
            InstanciasMessageIds.DungeonEncerrada
        );
        Assert.Equal(ResultadoDungeon.Vitoria, encerrada.Resultado);
        Assert.Equal(inicio.IdInstancia, encerrada.IdInstancia);
        Assert.True(
            f.Fabrica.Services.GetRequiredService<IPosicoesDoMundo>()
                .TentarObter(jogador.PersonagemId, out var fora)
        );
        Assert.Equal(AreasDoMundo.Cidade, fora.Area);
        Assert.Equal(0, f.Instancias.CorridasAtivas);
    }

    [Fact]
    public async Task So_o_lider_inicia_membros_proximos_entram_e_os_distantes_podem_entrar_depois()
    {
        using var f = new InstanciasFixture();
        using var lider = await f.EntrarAsync();
        using var membro = await f.EntrarAsync();
        f.Grupos.Definir(45, lider.PersonagemId, lider.PersonagemId, membro.PersonagemId);

        membro.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        var recusa = await membro.Cliente.ReceberAsync<EntradaRecusada>(
            InstanciasMessageIds.EntradaRecusada
        );
        Assert.Equal(CodigosErroInstancias.NaoELider, recusa.Codigo);

        Assert.True(f.Posicoes.TentarObter(membro.PersonagemId, out var posicaoMembro));
        f.Posicoes.Definir(membro.PersonagemId, posicaoMembro with { X = posicaoMembro.X + 11 });
        lider.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        var inicioLider = await lider.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );
        Assert.DoesNotContain(InstanciasMessageIds.DungeonIniciada, membro.Cliente.IdsRecebidos);

        f.Posicoes.Definir(membro.PersonagemId, posicaoMembro);
        membro.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        var inicioMembro = await membro.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );
        Assert.Equal(inicioLider.IdInstancia, inicioMembro.IdInstancia);
        Assert.Equal(inicioLider.Area, inicioMembro.Area);
    }

    [Fact]
    public async Task Grupos_solo_simultaneos_recebem_areas_sem_vazar_presenca()
    {
        using var f = new InstanciasFixture();
        using var a = await f.EntrarAsync();
        using var b = await f.EntrarAsync();
        a.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        b.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());

        var inicioA = await a.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );
        var inicioB = await b.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );

        Assert.NotEqual(inicioA.Area, inicioB.Area);
        var posicoes = f.Fabrica.Services.GetRequiredService<IPosicoesDoMundo>();
        Assert.Equal([a.PersonagemId], posicoes.PersonagensNaArea(inicioA.Area));
        Assert.Equal([b.PersonagemId], posicoes.PersonagensNaArea(inicioB.Area));
    }

    [Fact]
    public async Task Personagem_ou_grupo_ja_associado_a_outra_corrida_e_recusado()
    {
        using var f = new InstanciasFixture();
        using var lider = await f.EntrarAsync();
        using var membro = await f.EntrarAsync();
        using var outroLider = await f.EntrarAsync();
        f.Grupos.Definir(47, lider.PersonagemId, lider.PersonagemId, membro.PersonagemId);
        lider.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        await lider.Cliente.ReceberAsync<DungeonIniciada>(InstanciasMessageIds.DungeonIniciada);
        await membro.Cliente.ReceberAsync<DungeonIniciada>(InstanciasMessageIds.DungeonIniciada);

        f.Grupos.Definir(48, outroLider.PersonagemId, outroLider.PersonagemId, membro.PersonagemId);
        outroLider.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());

        var recusa = await outroLider.Cliente.ReceberAsync<EntradaRecusada>(
            InstanciasMessageIds.EntradaRecusada
        );
        Assert.Equal(CodigosErroInstancias.GrupoEmOutraDungeon, recusa.Codigo);
    }

    [Fact]
    public async Task Posicao_fora_da_cidade_recusa_a_entrada_com_longe_demais()
    {
        using var f = new InstanciasFixture();
        using var jogador = await f.EntrarAsync();
        f.Posicoes.Definir(
            jogador.PersonagemId,
            new PosicaoNoMundo(AreasDoMundo.Instancia("alheia"), 0, 0, 0, 0)
        );

        jogador.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());

        var recusa = await jogador.Cliente.ReceberAsync<EntradaRecusada>(
            InstanciasMessageIds.EntradaRecusada
        );
        Assert.Equal(CodigosErroInstancias.LongeDemais, recusa.Codigo);
    }

    [Fact]
    public async Task Desconexao_e_retorno_antes_de_120_segundos_preservam_a_corrida()
    {
        using var f = new InstanciasFixture();
        using var lider = await f.EntrarAsync();
        var membro = await f.EntrarAsync();
        f.Grupos.Definir(46, lider.PersonagemId, lider.PersonagemId, membro.PersonagemId);
        lider.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        var inicioLider = await lider.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );
        await membro.Cliente.ReceberAsync<DungeonIniciada>(InstanciasMessageIds.DungeonIniciada);

        membro.Cliente.Sair();
        membro.Cliente.Dispose();
        await InstanciasFixture.EsperarAsync(() =>
            !f.TempoReal.TentarObterSessao(membro.PersonagemId, out _)
        );
        f.Relogio.Advance(TimeSpan.FromSeconds(119));

        using var reconectado = await f.ReconectarAsync(membro);
        reconectado.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        var retorno = await reconectado.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );
        Assert.Equal(inicioLider.IdInstancia, retorno.IdInstancia);
        Assert.Equal(inicioLider.Area, retorno.Area);
    }

    [Fact]
    public async Task Corrida_vazia_fecha_apos_cinco_minutos()
    {
        using var f = new InstanciasFixture();
        using var jogador = await f.EntrarAsync();
        jogador.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        var inicio = await jogador.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );

        jogador.Cliente.Sair();
        await InstanciasFixture.EsperarAsync(() =>
            !f.TempoReal.TentarObterSessao(jogador.PersonagemId, out _)
        );
        f.Instancias.Varrer();
        f.Relogio.Advance(ServicoDeInstancias.PrazoVazia);
        f.Instancias.Varrer();

        Assert.Equal(0, f.Instancias.CorridasAtivas);
        Assert.Empty(
            f.Fabrica.Services.GetRequiredService<IPosicoesDoMundo>()
                .PersonagensNaArea(AreasDoMundo.Instancia(inicio.IdInstancia))
        );
    }

    [Fact]
    public async Task Vaga_desconectada_expira_apos_120_segundos()
    {
        using var f = new InstanciasFixture();
        using var lider = await f.EntrarAsync();
        using var membro = await f.EntrarAsync();
        f.Grupos.Definir(49, lider.PersonagemId, lider.PersonagemId, membro.PersonagemId);
        lider.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        await lider.Cliente.ReceberAsync<DungeonIniciada>(InstanciasMessageIds.DungeonIniciada);
        await membro.Cliente.ReceberAsync<DungeonIniciada>(InstanciasMessageIds.DungeonIniciada);

        membro.Cliente.Sair();
        membro.Cliente.Dispose();
        await InstanciasFixture.EsperarAsync(() =>
            !f.TempoReal.TentarObterSessao(membro.PersonagemId, out _)
        );
        f.Relogio.Advance(ServicoDeInstancias.PrazoDeReconexao);
        f.Instancias.Varrer();

        var reconectado = await f.ReconectarAsync(membro);
        using (reconectado)
        {
            reconectado.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
            var recusa = await reconectado.Cliente.ReceberAsync<EntradaRecusada>(
                InstanciasMessageIds.EntradaRecusada
            );
            Assert.Equal(CodigosErroInstancias.GrupoEmOutraDungeon, recusa.Codigo);
        }
    }
}

public sealed class QuedaDeTeste : IEstadoDeQueda
{
    private readonly ConcurrentDictionary<long, bool> _caidos = new();

    public void Derrubar(long personagemId) => _caidos[personagemId] = true;

    public bool EstaCaido(long personagemId) => _caidos.ContainsKey(personagemId);
}
