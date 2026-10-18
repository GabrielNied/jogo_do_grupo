using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Contratos.Talentos;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Social;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Social no servidor. Por enquanto, o grupo de jogadores (convite, lista, líder, sair, expulsar,
/// passar liderança e desconexão). As regras vivem em <see cref="GruposDoServidor"/>; aqui só se
/// liga o tempo real a elas. A identidade de quem pede vem sempre da sessão autenticada.
/// O chat de texto (canais Cidade e Sussurro) segue o mesmo molde, com as regras em
/// <see cref="ChatDoServidor"/>.
/// </summary>
public sealed class SocialModule : IFeatureModule, IConsultaDeGrupo
{
    private readonly DiretorioTardio _diretorio = new();
    private GruposDoServidor? _grupos;
    private AmigosDoServidor? _amigos;
    private ControleDeMarcadoresDeGrupo? _marcadores;
    private ServicoDeInspecao? _inspecao;

    /// <summary>Quem tem sessão de tempo real agora, por personagem (para entregar o chat).</summary>
    private readonly ConcurrentDictionary<long, SessaoTempoReal> _online = new();

    private readonly Lock _travaDaFila = new();

    /// <summary>
    /// Os pedidos de chat são processados um de cada vez, na ordem em que chegaram, fora do
    /// laço de rede (o chat consulta nomes no banco).
    /// </summary>
    private Task _fila = Task.CompletedTask;

    private IServiceProvider? _servicos;
    private ServidorTempoReal? _tempoReal;
    private ChatDoServidor? _chat;
    private LimitesDoChat? _limites;
    private HistoricoDaCidadeDoChat? _historico;
    private ILogger<SocialModule>? _log;

    public string Nome => "Social";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<IConsultaDeGrupo>(this);
        servicos.AddSingleton<IAliadosDeCombate, AliadosDeCombateDoSocial>();
        servicos.AddSingleton<LimitesDoChat>();
        servicos.AddSingleton<ChatDoServidor>();
        servicos.AddSingleton<HistoricoDaCidadeDoChat>();

        // O módulo não recebe o provedor de serviços; o host cria este serviço antes de abrir a
        // porta UDP, então o diretório já está ligado quando a primeira mensagem chegar.
        servicos.AddHostedService(sp =>
        {
            _diretorio.Ligar(sp.GetRequiredService<IDiretorioDePersonagens>());
            return new VarreduraDosGrupos(
                _grupos
                    ?? throw new InvalidOperationException(
                        "Social: Inicializar precisa rodar antes de o host subir."
                    ),
                sp.GetRequiredService<ILogger<VarreduraDosGrupos>>()
            );
        });
    }

    /// <summary>
    /// Social não tem rotas HTTP. Este é o passo em que o módulo recebe os serviços do app, de
    /// onde saem o chat e as consultas das outras funcionalidades.
    /// </summary>
    public void MapearRotas(IEndpointRouteBuilder rotas) => _servicos = rotas.ServiceProvider;

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(SocialMessageIds.Faixa)
            .Ao<ConvidarParaGrupo>(
                SocialMessageIds.ConvidarParaGrupo,
                (sessao, pedido) => _grupos!.Convidar(sessao.PersonagemId, pedido.Nome)
            )
            .Ao<ResponderConvite>(
                SocialMessageIds.ResponderConvite,
                (sessao, pedido) =>
                    _grupos!.Responder(sessao.PersonagemId, pedido.ConviteId, pedido.Aceitar)
            )
            .Ao<SairDoGrupo>(
                SocialMessageIds.SairDoGrupo,
                (sessao, _) => _grupos!.Sair(sessao.PersonagemId)
            )
            .Ao<ExpulsarDoGrupo>(
                SocialMessageIds.ExpulsarDoGrupo,
                (sessao, pedido) => _grupos!.Expulsar(sessao.PersonagemId, pedido.PersonagemId)
            )
            .Ao<PassarLideranca>(
                SocialMessageIds.PassarLideranca,
                (sessao, pedido) =>
                    _grupos!.PassarLideranca(sessao.PersonagemId, pedido.PersonagemId)
            )
            .Ao<EnviarMensagemDeChat>(SocialMessageIds.EnviarMensagemDeChat, AoEnviarMensagem)
            .Ao<PedirAmizade>(
                SocialMessageIds.PedirAmizade,
                (sessao, pedido) => _amigos!.PedirAmizade(sessao.PersonagemId, pedido.Nome)
            )
            .Ao<ResponderPedidoDeAmizade>(
                SocialMessageIds.ResponderPedidoDeAmizade,
                (sessao, pedido) =>
                    _amigos!.ResponderPedido(sessao.PersonagemId, pedido.PedidoId, pedido.Aceitar)
            )
            .Ao<CancelarPedidoDeAmizade>(
                SocialMessageIds.CancelarPedidoDeAmizade,
                (sessao, pedido) => _amigos!.CancelarPedido(sessao.PersonagemId, pedido.PedidoId)
            )
            .Ao<RemoverAmigo>(
                SocialMessageIds.RemoverAmigo,
                (sessao, pedido) => _amigos!.RemoverAmigo(sessao.PersonagemId, pedido.AmigoId)
            )
            .Ao<IgnorarJogador>(
                SocialMessageIds.IgnorarJogador,
                (sessao, pedido) => _amigos!.Ignorar(sessao.PersonagemId, pedido.Nome)
            )
            .Ao<DeixarDeIgnorar>(
                SocialMessageIds.DeixarDeIgnorar,
                (sessao, pedido) => _amigos!.DeixarDeIgnorar(sessao.PersonagemId, pedido.IgnoradoId)
            )
            .Ao<DefinirMarcadorDeAlvo>(
                SocialMessageIds.DefinirMarcadorDeAlvo,
                (sessao, pedido) => _marcadores?.DefinirMarcador(sessao.PersonagemId, pedido)
            .Ao<PedirInspecao>(
                SocialMessageIds.PedirInspecao,
                (sessao, pedido) =>
                    _ = _inspecao!.ProcessarPedidoAsync(
                        sessao.PersonagemId,
                        pedido,
                        CancellationToken.None
                    )
            );

    public void Inicializar(ServerContext contexto)
    {
        var saida = new SaidaTempoReal(contexto.TempoReal);
        var escopos = _servicos!.GetRequiredService<IServiceScopeFactory>();
        _inspecao = new ServicoDeInspecao(
            _diretorio,
            _servicos!.GetRequiredService<IConsultaInspecaoEquipamento>(),
            escopos,
            saida,
            id => _online.ContainsKey(id),
            this,
            () => _servicos!.GetService<IPosicoesDoMundo>(),
            () => _servicos!.GetService<IAtributosDaArvore>(),
            _servicos!.GetRequiredService<ILogger<ServicoDeInspecao>>()
        );
        _amigos = new AmigosDoServidor(
            _diretorio,
            saida,
            escopos,
            () => TimeProvider.System.GetUtcNow().ToUnixTimeSeconds(),
            () => _servicos!.GetService<IPosicoesDoMundo>()
        );
        // Carrega do banco no primeiro uso: na subida o banco pode não estar migrado ainda.
        var grupos = new GruposDoServidor(
            _diretorio,
            saida,
            () => Environment.TickCount64,
            _amigos.ContatoBarradoEntrePersonagens,
            () => _servicos!.GetService<IPosicoesDoMundo>()
        );
        _grupos = grupos;
        _marcadores = new ControleDeMarcadoresDeGrupo(
            this,
            saida,
            _servicos!.GetRequiredService<ILogger<ControleDeMarcadoresDeGrupo>>()
        );

        _tempoReal = contexto.TempoReal;
        _chat = _servicos!.GetRequiredService<ChatDoServidor>();
        _limites = _servicos!.GetRequiredService<LimitesDoChat>();
        _historico = _servicos!.GetRequiredService<HistoricoDaCidadeDoChat>();
        contexto.Eventos.Assinar<JogadorEntrouNaArea>(AoEntrarNaArea);
        contexto.Eventos.Assinar<VidaDoPersonagemMudou>(AoMudarVida);
        _log = _servicos!.GetRequiredService<ILogger<SocialModule>>();

        contexto.TempoReal.SessaoAutenticada += sessao =>
        {
            _online[sessao.PersonagemId] = sessao;
            saida.Registrar(sessao);
            _amigos!.RegistrarOnline(sessao.ContaId, sessao.PersonagemId);
            grupos.AoConectar(sessao.PersonagemId);
        };
        contexto.TempoReal.SessaoEncerrada += sessao =>
        {
            // Só tira se ainda for esta sessão: reconectar troca a sessão antes de a antiga acabar.
            _online.TryRemove(new KeyValuePair<long, SessaoTempoReal>(sessao.PersonagemId, sessao));
            _limites!.Esquecer(sessao.PersonagemId);
            // Sessão já substituída por outra do mesmo personagem: ele continua online.
            _amigos!.RemoverOnline(sessao.ContaId, sessao.PersonagemId);
            if (saida.Remover(sessao))
            {
                grupos.AoDesconectar(sessao.PersonagemId);
            }
        };
        foreach (var sessao in contexto.TempoReal.Sessoes)
        {
            _online[sessao.PersonagemId] = sessao;
            saida.Registrar(sessao);
            _amigos!.RegistrarOnline(sessao.ContaId, sessao.PersonagemId);
            grupos.AoConectar(sessao.PersonagemId);
        }
    }

    /// <summary>Expõe a consulta Social; os grupos existem após a inicialização do módulo.</summary>
    public GrupoAtual? Consultar(long personagemId) =>
        (
            _grupos ?? throw new InvalidOperationException("Social ainda não foi inicializado.")
        ).Consultar(personagemId);

    // Quem entra na cidade recebe o que foi dito há pouco no canal Cidade (se houver algo).
    private void AoEntrarNaArea(JogadorEntrouNaArea evento)
    {
        _amigos?.AoMudarArea(evento.PersonagemId);
        AoMudarAreaDoGrupo(evento.PersonagemId);
        if (
            evento.Area != AreasDoMundo.Cidade
            || !_online.TryGetValue(evento.PersonagemId, out var sessao)
        )
        {
            return;
        }

        var mensagens = _historico!.Atual();
        if (mensagens.Length > 0)
        {
            _tempoReal!.Enviar(
                sessao,
                SocialMessageIds.HistoricoDaCidade,
                new HistoricoDaCidade { Mensagens = mensagens }
            );
        }
    }

    private void AoMudarAreaDoGrupo(long personagemId)
    {
        if (_grupos == null)
        {
            return;
        }

        var grupo = _grupos.Consultar(personagemId);
        if (grupo == null)
        {
            return;
        }

        var area = "";
        if (
            _servicos?.GetService<IPosicoesDoMundo>()?.TentarObter(personagemId, out var pos)
            == true
        )
        {
            area = RotulosDeArea.Descricao(pos.Area, pos.X, pos.Z);
        }

        if (!_grupos.AtualizarArea(personagemId, area))
        {
            return;
        }

        var msg = new AreaDoMembroMudou { PersonagemId = personagemId, Area = area };

        foreach (var membroId in grupo.Membros)
        {
            if (
                _tempoReal != null
                && _tempoReal.TentarObterSessao(membroId, out var sessao)
                && sessao != null
            )
            {
                _tempoReal.Enviar(sessao, SocialMessageIds.AreaDoMembroMudou, msg);
            }
        }
    }

    private void AoMudarVida(VidaDoPersonagemMudou mudou)
    {
        if (_grupos == null)
        {
            return;
        }

        var grupo = _grupos.Consultar(mudou.PersonagemId);
        if (grupo == null)
        {
            return;
        }

        _grupos.AtualizarVida(mudou.PersonagemId, mudou.VidaAtual, mudou.VidaMaxima);

        var msg = new VidaDoMembroMudou
        {
            PersonagemId = mudou.PersonagemId,
            Vida = mudou.VidaAtual,
            VidaMaxima = mudou.VidaMaxima,
        };

        foreach (var membroId in grupo.Membros)
        {
            if (
                _tempoReal != null
                && _tempoReal.TentarObterSessao(membroId, out var sessao)
                && sessao != null
            )
            {
                _tempoReal.Enviar(sessao, SocialMessageIds.VidaDoMembroMudou, msg);
            }
        }
    }

    private void AoEnviarMensagem(SessaoTempoReal sessao, EnviarMensagemDeChat pedido)
    {
        lock (_travaDaFila)
        {
            _fila = _fila.ContinueWith(
                _ => Processar(sessao, pedido),
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default
            );
        }
    }

    private void Processar(SessaoTempoReal sessao, EnviarMensagemDeChat pedido)
    {
        try
        {
            var resultado = _chat!.Processar(
                sessao.PersonagemId,
                pedido,
                _online.ContainsKey,
                _grupos!.ListaDe,
                _amigos!.ContatoBarradoEntrePersonagens
            );
            if (resultado.Recusa != null)
            {
                _tempoReal!.Enviar(
                    sessao,
                    SocialMessageIds.MensagemDeChatRecusada,
                    resultado.Recusa
                );
                // O texto da mensagem nunca vai para o log (privacidade).
                _log!.LogDebug(
                    "Chat recusado: personagem {PersonagemId}, canal {Canal}, {Codigo}",
                    sessao.PersonagemId,
                    pedido.Canal,
                    resultado.Recusa.Codigo
                );
                return;
            }

            var mensagem = resultado.Mensagem!;
            if (mensagem.Canal == CanalDeChat.Cidade)
            {
                _historico!.Guardar(mensagem);
            }

            foreach (var personagemId in resultado.Destinatarios)
            {
                if (_online.TryGetValue(personagemId, out var destino))
                {
                    _tempoReal!.Enviar(destino, SocialMessageIds.MensagemDeChat, mensagem);
                }
            }

            _log!.LogDebug(
                "Chat: personagem {PersonagemId}, canal {Canal}, {Quantidade} destinatários",
                sessao.PersonagemId,
                pedido.Canal,
                resultado.Destinatarios.Count
            );
        }
        catch (Exception erro)
        {
            // Uma falha (ex.: banco ocupado) não pode parar a fila do chat.
            _log!.LogError(
                erro,
                "Falha ao processar o chat do personagem {PersonagemId}",
                sessao.PersonagemId
            );
        }
    }

    /// <summary>
    /// O diretório de personagens só existe depois que o app é montado; este repassa as consultas
    /// a ele quando for ligado (antes disso, nenhum personagem é encontrado).
    /// </summary>
    private sealed class DiretorioTardio : IDiretorioDePersonagens
    {
        private volatile IDiretorioDePersonagens? _real;

        public void Ligar(IDiretorioDePersonagens real) => _real = real;

        public bool TentarObterPorNome(string nome, out long personagemId)
        {
            personagemId = 0;
            return _real?.TentarObterPorNome(nome, out personagemId) ?? false;
        }

        public bool TentarObterNome(long personagemId, out string nome)
        {
            nome = "";
            return _real?.TentarObterNome(personagemId, out nome) ?? false;
        }

        public bool TentarObterRaca(long personagemId, out int raca)
        {
            raca = 0;
            return _real?.TentarObterRaca(personagemId, out raca) ?? false;
        }

        public bool TentarObterContaDoPersonagem(long personagemId, out long contaId)
        {
            contaId = 0;
            return _real?.TentarObterContaDoPersonagem(personagemId, out contaId) ?? false;
        }
    }
}
