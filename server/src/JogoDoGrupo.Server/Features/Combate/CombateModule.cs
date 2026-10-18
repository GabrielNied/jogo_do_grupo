using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Social;
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
    private volatile ServicoDeGuarda? _guarda;
    private volatile ServicoDeTrocaDeArma? _troca;
    private volatile ServicoDeDanoRecebido? _danoRecebido;
    private volatile ServicoDeQueda? _queda;
    private volatile RecursosEmMemoria? _recursos;

    public string Nome => "Combate";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.TryAddSingleton<RelogioDeCombate>();
        servicos.AddSingleton<IBonusDeAtributos, SemBonusDeEfeitos>();
        servicos.AddSingleton(sp => new GerenciadorDeEfeitosDeStatus(() =>
            sp.GetRequiredService<RelogioDeCombate>().AgoraMs
        ));
        servicos.AddSingleton<RecursosEmMemoria>();
        servicos.AddHostedService(sp =>
        {
            _recursos = sp.GetRequiredService<RecursosEmMemoria>();
            return _recursos;
        });
        servicos.AddSingleton<IAlvosDeCombate>(new BonecoDeTreino());
        servicos.AddSingleton(sp => new ControleDeAcoes(
            sp.GetRequiredService<GerenciadorDeEfeitosDeStatus>()
        ));
        servicos.TryAddSingleton<IAliadosDeCombate, NinguemEAliado>();
        servicos.TryAddSingleton<ConfigQueda>();
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDeQueda(
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetRequiredService<EventosDoServidor>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<ServicoDeQueda>>(),
                sp.GetRequiredService<ConfigQueda>(),
                vizinhanca: sp.GetService<IVizinhanca>(),
                grupos: sp.GetService<IConsultaDeGrupo>(),
                relogioMs: () => sp.GetRequiredService<RelogioDeCombate>().AgoraMs,
                esperarAte: sp.GetRequiredService<RelogioDeCombate>().EsperarAteAsync
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
            sp.GetRequiredService<ILogger<ServicoDeEsquivas>>(),
            queda: sp.GetRequiredService<ServicoDeQueda>(),
            vizinhanca: sp.GetService<IVizinhanca>(),
            eventos: sp.GetService<EventosDoServidor>(),
            relogioMs: () => sp.GetRequiredService<RelogioDeCombate>().AgoraMs
        ));
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDeTrocaDeArma(
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetRequiredService<ControleDeAcoes>(),
                sp.GetService<ITrocaDeConjunto>(),
                sp.GetRequiredService<ILogger<ServicoDeTrocaDeArma>>(),
                queda: sp.GetRequiredService<ServicoDeQueda>(),
                relogioMs: () => sp.GetRequiredService<RelogioDeCombate>().AgoraMs,
                armas: sp.GetService<IArmaEquipada>()
            );
            _troca = servico;
            return servico;
        });
        servicos.AddSingleton<ITrocaDeArma>(sp => sp.GetRequiredService<ServicoDeTrocaDeArma>());
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDeGuarda(
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetRequiredService<ControleDeAcoes>(),
                sp.GetRequiredService<ServicoDeQueda>(),
                sp.GetService<ILimiteDeVelocidadeDoMundo>(),
                escopos: sp.GetRequiredService<IServiceScopeFactory>(),
                log: sp.GetRequiredService<ILogger<ServicoDeGuarda>>(),
                vizinhanca: sp.GetService<IVizinhanca>(),
                relogioMs: () => sp.GetRequiredService<RelogioDeCombate>().AgoraMs
            );
            _guarda = servico;
            return servico;
        });
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDeDanoRecebido(
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<ServicoDeDanoRecebido>>(),
                sp.GetRequiredService<ServicoDeQueda>(),
                sp.GetRequiredService<ControleDeAcoes>(),
                guarda: sp.GetRequiredService<ServicoDeGuarda>(),
                posicoes: sp.GetRequiredService<IPosicoesDoMundo>(),
                efeitos: sp.GetRequiredService<GerenciadorDeEfeitosDeStatus>(),
                vizinhanca: sp.GetService<IVizinhanca>(),
                equipamentos: sp.GetService<IAtributosDosEquipamentos>(),
                eventos: sp.GetService<EventosDoServidor>(),
                relogioMs: () => sp.GetRequiredService<RelogioDeCombate>().AgoraMs
            );
            _danoRecebido = servico;
            return servico;
        });
        servicos.AddSingleton(sp => new ServicoDeProjeteis(
            sp.GetRequiredService<ServidorTempoReal>(),
            sp.GetRequiredService<IPosicoesDoMundo>(),
            sp.GetServices<IAlvosDeCombate>(),
            sp.GetRequiredService<ILogger<ServicoDeProjeteis>>(),
            aliados: sp.GetService<IAliadosDeCombate>(),
            vizinhanca: sp.GetService<IVizinhanca>(),
            efeitos: sp.GetService<GerenciadorDeEfeitosDeStatus>(),
            eventos: sp.GetService<EventosDoServidor>(),
            controle: sp.GetService<ControleDeAcoes>(),
            queda: sp.GetService<ServicoDeQueda>(),
            escopos: sp.GetService<IServiceScopeFactory>(),
            relogioMs: () => sp.GetRequiredService<RelogioDeCombate>().AgoraMs
        ));
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDeProjeteis>());
        servicos.AddSingleton<IDisparoDeProjeteis>(sp =>
            sp.GetRequiredService<ServicoDeProjeteis>()
        );
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDeAtaques(
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetServices<IAlvosDeCombate>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<ServicoDeAtaques>>(),
                controle: sp.GetRequiredService<ControleDeAcoes>(),
                queda: sp.GetRequiredService<ServicoDeQueda>(),
                aliados: sp.GetRequiredService<IAliadosDeCombate>(),
                limiteDeVelocidade: sp.GetService<ILimiteDeVelocidadeDoMundo>(),
                efeitos: sp.GetRequiredService<GerenciadorDeEfeitosDeStatus>(),
                vizinhanca: sp.GetService<IVizinhanca>(),
                eventos: sp.GetService<EventosDoServidor>(),
                relogioMs: () => sp.GetRequiredService<RelogioDeCombate>().AgoraMs,
                esperarAte: sp.GetRequiredService<RelogioDeCombate>().EsperarAteAsync,
                armas: sp.GetService<IArmaEquipada>(),
                projeteis: sp.GetService<ServicoDeProjeteis>()
            );
            _ataques = servico;
            _esquivas = sp.GetRequiredService<ServicoDeEsquivas>();
            _troca = sp.GetRequiredService<ServicoDeTrocaDeArma>();
            _guarda = sp.GetRequiredService<ServicoDeGuarda>();
            return servico;
        });
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDeAtaques>());
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDeDanoRecebido>());
        servicos.AddSingleton<IDanoAoPersonagem>(sp =>
            sp.GetRequiredService<ServicoDeDanoRecebido>()
        );
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDeQueda>());
        servicos.AddSingleton(sp => new ServicoDeEfeitosDeStatus(
            sp.GetRequiredService<GerenciadorDeEfeitosDeStatus>(),
            sp.GetRequiredService<ServidorTempoReal>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ILogger<ServicoDeEfeitosDeStatus>>(),
            posicoes: sp.GetService<IPosicoesDoMundo>(),
            queda: sp.GetService<ServicoDeQueda>(),
            vizinhanca: sp.GetService<IVizinhanca>(),
            relogioMs: () => sp.GetRequiredService<RelogioDeCombate>().AgoraMs
        ));
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDeEfeitosDeStatus>());
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
            .Ao<IntencaoDeGuarda>(
                CombateMessageIds.IntencaoDeGuarda,
                (sessao, intencao) => _guarda?.AoGuardar(sessao, intencao)
            )
            .Ao<IntencaoDeTrocaDeArma>(
                CombateMessageIds.IntencaoDeTrocaDeArma,
                (sessao, intencao) => _troca?.AoTrocar(sessao, intencao)
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
            )
            .Ao<ConsultarArmaAtiva>(
                CombateMessageIds.ConsultarArmaAtiva,
                (sessao, _) => _ataques?.AoConsultarArmaAtiva(sessao)
            );

    public void Inicializar(ServerContext contexto)
    {
        contexto.Eventos.Assinar<GolpeDeInimigoAcertou>(evento =>
            _danoRecebido?.AoGolpeDeInimigo(evento)
        );
        contexto.Eventos.Assinar<EquipamentoMudou>(evento =>
            _recursos?.AoEquipamentoMudar(evento.PersonagemId)
        );
    }

    // Chamado pelo módulo no Inicializar após obter RecursosEmMemoria do contêiner.
    internal void AssinarEquipamentoMudou(EventosDoServidor eventos, RecursosEmMemoria recursos) =>
        eventos.Assinar<EquipamentoMudou>(e => recursos.AoEquipamentoMudar(e.PersonagemId));
}
