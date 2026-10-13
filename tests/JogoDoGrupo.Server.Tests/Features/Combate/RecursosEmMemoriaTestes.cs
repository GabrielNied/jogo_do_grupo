using System.Data.Common;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

/// <summary>
/// Os recursos ficam em memória: o golpe leve não toca no banco e a stamina do pesado é cobrado
/// uma vez só, mesmo com pedidos simultâneos.
/// </summary>
public sealed class RecursosEmMemoriaTestes : IClassFixture<ServidorDeTeste>, IDisposable
{
    private readonly ContadorDeBanco _banco = new();
    private readonly RelogioFalso _relogio = new();
    private readonly WebApplicationFactory<Program> _fabrica;

    public RecursosEmMemoriaTestes(ServidorDeTeste servidor)
    {
        _fabrica = servidor.WithWebHostBuilder(b =>
            b.ConfigureServices(s =>
            {
                s.AddSingleton<TimeProvider>(_relogio);
                s.AddDbContext<JogoDbContext>(o => o.AddInterceptors(_banco));
            })
        );
    }

    public void Dispose() => _fabrica.Dispose();

    private RecursosEmMemoria Recursos => _fabrica.Services.GetRequiredService<RecursosEmMemoria>();

    private async Task<long> NovoPersonagemAsync()
    {
        using var http = await ServidorDeTeste.CriarClienteLogadoEmAsync(_fabrica);
        var resposta = await ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        return (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;
    }

    [Fact]
    public async Task Golpe_leve_nao_abre_transacao_nem_consulta_o_banco()
    {
        var id = await NovoPersonagemAsync();
        await Recursos.TentarGastarStaminaAsync(id, 0, CancellationToken.None); // Primeira vez: lê.
        _banco.Zerar();

        for (var i = 0; i < 20; i++)
        {
            var inicio = await Recursos.TentarGastarStaminaAsync(id, 0, CancellationToken.None);
            Assert.Null(inicio.Recusa);
            Assert.Null(inicio.Recursos);
        }

        Assert.Equal(0, _banco.Transacoes);
        Assert.Equal(0, _banco.Comandos);
    }

    [Fact]
    public async Task Pesado_nao_abre_transacao_depois_de_carregado()
    {
        var id = await NovoPersonagemAsync();
        await Recursos.TentarGastarStaminaAsync(id, 0, CancellationToken.None);
        _banco.Zerar();

        var inicio = await Recursos.TentarGastarStaminaAsync(
            id,
            RegrasAtaque.CustoDeStaminaPesado,
            CancellationToken.None
        );

        Assert.Null(inicio.Recusa);
        Assert.Equal(
            RegrasRecursos.StaminaBase - RegrasAtaque.CustoDeStaminaPesado,
            inicio.Recursos!.StaminaAtual
        );
        Assert.Equal(0, _banco.Transacoes);
        Assert.Equal(0, _banco.Comandos);
    }

    [Fact]
    public async Task Stamina_e_cobrado_uma_vez_so_sob_concorrencia()
    {
        var id = await NovoPersonagemAsync();
        var custo = RegrasAtaque.CustoDeStaminaPesado;
        await Recursos.AtualizarAsync(id, (r, _) => r.Stamina = custo, CancellationToken.None);

        var resultados = await Task.WhenAll(
            Enumerable
                .Range(0, 30)
                .Select(_ =>
                    Task.Run(() =>
                        Recursos.TentarGastarStaminaAsync(id, custo, CancellationToken.None)
                    )
                )
        );

        Assert.Single(resultados, r => r.Recusa == null);
        Assert.All(
            resultados.Where(r => r.Recusa != null),
            r => Assert.Equal(CodigosErroCombate.StaminaInsuficiente, r.Recusa)
        );
    }

    [Fact]
    public async Task Gravar_leva_o_que_mudou_para_o_banco_e_nao_repete_sem_mudanca()
    {
        var id = await NovoPersonagemAsync();
        await Recursos.TirarVidaAsync(id, 30, CancellationToken.None);

        await Recursos.GravarAsync(id);
        _banco.Zerar();
        await Recursos.GravarAsync(id); // Nada mudou desde a última gravação.

        Assert.Equal(0, _banco.Comandos);
        using var escopo = _fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var linha = await db.Set<RecursosPersonagem>()
            .AsNoTracking()
            .SingleAsync(r => r.PersonagemId == id);
        Assert.Equal(RegrasRecursos.VidaMaxima(0) - 30, linha.Vida);
    }

    [Fact]
    public async Task Atributos_guardados_vencem_depois_de_um_tempo()
    {
        var id = await NovoPersonagemAsync();
        await Recursos.AtributosAsync(id, CancellationToken.None);
        _banco.Zerar();

        await Recursos.AtributosAsync(id, CancellationToken.None);
        Assert.Equal(0, _banco.Comandos); // Ainda valem: nada foi lido.

        _relogio.Avancar(ConfigRecursos.ValidadeDosAtributosSegundos + 0.1);
        await Recursos.AtributosAsync(id, CancellationToken.None);
        Assert.True(_banco.Comandos > 0); // Venceram: leu Itens e Personagem de novo.
    }

    private sealed class ContadorDeBanco : DbCommandInterceptor, IDbTransactionInterceptor
    {
        private int _comandos;
        private int _transacoes;

        public int Comandos => Volatile.Read(ref _comandos);
        public int Transacoes => Volatile.Read(ref _transacoes);

        public void Zerar()
        {
            Volatile.Write(ref _comandos, 0);
            Volatile.Write(ref _transacoes, 0);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default
        )
        {
            Interlocked.Increment(ref _comandos);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default
        )
        {
            Interlocked.Increment(ref _comandos);
            return ValueTask.FromResult(result);
        }

        public ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default
        )
        {
            Interlocked.Increment(ref _transacoes);
            return ValueTask.FromResult(result);
        }
    }
}
