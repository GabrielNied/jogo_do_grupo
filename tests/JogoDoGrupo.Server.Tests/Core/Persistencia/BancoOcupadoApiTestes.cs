using System.Data.Common;
using System.Net;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Core.Persistencia;

public class BancoOcupadoApiTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    [Theory]
    [InlineData(false, false, 5)]
    [InlineData(false, false, 6)]
    [InlineData(false, true, 5)]
    [InlineData(false, true, 6)]
    [InlineData(true, false, 5)]
    [InlineData(true, false, 6)]
    [InlineData(true, true, 5)]
    [InlineData(true, true, 6)]
    [InlineData(false, false, 1)]
    [InlineData(true, true, 1)]
    public async Task Falha_na_transacao_nao_grava_e_permite_nova_tentativa(
        bool bau,
        bool noCommit,
        int codigo
    )
    {
        var falha = new FalhaTransacao(noCommit, codigo);
        using var fabrica = servidor.WithWebHostBuilder(builder =>
            builder.ConfigureServices(servicos =>
                servicos.AddDbContext<JogoDbContext>(opcoes => opcoes.AddInterceptors(falha))
            )
        );
        using var cliente = await ServidorDeTeste.CriarClienteLogadoEmAsync(fabrica);
        var nome = ServidorDeTeste.NomeUnico();
        var requisicao = new CriarPersonagemRequest { Nome = nome, Raca = Raca.Valdren };
        long personagemId = 0;
        if (bau)
        {
            var criado = await ServidorDeTeste.PostJsonAsync(
                cliente,
                RotasPersonagem.Personagens,
                requisicao
            );
            Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
            personagemId = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(criado)).Id;
        }

        Task<HttpResponseMessage> Enviar() =>
            bau
                ? cliente.PostAsync(RotasItens.BauDe(personagemId), null)
                : ServidorDeTeste.PostJsonAsync(cliente, RotasPersonagem.Personagens, requisicao);

        falha.Ativa = true;
        var resposta = await Enviar();
        falha.Ativa = false;

        Assert.Equal(
            codigo is 5 or 6
                ? HttpStatusCode.ServiceUnavailable
                : HttpStatusCode.InternalServerError,
            resposta.StatusCode
        );
        Assert.Equal("application/json", resposta.Content.Headers.ContentType?.MediaType);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta);
        Assert.Equal(
            codigo is 5 or 6 ? CodigosErro.ServidorOcupado : CodigosErro.ErroInterno,
            erro.Codigo
        );

        if (bau)
        {
            var consulta = await cliente.GetAsync(RotasItens.InventarioDe(personagemId));
            Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
            Assert.Empty(
                (await ServidorDeTeste.LerJsonAsync<InventarioResponse>(consulta)).Entradas
            );
        }
        else
        {
            var consulta = await cliente.GetAsync(RotasPersonagem.Personagens);
            Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
            Assert.Empty(
                (await ServidorDeTeste.LerJsonAsync<ListaPersonagensResponse>(consulta)).Personagens
            );
        }

        Assert.Equal(bau ? HttpStatusCode.OK : HttpStatusCode.Created, (await Enviar()).StatusCode);
    }

    private sealed class FalhaTransacao(bool noCommit, int codigo) : DbTransactionInterceptor
    {
        public bool Ativa;

        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default
        )
        {
            if (Ativa && !noCommit)
                throw new SqliteException("falha simulada", codigo);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(
            DbTransaction transaction,
            TransactionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default
        )
        {
            if (Ativa && noCommit)
                throw new SqliteException("falha simulada", codigo);
            return ValueTask.FromResult(result);
        }
    }
}
