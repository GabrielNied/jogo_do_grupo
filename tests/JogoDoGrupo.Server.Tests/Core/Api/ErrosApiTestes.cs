using System.Net;
using System.Net.Http.Headers;
using System.Text;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.Contas;
using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Server.Tests.Core.Api;

public class ErrosApiTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private readonly HttpClient _cliente = servidor.CreateClient();

    [Fact]
    public async Task Corpo_ausente_da_400_requisicao_invalida()
    {
        var resposta = await _cliente.PostAsync(RotasContas.Registrar, null);

        await AssertErro(resposta, HttpStatusCode.BadRequest, CodigosErro.RequisicaoInvalida);
    }

    [Fact]
    public async Task Json_malformado_da_400_requisicao_invalida()
    {
        var resposta = await _cliente.PostAsync(
            RotasContas.Login,
            new StringContent("{ \"Usuario\": ", Encoding.UTF8, "application/json")
        );

        await AssertErro(resposta, HttpStatusCode.BadRequest, CodigosErro.RequisicaoInvalida);
    }

    [Fact]
    public async Task Content_type_text_plain_da_400_requisicao_invalida()
    {
        var resposta = await _cliente.PostAsync(
            RotasContas.Registrar,
            new StringContent(
                "{\"Usuario\":\"abc\",\"Senha\":\"senha-segura\"}",
                Encoding.UTF8,
                "text/plain"
            )
        );

        await AssertErro(resposta, HttpStatusCode.BadRequest, CodigosErro.RequisicaoInvalida);
    }

    [Fact]
    public async Task Rota_protegida_sem_token_da_401_com_corpo()
    {
        var resposta = await _cliente.GetAsync(RotasPersonagem.Personagens);

        await AssertErro(resposta, HttpStatusCode.Unauthorized, CodigosErro.NaoAutenticado);
    }

    [Fact]
    public async Task Rota_protegida_com_token_invalido_da_401_com_corpo()
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, RotasPersonagem.Personagens);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "isso.nao.e-token"
        );

        var resposta = await _cliente.SendAsync(requisicao);

        await AssertErro(resposta, HttpStatusCode.Unauthorized, CodigosErro.NaoAutenticado);
    }

    [Fact]
    public async Task Rota_protegida_com_token_valido_nao_da_401()
    {
        var logado = await servidor.CriarClienteLogadoAsync();

        var resposta = await logado.GetAsync(RotasPersonagem.Personagens);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
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
}
