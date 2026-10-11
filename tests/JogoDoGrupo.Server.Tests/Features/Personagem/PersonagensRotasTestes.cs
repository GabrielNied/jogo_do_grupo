using System.Net;
using System.Net.Http.Json;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Server.Tests.Features.Personagem;

public class PersonagensRotasTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private static Task<HttpResponseMessage> Criar(
        HttpClient cliente,
        string nome,
        Raca raca = Raca.Humano
    ) =>
        ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = nome, Raca = raca }
        );

    private static async Task<PersonagemResumo[]> Listar(HttpClient cliente)
    {
        var resposta = await cliente.GetAsync(RotasPersonagem.Personagens);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await ServidorDeTeste.LerJsonAsync<ListaPersonagensResponse>(resposta)).Personagens;
    }

    private static async Task AssertErro(
        HttpResponseMessage resposta,
        HttpStatusCode status,
        string codigo
    )
    {
        Assert.Equal(status, resposta.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta);
        Assert.Equal(codigo, erro.Codigo);
    }

    [Fact]
    public async Task Listar_conta_nova_devolve_lista_vazia()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();

        var resposta = await cliente.GetAsync(RotasPersonagem.Personagens);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("{\"Personagens\":[]}", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Listar_e_criar_sem_token_da_401()
    {
        var anonimo = servidor.CreateClient();

        await AssertErro(
            await anonimo.GetAsync(RotasPersonagem.Personagens),
            HttpStatusCode.Unauthorized,
            CodigosErro.NaoAutenticado
        );
        await AssertErro(
            await Criar(anonimo, ServidorDeTeste.NomeUnico()),
            HttpStatusCode.Unauthorized,
            CodigosErro.NaoAutenticado
        );
    }

    [Fact]
    public async Task Criar_valido_devolve_201_com_nome_como_digitado()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var nome = "Zz" + ServidorDeTeste.NomeUnico("A");

        var resposta = await Criar(cliente, nome);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var criado = await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta);
        Assert.True(criado.Id > 0);
        Assert.Equal(nome, criado.Nome);
        Assert.Equal(Raca.Humano, criado.Raca);
        Assert.Equal(1, criado.Nivel);
    }

    [Fact]
    public async Task Listar_devolve_so_os_da_propria_conta_em_ordem_de_id()
    {
        var a = await servidor.CriarClienteLogadoAsync();
        var b = await servidor.CriarClienteLogadoAsync();
        var nomesA = new[]
        {
            ServidorDeTeste.NomeUnico("Z"),
            ServidorDeTeste.NomeUnico("A"),
            ServidorDeTeste.NomeUnico("M"),
        };
        var nomeB = ServidorDeTeste.NomeUnico("B");
        await Criar(a, nomesA[0]);
        await Criar(b, nomeB);
        await Criar(a, nomesA[1]);
        await Criar(a, nomesA[2]);

        var listaA = await Listar(a);
        var listaB = await Listar(b);

        Assert.Equal(nomesA, listaA.Select(p => p.Nome));
        Assert.Equal(listaA.Select(p => p.Id).Order(), listaA.Select(p => p.Id));
        Assert.Equal(new[] { nomeB }, listaB.Select(p => p.Nome));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("abcdefghijklmnopq")]
    [InlineData("abc def")]
    [InlineData("abc123")]
    [InlineData("abc_")]
    [InlineData("João")]
    [InlineData("  abc")]
    public async Task Criar_com_nome_invalido_da_400(string nome)
    {
        var cliente = await servidor.CriarClienteLogadoAsync();

        await AssertErro(
            await Criar(cliente, nome),
            HttpStatusCode.BadRequest,
            CodigosErroPersonagem.NomeInvalido
        );
        Assert.Empty(await Listar(cliente));
    }

    [Fact]
    public async Task Criar_sem_nome_no_corpo_da_400_nome_invalido()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();

        var resposta = await cliente.PostAsJsonAsync(RotasPersonagem.Personagens, new { Raca = 1 });

        await AssertErro(resposta, HttpStatusCode.BadRequest, CodigosErroPersonagem.NomeInvalido);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    [InlineData(-1)]
    public async Task Criar_com_raca_invalida_da_400(int raca)
    {
        var cliente = await servidor.CriarClienteLogadoAsync();

        var resposta = await cliente.PostAsJsonAsync(
            RotasPersonagem.Personagens,
            new { Nome = ServidorDeTeste.NomeUnico(), Raca = raca }
        );

        await AssertErro(resposta, HttpStatusCode.BadRequest, CodigosErroPersonagem.RacaInvalida);
    }

    [Fact]
    public async Task Criar_sem_raca_no_corpo_da_400_raca_invalida()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();

        var resposta = await cliente.PostAsJsonAsync(
            RotasPersonagem.Personagens,
            new { Nome = ServidorDeTeste.NomeUnico() }
        );

        await AssertErro(resposta, HttpStatusCode.BadRequest, CodigosErroPersonagem.RacaInvalida);
    }

    [Fact]
    public async Task Criar_nome_repetido_em_outra_capitalizacao_da_409()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var nome = ServidorDeTeste.NomeUnico("A");
        await Criar(cliente, nome);

        await AssertErro(
            await Criar(cliente, nome.ToUpperInvariant()),
            HttpStatusCode.Conflict,
            CodigosErroPersonagem.NomeJaExiste
        );
        Assert.Single(await Listar(cliente));
    }

    [Fact]
    public async Task Criar_nome_que_outra_conta_usa_da_409()
    {
        var a = await servidor.CriarClienteLogadoAsync();
        var b = await servidor.CriarClienteLogadoAsync();
        var nome = ServidorDeTeste.NomeUnico("A");
        await Criar(a, nome);

        await AssertErro(
            await Criar(b, nome.ToLowerInvariant()),
            HttpStatusCode.Conflict,
            CodigosErroPersonagem.NomeJaExiste
        );
        Assert.Empty(await Listar(b));
    }

    [Fact]
    public async Task Criar_alem_do_limite_da_409()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        for (var i = 0; i < RegrasPersonagem.MaximoPorConta; i++)
        {
            Assert.Equal(
                HttpStatusCode.Created,
                (await Criar(cliente, ServidorDeTeste.NomeUnico())).StatusCode
            );
        }

        await AssertErro(
            await Criar(cliente, ServidorDeTeste.NomeUnico()),
            HttpStatusCode.Conflict,
            CodigosErroPersonagem.LimitePersonagens
        );
        Assert.Equal(RegrasPersonagem.MaximoPorConta, (await Listar(cliente)).Length);
    }

    [Fact]
    public async Task Nome_invalido_vem_antes_de_raca_invalida()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();

        var resposta = await cliente.PostAsJsonAsync(
            RotasPersonagem.Personagens,
            new { Nome = "a", Raca = 99 }
        );

        await AssertErro(resposta, HttpStatusCode.BadRequest, CodigosErroPersonagem.NomeInvalido);
    }

    [Fact]
    public async Task Limite_vem_antes_de_nome_repetido()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var repetido = ServidorDeTeste.NomeUnico();
        await Criar(cliente, repetido);
        for (var i = 1; i < RegrasPersonagem.MaximoPorConta; i++)
        {
            await Criar(cliente, ServidorDeTeste.NomeUnico());
        }

        await AssertErro(
            await Criar(cliente, repetido),
            HttpStatusCode.Conflict,
            CodigosErroPersonagem.LimitePersonagens
        );
    }

    [Fact]
    public async Task Dez_criacoes_simultaneas_na_mesma_conta_criam_exatamente_o_limite()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();

        var respostas = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => Criar(cliente, ServidorDeTeste.NomeUnico()))
        );

        Assert.Equal(
            RegrasPersonagem.MaximoPorConta,
            respostas.Count(r => r.StatusCode == HttpStatusCode.Created)
        );
        Assert.Equal(
            10 - RegrasPersonagem.MaximoPorConta,
            respostas.Count(r => r.StatusCode == HttpStatusCode.Conflict)
        );
        Assert.Equal(RegrasPersonagem.MaximoPorConta, (await Listar(cliente)).Length);
    }

    [Fact]
    public async Task Mesmo_nome_simultaneo_por_contas_diferentes_cria_so_um()
    {
        var a = await servidor.CriarClienteLogadoAsync();
        var b = await servidor.CriarClienteLogadoAsync();
        var nome = ServidorDeTeste.NomeUnico("A");

        var respostas = await Task.WhenAll(Criar(a, nome), Criar(b, nome.ToUpperInvariant()));

        Assert.Equal(1, respostas.Count(r => r.StatusCode == HttpStatusCode.Created));
        var recusada = respostas.Single(r => r.StatusCode != HttpStatusCode.Created);
        await AssertErro(recusada, HttpStatusCode.Conflict, CodigosErroPersonagem.NomeJaExiste);
        Assert.Equal(1, (await Listar(a)).Length + (await Listar(b)).Length);
    }
}
