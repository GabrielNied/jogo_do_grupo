using System.Net;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Talentos;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Talentos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TipoDeEfeito = JogoDoGrupo.Shared.Features.Talentos.TipoDeEfeito;

namespace JogoDoGrupo.Server.Tests.Features.Talentos;

/// <summary>Nó de secundário comprado entra na pipeline do Combate com a fonte Árvore.</summary>
public class SecundariosDaArvoreTestes : IClassFixture<ServidorDeTeste>, IDisposable
{
    private const string NoCritico = "critico-1";
    private const double ChancePorRank = 0.02;
    private const double VelocidadePorRank = 0.1;

    private readonly WebApplicationFactory<Program> _fabrica;

    public SecundariosDaArvoreTestes(ServidorDeTeste real)
    {
        var config = ArvoreDeTeste.Criar();
        config.Nos = [.. config.Nos, NoSecundario()];
        TalentosModule.ValidarOuFalhar(config);
        _fabrica = real.WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton(config)));
    }

    public void Dispose() => _fabrica.Dispose();

    private static NoDaArvore NoSecundario() =>
        new()
        {
            Id = NoCritico,
            Nome = "Olho clínico",
            Caminhos = [AtributoPrincipal.Agilidade],
            RankMaximo = 2,
            CustoPorRank = [1, 1],
            Efeitos =
            [
                new EfeitoDeNo
                {
                    Tipo = TipoDeEfeito.AtributoSecundario,
                    Chave = nameof(AtributoSecundario.ChanceCritica),
                    ValorPorRank = ChancePorRank,
                },
                new EfeitoDeNo
                {
                    Tipo = TipoDeEfeito.AtributoSecundario,
                    Chave = nameof(AtributoSecundario.VelocidadeAtaqueFisico),
                    ValorPorRank = VelocidadePorRank,
                    Multiplica = true,
                },
            ],
        };

    [Fact]
    public async Task No_de_secundario_comprado_aparece_na_pipeline_com_a_fonte_arvore()
    {
        var cliente = await ServidorDeTeste.CriarClienteLogadoEmAsync(_fabrica);
        var criado = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(criado)).Id;
        await using (var escopo = _fabrica.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var personagem = await db.Set<JogoDoGrupo.Server.Features.Personagem.Personagem>()
                .SingleAsync(p => p.Id == id);
            personagem.PontosLivres = 5;
            await db.SaveChangesAsync();
        }

        var antes = await Recursos(cliente, id);
        foreach (var rank in new[] { 1, 2 })
        {
            var compra = await ServidorDeTeste.PostJsonAsync(
                cliente,
                RotasTalentos.ComprarDe(id),
                new ComprarNoRequest
                {
                    NoId = NoCritico,
                    RankAlvo = rank,
                    VersaoConfiguracao = ArvoreDeTeste.Versao,
                }
            );
            Assert.Equal(HttpStatusCode.OK, compra.StatusCode);
        }

        var depois = await Recursos(cliente, id);

        var chance = depois.Secundarios.Single(s => s.Atributo == AtributoSecundario.ChanceCritica);
        var parcela = Assert.Single(chance.Parcelas, p => p.Fonte == FonteDeModificador.Arvore);
        Assert.Equal(OperacaoDeModificador.Soma, parcela.Operacao);
        Assert.Equal(2 * ChancePorRank, parcela.Valor, 9);
        Assert.Contains("Olho clínico", parcela.Origem);
        var antesChance = antes.Secundarios.Single(s =>
            s.Atributo == AtributoSecundario.ChanceCritica
        );
        Assert.Equal(antesChance.Valor + 2 * ChancePorRank, chance.Valor, 9);
        var velocidade = depois.Secundarios.Single(s =>
            s.Atributo == AtributoSecundario.VelocidadeAtaqueFisico
        );
        Assert.Contains(
            velocidade.Parcelas,
            p =>
                p.Fonte == FonteDeModificador.Arvore
                && p.Operacao == OperacaoDeModificador.Multiplica
        );
        var velocidadeAntes = antes
            .Secundarios.Single(s => s.Atributo == AtributoSecundario.VelocidadeAtaqueFisico)
            .Valor;
        Assert.True(velocidade.Valor > velocidadeAntes);
    }

    [Fact]
    public void Secundario_com_chave_que_nao_existe_nao_sobe()
    {
        var config = ArvoreDeTeste.Criar();
        var no = NoSecundario();
        no.Efeitos[0].Chave = "Inexistente";
        config.Nos = [.. config.Nos, no];

        var erro = Assert.Throws<InvalidOperationException>(() =>
            TalentosModule.ValidarOuFalhar(config)
        );

        Assert.Contains("Inexistente", erro.Message);
    }

    private static async Task<RecursosResponse> Recursos(HttpClient cliente, long id)
    {
        var resposta = await cliente.GetAsync(RotasCombate.RecursosDe(id));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await ServidorDeTeste.LerJsonAsync<RecursosResponse>(resposta);
    }
}
