using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Core.Canais;

internal static class CanaisServicos
{
    public static IServiceCollection AdicionarCanais(
        this IServiceCollection servicos,
        IConfiguration configuracao
    )
    {
        var config = ConfiguracaoCanais.Ler(configuracao);
        servicos.AddSingleton(config);
        servicos.AddSingleton<IGerenciadorDeCanais, GerenciadorDeCanais>();
        return servicos;
    }
}
