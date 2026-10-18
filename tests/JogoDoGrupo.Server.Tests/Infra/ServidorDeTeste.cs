using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JogoDoGrupo.Shared.Core.Contas;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Infra;

/// <summary>
/// Sobe o servidor de verdade em memória, com um banco SQLite só desta instância (arquivo
/// temporário, migrado na inicialização). Use com <c>IClassFixture&lt;ServidorDeTeste&gt;</c>:
/// cada classe de teste ganha um banco limpo. Dentro da classe, use nomes únicos
/// (<see cref="NomeUnico"/>) para os testes não dependerem uns dos outros.
/// </summary>
public sealed class ServidorDeTeste : WebApplicationFactory<Program>
{
    private readonly string _arquivoBanco = Path.Combine(
        Path.GetTempPath(),
        $"jogodogrupo-teste-{Guid.NewGuid():N}.db"
    );

    /// <summary>Mesmo formato de JSON do servidor e do JsonUtility do Unity.</summary>
    public static readonly JsonSerializerOptions Json = new()
    {
        IncludeFields = true,
        PropertyNamingPolicy = null,
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Jogo", $"Data Source={_arquivoBanco};Pooling=False");
        // Cada servidor de teste escuta o UDP numa porta livre, só no loopback.
        builder.UseSetting("TempoReal:Porta", "0");
        builder.UseSetting("TempoReal:Endereco", "127.0.0.1");
        // O baú exige estar perto e respeita a recarga (#492); os testes que só precisam de
        // itens abrem sem posição nem espera. Os de posição e recarga ligam de novo.
        builder.UseSetting("Itens:Bau:ExigirPosicao", "false");
        builder.UseSetting("Itens:Bau:RecargaSegundos", "0");
        builder.UseSetting("Economia:Npc:ExigirProximidade", "false");
        builder.UseSetting("Contas:RateLimit:Ativo", "false");
        // Nos testes de integração comuns os jogadores avulsos precisam estar no mesmo canal.
        builder.UseSetting("Canais:Quantidade", "1");
        // O hash de senha de produção (PBKDF2, 100 mil iterações) custa dezenas de ms de CPU por
        // registro e login; com centenas de testes criando conta, essa carga atrasava os testes de
        // tempo real que rodam ao lado. Nos testes, uma iteração basta (o formato não muda).
        builder.ConfigureServices(s =>
            s.Configure<PasswordHasherOptions>(o => o.IterationCount = 1)
        );
    }

    /// <summary>Nome único só com letras (serve para usuário e para personagem).</summary>
    public static string NomeUnico(string prefixo = "T")
    {
        var letras = Guid.NewGuid().ToString("N").Select(c => (char)('a' + c % 26)).Take(10);
        return prefixo + new string(letras.ToArray());
    }

    /// <summary>
    /// Registra uma conta nova (usuário único) e devolve um cliente já logado, com o token no
    /// header <c>Authorization</c>. Use para testar rotas que exigem login.
    /// </summary>
    public Task<HttpClient> CriarClienteLogadoAsync(string senha = "senha-segura") =>
        CriarClienteLogadoEmAsync(this, senha);

    /// <summary>
    /// O mesmo de <see cref="CriarClienteLogadoAsync"/>, para uma fábrica derivada
    /// (<c>WithWebHostBuilder</c>, ex.: com um serviço trocado por um falso). Ela usa o mesmo banco.
    /// </summary>
    public static async Task<HttpClient> CriarClienteLogadoEmAsync(
        WebApplicationFactory<Program> fabrica,
        string senha = "senha-segura"
    )
    {
        var cliente = fabrica.CreateClient();
        var usuario = NomeUnico("U");
        var registro = await PostJsonAsync(
            cliente,
            RotasContas.Registrar,
            new RegistrarContaRequest { Usuario = usuario, Senha = senha }
        );
        registro.EnsureSuccessStatusCode();
        var login = await PostJsonAsync(
            cliente,
            RotasContas.Login,
            new LoginRequest { Usuario = usuario, Senha = senha }
        );
        login.EnsureSuccessStatusCode();
        var token = (await LerJsonAsync<LoginResponse>(login)).Token;
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );
        return cliente;
    }

    public static Task<HttpResponseMessage> PostJsonAsync<T>(
        HttpClient cliente,
        string rota,
        T corpo
    ) => cliente.PostAsJsonAsync(rota, corpo, Json);

    public static async Task<T> LerJsonAsync<T>(HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<T>(Json))!;
}
