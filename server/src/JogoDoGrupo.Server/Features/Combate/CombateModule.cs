using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JogoDoGrupo.Server.Features.Combate;

public sealed class CombateModule : IFeatureModule
{
    // Criado pelo contêiner quando o app sobe (hosted service); o manipulador de rede só o usa
    // depois que uma sessão se autentica.
    private volatile ServicoDeAtaques? _ataques;
    private volatile ServicoDeEsquivas? _esquivas;
    private volatile ServicoDeDanoRecebido? _danoRecebido;
    private volatile ServicoDeQueda? _queda;

    public string Nome => "Combate";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<IBonusDeAtributos, BonusDeEquipamentos>();
        servicos.AddSingleton<IAlvosDeCombate>(new BonecoDeTreino());
        servicos.AddSingleton<ControleDeAcoes>();
        servicos.TryAddSingleton<ConfigQueda>();
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDeQueda(
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetRequiredService<EventosDoServidor>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<ILogger<ServicoDeQueda>>(),
                sp.GetRequiredService<ConfigQueda>()
            );
            _queda = servico;
            return servico;
        });
        servicos.AddSingleton<IEstadoDeQueda>(sp => sp.GetRequiredService<ServicoDeQueda>());
        servicos.AddSingleton(sp => new ServicoDeEsquivas(
            sp.GetRequiredService<ServidorTempoReal>(),
            sp.GetRequiredService<IPosicoesDoMundo>(),
            sp.GetRequiredService<IDeslocamentosDoMundo>(),
            sp.GetRequiredService<ControleDeAcoes>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<ILogger<ServicoDeEsquivas>>(),
            queda: sp.GetRequiredService<ServicoDeQueda>()
        ));
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDeDanoRecebido(
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<ILogger<ServicoDeDanoRecebido>>(),
                sp.GetRequiredService<ServicoDeQueda>()
            );
            _danoRecebido = servico;
            return servico;
        });
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDeAtaques(
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetServices<IAlvosDeCombate>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<ILogger<ServicoDeAtaques>>(),
                controle: sp.GetRequiredService<ControleDeAcoes>(),
                queda: sp.GetRequiredService<ServicoDeQueda>()
            );
            _ataques = servico;
            _esquivas = sp.GetRequiredService<ServicoDeEsquivas>();
            return servico;
        });
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDeAtaques>());
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDeDanoRecebido>());
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDeQueda>());
    }

    public void MapearRotas(IEndpointRouteBuilder rotas) => CombateRotas.Mapear(rotas);

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(CombateMessageIds.Faixa)
            .Ao<IntencaoDeAtaque>(
                CombateMessageIds.IntencaoDeAtaque,
                (sessao, intencao) => _ataques?.AoAtacar(sessao, intencao)
            )
            .Ao<IntencaoDeEsquiva>(
                CombateMessageIds.IntencaoDeEsquiva,
                (sessao, intencao) => _esquivas?.AoEsquivar(sessao, intencao)
            )
            .Ao<ComecarALevantar>(
                CombateMessageIds.ComecarALevantar,
                (sessao, pedido) => _queda?.AoComecarALevantar(sessao, pedido)
            )
            .Ao<CancelarLevantar>(
                CombateMessageIds.CancelarLevantar,
                (sessao, _) => _queda?.AoCancelarLevantar(sessao)
            )
            .Ao<VoltarParaACidade>(
                CombateMessageIds.VoltarParaACidade,
                (sessao, _) => _queda?.AoVoltarParaACidade(sessao)
            );

    public void Inicializar(ServerContext contexto) =>
        contexto.Eventos.Assinar<GolpeDeInimigoAcertou>(evento =>
            _danoRecebido?.AoGolpeDeInimigo(evento)
        );
}
