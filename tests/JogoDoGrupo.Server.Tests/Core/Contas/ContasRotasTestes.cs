using System.Net;
using System.Text;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.Contas;

namespace JogoDoGrupo.Server.Tests.Core.Contas;

public class ContasRotasTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private const string Senha = "senha-segura";
    private readonly HttpClient _cliente = servidor.CreateClient();

    [Fact]
    public async Task Registrar_e_logar_devolve_token()
    {
        var usuario = ServidorDeTeste.NomeUnico();

        var registro = await Registrar(usuario);
        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);

        var login = await ServidorDeTeste.PostJsonAsync(
            _cliente,
            RotasContas.Login,
            new LoginRequest { Usuario = usuario.ToUpperInvariant(), Senha = Senha }
        );
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var corpo = await ServidorDeTeste.LerJsonAsync<LoginResponse>(login);
        Assert.False(string.IsNullOrEmpty(corpo.Token));
        Assert.True(corpo.ExpiraEmUnix > DateTimeOffset.UtcNow.AddYears(50).ToUnixTimeSeconds());
    }

    [Fact]
    public async Task Registrar_usuario_repetido_sem_diferenciar_maiusculas_da_409()
    {
        var usuario = ServidorDeTeste.NomeUnico();
        await Registrar(usuario);

        var repetido = await Registrar(usuario.ToUpperInvariant());

        await AssertErro(repetido, HttpStatusCode.Conflict, CodigosErro.UsuarioJaExiste);
    }

    [Fact]
    public async Task Login_com_senha_errada_da_401_sem_dizer_o_motivo()
    {
        var usuario = ServidorDeTeste.NomeUnico();
        await Registrar(usuario);

        var login = await ServidorDeTeste.PostJsonAsync(
            _cliente,
            RotasContas.Login,
            new LoginRequest { Usuario = usuario, Senha = "outra-senha" }
        );

        await AssertErro(login, HttpStatusCode.Unauthorized, CodigosErro.CredenciaisInvalidas);
    }

    [Fact]
    public async Task Json_malformado_da_400_requisicao_invalida()
    {
        var resposta = await _cliente.PostAsync(
            RotasContas.Registrar,
            new StringContent("{ isso não é json", Encoding.UTF8, "application/json")
        );

        await AssertErro(resposta, HttpStatusCode.BadRequest, CodigosErro.RequisicaoInvalida);
    }

    private Task<HttpResponseMessage> Registrar(string usuario) =>
        ServidorDeTeste.PostJsonAsync(
            _cliente,
            RotasContas.Registrar,
            new RegistrarContaRequest { Usuario = usuario, Senha = Senha }
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
