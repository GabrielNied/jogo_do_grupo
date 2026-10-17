using System.Net;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JogoDoGrupo.Server.Tests.Core;

public class ServidorProducaoTestes
{
    private const string ChaveDev =
        "SO-PARA-DESENVOLVIMENTO-nao-use-em-producao-troque-por-Jwt__Chave";

    [Fact]
    public async Task Saude_com_banco_conectado_responde_200_ok()
    {
        using var servidor = new ServidorDeTeste();
        using var cliente = servidor.CreateClient();

        var resposta = await cliente.GetAsync("/saude");

        resposta.EnsureSuccessStatusCode();
        var saude = await ServidorDeTeste.LerJsonAsync<EstadoDaSaude>(resposta);
        Assert.Equal("ok", saude.Estado);
        Assert.Equal(EstadoDaSaude.VersaoAtual, saude.Versao);
        Assert.Equal("conectado", saude.Banco);
    }

    [Fact]
    public async Task Saude_com_banco_indisponivel_responde_503_service_unavailable()
    {
        var caminhoInvalido = Path.Combine(
            Path.GetTempPath(),
            $"pasta-inexistente-{Guid.NewGuid():N}",
            "banco.db"
        );
        using var servidor = new ServidorDeTeste().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Banco:MigrarAoIniciar", "false");
            builder.UseSetting(
                "ConnectionStrings:Jogo",
                $"Data Source={caminhoInvalido};Mode=ReadOnly"
            );
        });
        using var cliente = servidor.CreateClient();

        var resposta = await cliente.GetAsync("/saude");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        var saude = await ServidorDeTeste.LerJsonAsync<EstadoDaSaude>(resposta);
        Assert.Equal("erro", saude.Estado);
        Assert.Equal(EstadoDaSaude.VersaoAtual, saude.Versao);
        Assert.Equal("indisponivel", saude.Banco);
    }

    [Fact]
    public async Task Cabecalhos_x_forwarded_sao_aceitos_pelo_servidor()
    {
        using var servidor = new ServidorDeTeste();
        using var cliente = servidor.CreateClient();

        var requisicao = new HttpRequestMessage(HttpMethod.Get, "/saude");
        requisicao.Headers.Add("X-Forwarded-For", "203.0.113.195");
        requisicao.Headers.Add("X-Forwarded-Proto", "https");

        var resposta = await cliente.SendAsync(requisicao);

        resposta.EnsureSuccessStatusCode();
        var saude = await ServidorDeTeste.LerJsonAsync<EstadoDaSaude>(resposta);
        Assert.Equal("ok", saude.Estado);
    }

    [Fact]
    public void Producao_sem_chave_jwt_falha_imediatamente_com_mensagem_descritiva()
    {
        using var fabrica = CriarFabrica("Production", ("Jwt:Chave", ""));

        var erro = Assert.ThrowsAny<Exception>(() => fabrica.CreateClient());
        AssertMensagem(erro, "Jwt__Chave");
    }

    [Fact]
    public void Producao_com_chave_de_desenvolvimento_falha_imediatamente()
    {
        using var fabrica = CriarFabrica("Production", ("Jwt:Chave", ChaveDev));

        var erro = Assert.ThrowsAny<Exception>(() => fabrica.CreateClient());
        AssertMensagem(erro, "desenvolvimento");
    }

    [Fact]
    public void Producao_com_chave_valida_sobe_com_sucesso()
    {
        using var fabrica = CriarFabrica("Production", ("Jwt:Chave", new string('k', 40)));

        using var cliente = fabrica.CreateClient();
        Assert.NotNull(cliente);
    }

    [Fact]
    public void Provedor_postgres_sem_string_de_conexao_falha_imediatamente()
    {
        using var fabrica = CriarFabrica(
            "Production",
            ("Jwt:Chave", new string('k', 40)),
            ("Banco:Provedor", "Postgres"),
            ("ConnectionStrings:Jogo", ""),
            ("ConnectionStrings:Postgres", ""),
            ("POSTGRES_CONEXAO", "")
        );

        var erro = Assert.ThrowsAny<Exception>(() => fabrica.CreateClient());
        AssertMensagem(erro, "PostgreSQL");
    }

    private static WebApplicationFactory<Program> CriarFabrica(
        string ambiente,
        params (string Chave, string Valor)[] configuracao
    )
    {
        var banco = Path.Combine(Path.GetTempPath(), $"jogodogrupo-prod-{Guid.NewGuid():N}.db");
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(ambiente);
            builder.UseSetting("ConnectionStrings:Jogo", $"Data Source={banco}");
            builder.UseSetting("TempoReal:Porta", "0");
            builder.UseSetting("TempoReal:Endereco", "127.0.0.1");
            foreach (var (chave, valor) in configuracao)
            {
                builder.UseSetting(chave, valor);
            }
        });
    }

    private static void AssertMensagem(Exception erro, string trecho)
    {
        var mensagens = new List<string>();
        for (var e = erro; e != null; e = e.InnerException)
        {
            mensagens.Add(e.Message);
        }
        Assert.Contains(trecho, string.Join(" | ", mensagens));
    }
}
