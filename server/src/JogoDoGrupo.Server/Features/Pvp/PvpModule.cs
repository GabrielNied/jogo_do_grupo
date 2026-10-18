using System;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Pvp;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Features.Pvp;

public sealed class PvpModule : IFeatureModule
{
    private ServicoDePvp? _servico;

    public string Nome => "PvP";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton(sp =>
        {
            var relogio = sp.GetService<TimeProvider>() ?? TimeProvider.System;
            return new ServicoDePvp(
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetRequiredService<ITransicoesDeAreaDoMundo>(),
                sp.GetService<IDiretorioDePersonagens>(),
                sp.GetService<ILogger<ServicoDePvp>>(),
                () => relogio.GetUtcNow().ToUnixTimeMilliseconds()
            );
        });
        servicos.AddSingleton<IHostedService>(sp => sp.GetRequiredService<ServicoDePvp>());
        servicos.AddSingleton(sp =>
        {
            var relogio = sp.GetService<TimeProvider>() ?? TimeProvider.System;
            return new AlvosDePvp(
                sp.GetRequiredService<ServicoDePvp>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                () => relogio.GetUtcNow().ToUnixTimeMilliseconds()
            );
        });
        servicos.AddSingleton<IAlvosDeCombate>(sp => sp.GetRequiredService<AlvosDePvp>());
    }

    public void MapearRotas(IEndpointRouteBuilder rotas) =>
        _servico = rotas.ServiceProvider.GetRequiredService<ServicoDePvp>();

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(PvpMessageIds.Faixa)
            .Ao<SolicitarDuelo>(
                PvpMessageIds.SolicitarDuelo,
                (sessao, msg) => _servico?.SolicitarDuelo(sessao, msg)
            )
            .Ao<ResponderConviteDeDuelo>(
                PvpMessageIds.ResponderConviteDeDuelo,
                (sessao, msg) => _servico?.ResponderConviteDuelo(sessao, msg)
            )
            .Ao<CancelarDuelo>(
                PvpMessageIds.CancelarDuelo,
                (sessao, _) => _servico?.CancelarDuelo(sessao)
            )
            .Ao<EntrarNaFilaDeArena>(
                PvpMessageIds.EntrarNaFilaDeArena,
                (sessao, _) => _servico?.EntrarNaFilaArena(sessao)
            )
            .Ao<SairDaFilaDeArena>(
                PvpMessageIds.SairDaFilaDeArena,
                (sessao, _) => _servico?.SairDaFilaArena(sessao)
            )
            .Ao<ConsultarRankingArena>(
                PvpMessageIds.ConsultarRankingArena,
                (sessao, _) => _servico?.ConsultarRankingArena(sessao)
            )
            .Ao<AlternarBandeiraPvp>(
                PvpMessageIds.AlternarBandeiraPvp,
                (sessao, msg) => _servico?.AlternarBandeiraPvp(sessao, msg.Ativar)
            );

    public void Inicializar(ServerContext contexto)
    {
        contexto.AoPasso(TimeSpan.FromSeconds(1), _ => _servico?.Varrer(), "PvP");
    }
}
