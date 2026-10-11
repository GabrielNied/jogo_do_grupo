using System.Collections.Concurrent;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

public sealed class SocialModule : IFeatureModule
{
    /// <summary>Quem tem sessão de tempo real agora, por personagem.</summary>
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
    private ILogger<SocialModule>? _log;

    public string Nome => "Social";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<LimitesDoChat>();
        servicos.AddSingleton<ChatDoServidor>();
    }

    /// <summary>
    /// Social não tem rotas HTTP. Este é o passo em que o módulo recebe os serviços do app, de
    /// onde saem o chat e as consultas das outras funcionalidades.
    /// </summary>
    public void MapearRotas(IEndpointRouteBuilder rotas) => _servicos = rotas.ServiceProvider;

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(SocialMessageIds.Faixa)
            .Ao<EnviarMensagemDeChat>(SocialMessageIds.EnviarMensagemDeChat, AoEnviarMensagem);

    public void Inicializar(ServerContext contexto)
    {
        _tempoReal = contexto.TempoReal;
        _chat = _servicos!.GetRequiredService<ChatDoServidor>();
        _limites = _servicos!.GetRequiredService<LimitesDoChat>();
        _log = _servicos!.GetRequiredService<ILogger<SocialModule>>();
        _tempoReal.SessaoAutenticada += sessao => _online[sessao.PersonagemId] = sessao;
        _tempoReal.SessaoEncerrada += AoEncerrar;
    }

    private void AoEncerrar(SessaoTempoReal sessao)
    {
        // Só tira se ainda for esta sessão: reconectar troca a sessão antes de a antiga acabar.
        _online.TryRemove(new KeyValuePair<long, SessaoTempoReal>(sessao.PersonagemId, sessao));
        _limites!.Esquecer(sessao.PersonagemId);
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
            var resultado = _chat!.Processar(sessao.PersonagemId, pedido, _online.ContainsKey);
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
}
