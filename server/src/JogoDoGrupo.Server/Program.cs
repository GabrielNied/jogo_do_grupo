using System.Reflection;
using JogoDoGrupo.Server.Core;

var contexto = new ServerContext();

var modulos = Assembly
    .GetExecutingAssembly()
    .GetTypes()
    .Where(t =>
        typeof(IFeatureModule).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false }
    )
    .Select(t => (IFeatureModule)Activator.CreateInstance(t)!)
    .OrderBy(m => m.Nome)
    .ToList();

foreach (var modulo in modulos)
{
    modulo.Inicializar(contexto);
    Console.WriteLine($"[ok] {modulo.Nome}");
}

Console.WriteLine($"Servidor iniciado com {modulos.Count} funcionalidades.");
