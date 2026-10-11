using System.Net;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.Contas;

namespace JogoDoGrupo.Server.Tests.Core.Contas;

public class ContasRegistroLoginTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private const string Senha = "senha-segura";
    private readonly HttpClient _cliente = servidor.CreateClient();

    [Fact]
    public async Task Registrar_valido_devolve_201_sem_corpo()
    {
        var resposta = await Registrar(ServidorDeTeste.NomeUnico(), Senha);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal("", await resposta.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("abcdefghijklmnopqrstu")]
    [InlineData("ana-maria")]
    [InlineData("ana maria")]
    [InlineData("joão")]
    [InlineData(null)]
    public async Task Registrar_usuario_invalido_da_400(string? usuario)
    {
        var resposta = await Registrar(usuario!, Senha);

        await AssertErro(resposta, HttpStatusCode.BadRequest, CodigosErro.UsuarioInvalido);
    }

    [Fact]
    public async Task Registrar_usuario_com_20_caracteres_da_201()
    {
        var usuario = ServidorDeTeste.NomeUnico().PadRight(RegrasConta.UsuarioMaximo, '_');

        var resposta = await Registrar(usuario, Senha);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    [Fact]
    public async Task Registrar_usuario_com_3_caracteres_da_201_ou_409_se_ja_usado()
    {
        // Só há 3 caracteres: sorteia para não colidir com outros testes da mesma classe.
        var usuario = ServidorDeTeste.NomeUnico()[..RegrasConta.UsuarioMinimo];

        var resposta = await Registrar(usuario, Senha);

        Assert.Contains(
            resposta.StatusCode,
            new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }
        );
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData(null)]
    public async Task Registrar_senha_invalida_da_400(string? senha)
    {
        var resposta = await Registrar(ServidorDeTeste.NomeUnico(), senha!);

        await AssertErro(resposta, HttpStatusCode.BadRequest, CodigosErro.SenhaInvalida);
    }

    [Fact]
    public async Task Registrar_senha_acima_do_maximo_da_400()
    {
        var resposta = await Registrar(ServidorDeTeste.NomeUnico(), new string('a', 65));

        await AssertErro(resposta, HttpStatusCode.BadRequest, CodigosErro.SenhaInvalida);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(64)]
    public async Task Registrar_senha_nos_limites_da_201(int tamanho)
    {
        var resposta = await Registrar(ServidorDeTeste.NomeUnico(), new string('a', tamanho));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    [Fact]
    public async Task Registrar_com_outra_capitalizacao_da_409()
    {
        var usuario = ServidorDeTeste.NomeUnico();
        await Registrar(usuario, Senha);

        var resposta = await Registrar(usuario.ToLowerInvariant(), Senha);

        await AssertErro(resposta, HttpStatusCode.Conflict, CodigosErro.UsuarioJaExiste);
    }

    [Fact]
    public async Task Registrar_o_mesmo_usuario_simultaneamente_cria_so_uma_conta()
    {
        var usuario = ServidorDeTeste.NomeUnico();

        var respostas = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => Task.Run(() => Registrar(usuario, Senha)))
        );

        Assert.Equal(1, respostas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(9, respostas.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Login_com_outra_capitalizacao_da_200()
    {
        var usuario = ServidorDeTeste.NomeUnico();
        await Registrar(usuario, Senha);

        var login = await Logar(usuario.ToLowerInvariant(), Senha);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Login_de_usuario_inexistente_da_401()
    {
        var login = await Logar(ServidorDeTeste.NomeUnico(), Senha);

        await AssertErro(login, HttpStatusCode.Unauthorized, CodigosErro.CredenciaisInvalidas);
    }

    [Fact]
    public async Task Login_com_senha_acima_do_maximo_da_401()
    {
        var usuario = ServidorDeTeste.NomeUnico();
        await Registrar(usuario, Senha);

        var login = await Logar(usuario, new string('a', 65));

        await AssertErro(login, HttpStatusCode.Unauthorized, CodigosErro.CredenciaisInvalidas);
    }

    private Task<HttpResponseMessage> Registrar(string usuario, string senha) =>
        ServidorDeTeste.PostJsonAsync(
            _cliente,
            RotasContas.Registrar,
            new RegistrarContaRequest { Usuario = usuario, Senha = senha }
        );

    private Task<HttpResponseMessage> Logar(string usuario, string senha) =>
        ServidorDeTeste.PostJsonAsync(
            _cliente,
            RotasContas.Login,
            new LoginRequest { Usuario = usuario, Senha = senha }
        );

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
}
