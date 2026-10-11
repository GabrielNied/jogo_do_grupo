using System.Reflection;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Contas;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddDbContext<JogoDbContext>(opcoes =>
    opcoes.UseSqlite(builder.Configuration.GetConnectionString("Jogo"))
);
builder.Services.AdicionarAutenticacaoJwt(builder.Configuration, builder.Environment);
builder.Services.AdicionarContas();
builder.Services.AdicionarTempoReal(builder.Configuration);
builder.Services.AddSingleton<EventosDoServidor>();

foreach (var modulo in modulos)
{
    modulo.ConfigurarServicos(builder.Services, builder.Configuration);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var escopo = app.Services.CreateScope();
    escopo.ServiceProvider.GetRequiredService<JogoDbContext>().Database.Migrate();
}

app.UsarErrosApi();
app.UseAuthentication();
app.UseAuthorization();

app.MapearContas();
foreach (var modulo in modulos)
{
    modulo.MapearRotas(app);
}

var mensagens = app.Services.GetRequiredService<MensagensTempoReal>();
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
