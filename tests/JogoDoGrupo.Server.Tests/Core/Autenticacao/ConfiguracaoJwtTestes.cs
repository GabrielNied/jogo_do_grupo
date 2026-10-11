using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JogoDoGrupo.Server.Tests.Core.Autenticacao;

/// <summary>
/// A validação da configuração roda ao subir o servidor, então cada caso sobe um servidor com
/// configuração alterada e espera a recusa.
/// </summary>
public class ConfiguracaoJwtTestes
{
    private const string ChaveDesenvolvimento =
        "SO-PARA-DESENVOLVIMENTO-nao-use-em-producao-troque-por-Jwt__Chave";

    [Fact]
    public void Configuracao_de_desenvolvimento_sobe()
    {
        using var fabrica = CriarFabrica("Development");

        using var cliente = fabrica.CreateClient();
    }

    [Fact]
    public void Emissor_vazio_e_recusado()
    {
        using var fabrica = CriarFabrica("Development", ("Jwt:Emissor", ""));

        AssertRecusa(fabrica, "Emissor");
    }

    [Fact]
    public void Audiencia_vazia_e_recusada()
    {
        using var fabrica = CriarFabrica("Development", ("Jwt:Audiencia", " "));

        AssertRecusa(fabrica, "Audiencia");
    }

    [Fact]
    public void Chave_de_desenvolvimento_fora_de_development_e_recusada()
    {
        using var fabrica = CriarFabrica("Production", ("Jwt:Chave", ChaveDesenvolvimento));

        AssertRecusa(fabrica, "desenvolvimento");
    }

    [Fact]
    public void Chave_propria_fora_de_development_sobe()
    {
        using var fabrica = CriarFabrica("Production", ("Jwt:Chave", new string('x', 40)));

        using var cliente = fabrica.CreateClient();
    }

    [Fact]
    public void Chave_curta_e_recusada()
    {
        using var fabrica = CriarFabrica("Development", ("Jwt:Chave", "curta"));

        AssertRecusa(fabrica, "32 bytes");
    }

    [Fact]
    public void Validade_zero_e_recusada()
    {
        using var fabrica = CriarFabrica("Development", ("Jwt:ValidadeHoras", "0"));

        AssertRecusa(fabrica, "ValidadeHoras");
    }

    private static WebApplicationFactory<Program> CriarFabrica(
        string ambiente,
        params (string Chave, string Valor)[] configuracao
    )
    {
        var banco = Path.Combine(Path.GetTempPath(), $"jogodogrupo-jwt-{Guid.NewGuid():N}.db");
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

    private static void AssertRecusa(WebApplicationFactory<Program> fabrica, string trecho)
    {
        var erro = Assert.ThrowsAny<Exception>(() => fabrica.CreateClient());

        var mensagens = new List<string>();
        for (var e = erro; e != null; e = e.InnerException)
        {
            mensagens.Add(e.Message);
        }
        Assert.Contains(trecho, string.Join(" | ", mensagens));
    }
}
