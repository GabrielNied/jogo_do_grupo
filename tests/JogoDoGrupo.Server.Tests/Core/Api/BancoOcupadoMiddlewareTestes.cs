using System.Data.Common;
using System.Net;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Core.Api;

/// <summary>
/// O Core responde 503 ServidorOcupado para banco ocupado em qualquer rota, sem a
/// funcionalidade precisar de catch próprio.
/// </summary>
public class BancoOcupadoMiddlewareTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    [Theory]
    [InlineData("sqlite5")]
    [InlineData("sqlite6")]
    [InlineData("40001")]
    public async Task Consulta_com_banco_ocupado_da_503_servidor_ocupado(string tipo)
    {
        var falha = new FalhaComando(tipo);
        using var fabrica = Fabrica(falha);
        using var cliente = await ServidorDeTeste.CriarClienteLogadoEmAsync(fabrica);

        falha.Ativa = true;
        var resposta = await cliente.GetAsync(RotasPersonagem.Personagens);
        falha.Ativa = false;

        await AssertOcupado(resposta);
        Assert.Equal(
            HttpStatusCode.OK,
            (await cliente.GetAsync(RotasPersonagem.Personagens)).StatusCode
        );
    }

    [Fact]
    public async Task Outro_erro_de_banco_continua_500_erro_interno()
    {
        var falha = new FalhaComando("sqlite1");
        using var fabrica = Fabrica(falha);
        using var cliente = await ServidorDeTeste.CriarClienteLogadoEmAsync(fabrica);

        falha.Ativa = true;
        var resposta = await cliente.GetAsync(RotasPersonagem.Personagens);
        falha.Ativa = false;

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta);
        Assert.Equal(CodigosErro.ErroInterno, erro.Codigo);
    }

    [Fact]
    public async Task Recursos_de_combate_com_banco_ocupado_na_transacao_da_503()
    {
        var falha = new FalhaTransacao();
        using var fabrica = servidor.WithWebHostBuilder(builder =>
            builder.ConfigureServices(servicos =>
                servicos.AddDbContext<JogoDbContext>(opcoes => opcoes.AddInterceptors(falha))
            )
        );
        using var cliente = await ServidorDeTeste.CriarClienteLogadoEmAsync(fabrica);
        var criado = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(criado)).Id;

        falha.Ativa = true;
        var resposta = await cliente.GetAsync(RotasCombate.RecursosDe(id));
        falha.Ativa = false;

        await AssertOcupado(resposta);
        Assert.Equal(
            HttpStatusCode.OK,
            (await cliente.GetAsync(RotasCombate.RecursosDe(id))).StatusCode
        );
    }

    private WebApplicationFactory<Program> Fabrica(FalhaComando falha) =>
        servidor.WithWebHostBuilder(builder =>
            builder.ConfigureServices(servicos =>
                servicos.AddDbContext<JogoDbContext>(opcoes => opcoes.AddInterceptors(falha))
            )
        );

    private static async Task AssertOcupado(HttpResponseMessage resposta)
    {
        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        Assert.Equal("application/json", resposta.Content.Headers.ContentType?.MediaType);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta);
        Assert.Equal(CodigosErro.ServidorOcupado, erro.Codigo);
    }

    private static Exception Criar(string tipo) =>
        tipo switch
        {
            "sqlite5" => new SqliteException("ocupado", 5),
            "sqlite6" => new SqliteException("bloqueado", 6),
            "sqlite1" => new SqliteException("outro erro", 1),
            _ => new ErroComSqlState(tipo),
        };

    private sealed class FalhaComando(string tipo) : DbCommandInterceptor
    {
        public bool Ativa;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default
        ) => Ativa ? throw Criar(tipo) : ValueTask.FromResult(result);
    }

    private sealed class FalhaTransacao : DbTransactionInterceptor
    {
        public bool Ativa;

        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default
        ) => Ativa ? throw new SqliteException("ocupado", 5) : ValueTask.FromResult(result);
    }

    private sealed class ErroComSqlState(string estado) : DbException
    {
        public override string SqlState => estado;
    }
}
