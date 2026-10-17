using System.Reflection;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Canais;
using JogoDoGrupo.Server.Core.Contas;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Observabilidade;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Core.Api;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

var metricas = new MetricasDoServidor();
builder.Services.AddSingleton(metricas);
builder.Host.UseSerilog(
    (contexto, configuracao) =>
        configuracao
            .MinimumLevel.Information()
            .Enrich.With(new FiltroDadosSensiveis())
            .WriteTo.Console()
            .WriteTo.File(
                new CompactJsonFormatter(),
                Path.GetFullPath(
                    contexto.Configuration["Observabilidade:CaminhoLog"] ?? "logs/servidor-.ndjson",
                    contexto.HostingEnvironment.ContentRootPath
                ),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14
            )
            .WriteTo.Sink(new ContadorDeErrosSink(metricas))
);

var modulos = Assembly
    .GetExecutingAssembly()
    .GetTypes()
    .Where(t =>
        typeof(IFeatureModule).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false }
    )
    .Select(t => (IFeatureModule)Activator.CreateInstance(t)!)
    .OrderBy(m => m.Nome)
    .ToList();

// JSON no formato do JsonUtility do Unity: campos públicos, nomes como no C# e enums como número.
builder.Services.ConfigureHttpJsonOptions(opcoes =>
{
    opcoes.SerializerOptions.IncludeFields = true;
    opcoes.SerializerOptions.PropertyNamingPolicy = null;
});

// Corpo ausente ou JSON malformado lança exceção, e o TratamentoErrosMiddleware responde 400.
builder.Services.Configure<RouteHandlerOptions>(opcoes => opcoes.ThrowOnBadRequest = true);
builder.Services.AdicionarBancoDeDados(builder.Configuration);
builder.Services.AdicionarAutenticacaoJwt(builder.Configuration, builder.Environment);
builder.Services.AdicionarContas();
builder.Services.AdicionarTempoReal(builder.Configuration);
builder.Services.AdicionarCanais(builder.Configuration);
builder.Services.AddSingleton<EventosDoServidor>();

foreach (var modulo in modulos)
{
    modulo.ConfigurarServicos(builder.Services, builder.Configuration);
}

var app = builder.Build();

// Banco:MigrarAoIniciar decide; sem a chave, migra só em Development.
if (app.Configuration.GetValue<bool?>("Banco:MigrarAoIniciar") ?? app.Environment.IsDevelopment())
{
    using var escopo = app.Services.CreateScope();
    escopo.ServiceProvider.GetRequiredService<JogoDbContext>().Database.Migrate();
}

app.UsarErrosApi();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/saude", () => Results.Ok(new { Estado = "ok", Versao = EstadoDaSaude.VersaoAtual }));
app.MapGet(
        "/admin/metricas",
        (MetricasDoServidor observabilidade) => Results.Ok(observabilidade.Obter())
    )
    .RequireAuthorization(policy => policy.RequireClaim("permissao", "administrar"));

app.MapearContas();
foreach (var modulo in modulos)
{
    modulo.MapearRotas(app);
}

var mensagens = app.Services.GetRequiredService<MensagensTempoReal>();
metricas.Observar(app.Services.GetRequiredService<ServidorTempoReal>());
var contexto = new ServerContext(
    app.Services.GetRequiredService<ServidorTempoReal>(),
    app.Services.GetRequiredService<EventosDoServidor>()
);

foreach (var modulo in modulos)
{
    modulo.RegistrarMensagens(mensagens);
    modulo.Inicializar(contexto);
    app.Logger.LogInformation("[ok] {Modulo}", modulo.Nome);
}

app.Logger.LogInformation("Servidor iniciado com {Quantidade} funcionalidades.", modulos.Count);

app.Run();
