using JogoDoGrupo.Server.Core.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Core.Persistencia;

/// <summary><c>Banco:MigrarAoIniciar</c> decide; sem a chave, migra só em Development.</summary>
public class MigracaoAoIniciarTestes
{
    [Theory]
    [InlineData("Development", null, true)]
    [InlineData("Production", null, false)]
    [InlineData("Production", "true", true)]
    [InlineData("Production", "false", false)]
    [InlineData("Development", "false", false)]
    public void Migracao_ao_iniciar_segue_ambiente_e_configuracao(
        string ambiente,
        string? migrar,
        bool esperado
    )
    {
        var banco = Path.Combine(Path.GetTempPath(), $"jogodogrupo-migra-{Guid.NewGuid():N}.db");
        using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(ambiente);
            builder.UseSetting("ConnectionStrings:Jogo", $"Data Source={banco}");
            builder.UseSetting("TempoReal:Porta", "0");
            builder.UseSetting("TempoReal:Endereco", "127.0.0.1");
            builder.UseSetting("Jwt:Chave", new string('x', 40));
            if (migrar != null)
            {
                builder.UseSetting("Banco:MigrarAoIniciar", migrar);
            }
        });

        using var cliente = fabrica.CreateClient();
        using var escopo = fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();

        Assert.Equal(esperado, !db.Database.GetPendingMigrations().Any());
    }
}
