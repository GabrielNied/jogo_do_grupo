using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JogoDoGrupo.Robos;
using JogoDoGrupo.Server.Core.Observabilidade;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace JogoDoGrupo.Server.Tests.Core.TempoReal;

public class RegistroDeRecusasTempoRealTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    [Fact]
    public void RegistrarRecusa_IgnoraContaZeroOuNegativa()
    {
        var config = new ConfiguracaoTempoReal();
        var registro = new RegistroDeRecusasTempoReal(
            config,
            NullLogger<RegistroDeRecusasTempoReal>.Instance
        );

        registro.RegistrarRecusa(0, 1, 100, MotivoRecusa.CorpoInvalido);
        registro.RegistrarRecusa(-5, 1, 100, MotivoRecusa.CorpoInvalido);

        Assert.Equal(0, registro.TotalContasComRecusas);
        Assert.Null(registro.Obter(0));
    }

    [Fact]
    public void RegistrarRecusa_IncrementaContadorEAgregaPorMotivo()
    {
        var relogio = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var config = new ConfiguracaoTempoReal
        {
            JanelaRecusasMinutos = 10,
            LimiteAlertaRecusasConta = 5,
        };
        var registro = new RegistroDeRecusasTempoReal(
            config,
            NullLogger<RegistroDeRecusasTempoReal>.Instance,
            relogio
        );

        registro.RegistrarRecusa(10, 101, 1000, MotivoRecusa.CorpoInvalido);
        registro.RegistrarRecusa(10, 101, 1001, MotivoRecusa.CorpoInvalido);
        registro.RegistrarRecusa(10, 102, 1002, MotivoRecusa.LimiteExcedido);

        var relatorio = registro.Obter(10);
        Assert.NotNull(relatorio);
        Assert.Equal(10, relatorio.ContaId);
        Assert.Equal(102, relatorio.PersonagemId);
        Assert.Equal(3, relatorio.TotalRecusas);
        Assert.Equal(nameof(MotivoRecusa.LimiteExcedido), relatorio.UltimoMotivo);
        Assert.Equal(1002, relatorio.UltimoMensagemId);
        Assert.False(relatorio.AlertaAtivo);
        Assert.Equal(2, relatorio.RecusasPorMotivo[nameof(MotivoRecusa.CorpoInvalido)]);
        Assert.Equal(1, relatorio.RecusasPorMotivo[nameof(MotivoRecusa.LimiteExcedido)]);
    }

    [Fact]
    public void Alerta_AtivaAoAtingirLimiteConfigurado()
    {
        var relogio = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var config = new ConfiguracaoTempoReal
        {
            JanelaRecusasMinutos = 10,
            LimiteAlertaRecusasConta = 3,
        };
        var registro = new RegistroDeRecusasTempoReal(
            config,
            NullLogger<RegistroDeRecusasTempoReal>.Instance,
            relogio
        );

        registro.RegistrarRecusa(20, 201, 100, MotivoRecusa.CorpoInvalido);
        registro.RegistrarRecusa(20, 201, 100, MotivoRecusa.CorpoInvalido);
        Assert.False(registro.Obter(20)!.AlertaAtivo);
        Assert.Equal(0, registro.TotalContasEmAlerta);

        registro.RegistrarRecusa(20, 201, 100, MotivoRecusa.LimiteExcedido);
        var relatorio = registro.Obter(20);
        Assert.NotNull(relatorio);
        Assert.True(relatorio.AlertaAtivo);
        Assert.Equal(1, registro.TotalContasEmAlerta);
        Assert.Single(registro.ListarSuspeitas());
    }

    [Fact]
    public void Janela_ExpiraRecusasForaDoTempoLimite()
    {
        var relogio = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var config = new ConfiguracaoTempoReal
        {
            JanelaRecusasMinutos = 10,
            LimiteAlertaRecusasConta = 3,
        };
        var registro = new RegistroDeRecusasTempoReal(
            config,
            NullLogger<RegistroDeRecusasTempoReal>.Instance,
            relogio
        );

        registro.RegistrarRecusa(30, 301, 100, MotivoRecusa.CorpoInvalido);
        registro.RegistrarRecusa(30, 301, 101, MotivoRecusa.CorpoInvalido);
        registro.RegistrarRecusa(30, 301, 102, MotivoRecusa.CorpoInvalido);
        Assert.True(registro.Obter(30)!.AlertaAtivo);

        // Avança 9 minutos: ainda dentro da janela de 10 min
        relogio.Advance(TimeSpan.FromMinutes(9));
        Assert.Equal(3, registro.Obter(30)!.TotalRecusas);

        // Avança mais 2 minutos (total 11 min): as 3 primeiras expiraram
        relogio.Advance(TimeSpan.FromMinutes(2));
        Assert.Null(registro.Obter(30));
        Assert.Equal(0, registro.TotalContasComRecusas);
        Assert.Equal(0, registro.TotalContasEmAlerta);
    }

    [Fact]
    public void Limpar_ResetaContadorParaGM()
    {
        var config = new ConfiguracaoTempoReal { LimiteAlertaRecusasConta = 2 };
        var registro = new RegistroDeRecusasTempoReal(
            config,
            NullLogger<RegistroDeRecusasTempoReal>.Instance
        );

        registro.RegistrarRecusa(40, 401, 100, MotivoRecusa.CorpoInvalido);
        registro.RegistrarRecusa(40, 401, 100, MotivoRecusa.CorpoInvalido);
        Assert.NotNull(registro.Obter(40));

        var limpou = registro.Limpar(40);
        Assert.True(limpou);
        Assert.Null(registro.Obter(40));
        Assert.False(registro.Limpar(40));
    }

    [Fact]
    public void ListarTodas_E_ListarSuspeitas_FiltramCorretamente()
    {
        var config = new ConfiguracaoTempoReal { LimiteAlertaRecusasConta = 3 };
        var registro = new RegistroDeRecusasTempoReal(
            config,
            NullLogger<RegistroDeRecusasTempoReal>.Instance
        );

        // Conta 1: abaixo do limite
        registro.RegistrarRecusa(1, 11, 10, MotivoRecusa.CorpoInvalido);
        // Conta 2: atinge alerta
        registro.RegistrarRecusa(2, 22, 10, MotivoRecusa.CorpoInvalido);
        registro.RegistrarRecusa(2, 22, 10, MotivoRecusa.CorpoInvalido);
        registro.RegistrarRecusa(2, 22, 10, MotivoRecusa.CorpoInvalido);

        var todas = registro.ListarTodas();
        var suspeitas = registro.ListarSuspeitas();

        Assert.Equal(2, todas.Count);
        Assert.Single(suspeitas);
        Assert.Equal(2, suspeitas[0].ContaId);
    }

    [Fact]
    public async Task EndpointAdmin_SemToken_Retorna401()
    {
        using var http = servidor.CreateClient();
        var resposta = await http.GetAsync("/admin/recusas-tempo-real");
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task EndpointAdmin_JogadorComum_Retorna403()
    {
        using var http = await servidor.CriarClienteLogadoAsync();
        var resposta = await http.GetAsync("/admin/recusas-tempo-real");
        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task EndpointAdmin_ComTokenAdmin_PermiteConsultarELimpar()
    {
        using var http = servidor.CreateClient();
        var chave = servidor.Services.GetRequiredService<IConfiguration>()["Jwt:Chave"]!;
        var tokenAdmin = GerarTokenAdmin(chave);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            tokenAdmin
        );

        var registro = servidor.Services.GetRequiredService<IRegistroDeRecusasTempoReal>();
        registro.RegistrarRecusa(999, 888, 1234, MotivoRecusa.LimiteExcedido);

        // GET /admin/recusas-tempo-real
        var resposta = await http.GetAsync("/admin/recusas-tempo-real");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var lista = await ServidorDeTeste.LerJsonAsync<List<RelatorioRecusasContaDto>>(resposta);
        Assert.NotNull(lista);
        Assert.Contains(lista, r => r.ContaId == 999 && r.UltimoMensagemId == 1234);

        // GET /admin/recusas-tempo-real?contaId=999
        var respostaConta = await http.GetAsync("/admin/recusas-tempo-real?contaId=999");
        Assert.Equal(HttpStatusCode.OK, respostaConta.StatusCode);
        var listaConta = await ServidorDeTeste.LerJsonAsync<List<RelatorioRecusasContaDto>>(
            respostaConta
        );
        Assert.NotNull(listaConta);
        Assert.Single(listaConta);
        Assert.Equal(999, listaConta[0].ContaId);

        // DELETE /admin/recusas-tempo-real/999
        var respostaDelete = await http.DeleteAsync("/admin/recusas-tempo-real/999");
        Assert.Equal(HttpStatusCode.OK, respostaDelete.StatusCode);

        // Conferir que foi limpo
        var relatorioAposDelete = registro.Obter(999);
        Assert.Null(relatorioAposDelete);
    }

    [Fact]
    public void MetricasDoServidor_RefleteContadoresDeRecusasTempoReal()
    {
        var config = new ConfiguracaoTempoReal { LimiteAlertaRecusasConta = 2 };
        var registro = new RegistroDeRecusasTempoReal(
            config,
            NullLogger<RegistroDeRecusasTempoReal>.Instance
        );
        var metricas = new MetricasDoServidor();
        var tempoReal = servidor.Services.GetRequiredService<ServidorTempoReal>();
        metricas.Observar(tempoReal, registro);

        var inicial = metricas.Obter();
        Assert.Equal(0, inicial.ContasComRecusasTempoReal);
        Assert.Equal(0, inicial.ContasEmAlertaTempoReal);

        registro.RegistrarRecusa(50, 501, 100, MotivoRecusa.CorpoInvalido);
        var com1 = metricas.Obter();
        Assert.Equal(1, com1.ContasComRecusasTempoReal);
        Assert.Equal(0, com1.ContasEmAlertaTempoReal);

        registro.RegistrarRecusa(50, 501, 100, MotivoRecusa.CorpoInvalido);
        var comAlerta = metricas.Obter();
        Assert.Equal(1, comAlerta.ContasComRecusasTempoReal);
        Assert.Equal(1, comAlerta.ContasEmAlertaTempoReal);
    }

    private static string GerarTokenAdmin(string chave)
    {
        var agora = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        static string Base64Url(byte[] dados) =>
            Convert.ToBase64String(dados).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var cabecalho = Base64Url(
            JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" })
        );
        var carga = Base64Url(
            JsonSerializer.SerializeToUtf8Bytes(
                new Dictionary<string, object>
                {
                    ["sub"] = "0",
                    ["jti"] = Guid.NewGuid().ToString("N"),
                    ["iss"] = "JogoDoGrupo",
                    ["aud"] = "JogoDoGrupo.Client",
                    ["nbf"] = agora,
                    ["iat"] = agora,
                    ["exp"] = agora + 300,
                    ["permissao"] = "administrar",
                }
            )
        );
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(chave));
        var assinatura = Base64Url(
            hmac.ComputeHash(Encoding.UTF8.GetBytes($"{cabecalho}.{carga}"))
        );
        return $"{cabecalho}.{carga}.{assinatura}";
    }
}
