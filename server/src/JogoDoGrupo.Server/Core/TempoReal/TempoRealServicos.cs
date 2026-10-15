using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Mundo;

namespace JogoDoGrupo.Server.Core.TempoReal;

internal static class TempoRealServicos
{
    /// <summary>
    /// Registra o host UDP (sobe e para junto com o app) e o registro de mensagens.
    /// Precisa da autenticação JWT já registrada.
    /// </summary>
    public static IServiceCollection AdicionarTempoReal(
        this IServiceCollection servicos,
        IConfiguration configuracao
    )
    {
        servicos.AddSingleton<IHistoricoDePosicoesDoMundo, HistoricoDePosicoesDoMundo>();
        servicos.AddSingleton(ConfiguracaoTempoReal.Ler(configuracao));
        servicos.AddSingleton<MensagensTempoReal>();
        servicos.AddSingleton<AutenticadorTempoReal>();
        servicos.AddSingleton(sp => new ServidorTempoReal(
            sp.GetRequiredService<ConfiguracaoTempoReal>(),
            sp.GetRequiredService<MensagensTempoReal>(),
            sp.GetRequiredService<AutenticadorTempoReal>(),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<ILogger<ServidorTempoReal>>()
        ));
        servicos.AddHostedService(sp => sp.GetRequiredService<ServidorTempoReal>());
        return servicos;
    }
}
