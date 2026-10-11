using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

public sealed class CombateModule : IFeatureModule
{
    // Criado pelo contêiner quando o app sobe (hosted service); o manipulador de rede só o usa
    // depois que uma sessão se autentica.
    private volatile ServicoDeAtaques? _ataques;
    private volatile ServicoDeEsquivas? _esquivas;

    public string Nome => "Combate";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<IBonusDeAtributos, SemBonusDeAtributos>();
        servicos.AddSingleton<IAlvosDeCombate>(new BonecoDeTreino());
        servicos.AddSingleton<ControleDeAcoes>();
        servicos.AddSingleton(sp => new ServicoDeEsquivas(
            sp.GetRequiredService<ServidorTempoReal>(),
            sp.GetRequiredService<IPosicoesDoMundo>(),
            sp.GetRequiredService<IDeslocamentosDoMundo>(),
            sp.GetRequiredService<ControleDeAcoes>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<ILogger<ServicoDeEsquivas>>()
        ));
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDeAtaques(
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetServices<IAlvosDeCombate>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<ILogger<ServicoDeAtaques>>(),
                controle: sp.GetRequiredService<ControleDeAcoes>()
            );
            _ataques = servico;
            _esquivas = sp.GetRequiredService<ServicoDeEsquivas>();
            return servico;
        });
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDeAtaques>());
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
            );

    public void Inicializar(ServerContext contexto) { }
}
