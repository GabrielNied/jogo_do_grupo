using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

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
        builder.UseSetting("ConnectionStrings:Jogo", $"Data Source={_arquivoBanco}");
    }

    /// <summary>Nome único só com letras (serve para usuário e para personagem).</summary>
    public static string NomeUnico(string prefixo = "T")
    {
        var letras = Guid.NewGuid().ToString("N").Select(c => (char)('a' + c % 26)).Take(10);
        return prefixo + new string(letras.ToArray());
    }

    public static Task<HttpResponseMessage> PostJsonAsync<T>(
        HttpClient cliente,
        string rota,
        T corpo
    ) => cliente.PostAsJsonAsync(rota, corpo, Json);

    public static async Task<T> LerJsonAsync<T>(HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<T>(Json))!;
}
