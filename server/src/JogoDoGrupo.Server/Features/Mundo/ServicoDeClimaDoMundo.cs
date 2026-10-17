namespace JogoDoGrupo.Server.Features.Mundo;

using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Serviço de segundo plano responsável por sincronizar o clima com os clientes conectados e
/// aplicar lentidão de movimento sob nevasca (issue #902).
/// </summary>
public sealed class ServicoDeClimaDoMundo : BackgroundService
{
    private readonly ClimaDoServidor _clima;
    private readonly ServidorTempoReal _tempoReal;
    private readonly Func<IReadOnlyList<AvatarNaCidade<SessaoTempoReal>>> _presentes;
    private readonly ILimiteDeVelocidadeDoMundo _limiteDeVelocidade;
    private readonly Func<long> _relogioMs;
    private readonly ILogger<ServicoDeClimaDoMundo>? _log;

    private readonly HashSet<long> _jogadoresSobNevasca = [];
    private readonly object _trava = new();
    private long _ultimaSincroniaMs;

    public ServicoDeClimaDoMundo(
        ClimaDoServidor clima,
        ServidorTempoReal tempoReal,
        Func<IReadOnlyList<AvatarNaCidade<SessaoTempoReal>>> presentes,
        ILimiteDeVelocidadeDoMundo limiteDeVelocidade,
        Func<long>? relogioMs = null,
        ILogger<ServicoDeClimaDoMundo>? log = null
    )
    {
        _clima = clima;
        _tempoReal = tempoReal;
        _presentes = presentes;
        _limiteDeVelocidade = limiteDeVelocidade;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(parada))
            {
                try
                {
                    Passo();
                }
                catch (Exception erro)
                {
                    _log?.LogError(erro, "Erro ao processar clima do mundo.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // O servidor está parando.
        }
    }

    /// <summary>
    /// Avalia o clima, sorteia novo se o período expirou, sincroniza com os clientes e aplica
    /// as regras de velocidade validada.
    /// </summary>
    public void Passo(long agoraMs = 0)
    {
        var agora = agoraMs > 0 ? agoraMs : _relogioMs();
        var mudou = _clima.Atualizar(agora);

        var intervaloMs = ConfigClima.IntervaloDeSincroniaSegundos * 1000L;
        if (mudou || agora - _ultimaSincroniaMs >= intervaloMs)
        {
            Sincronizar();
            _ultimaSincroniaMs = agora;
        }

        AplicarEfeitosDeMovimento();
    }

    /// <summary>
    /// Envia o clima atual a uma sessão específica (ex.: ao entrar na área).
    /// </summary>
    public void Enviar(SessaoTempoReal sessao) =>
        _tempoReal.Enviar(sessao, MundoMessageIds.ClimaDoMundo, _clima.Mensagem());

    /// <summary>
    /// Sincroniza o clima atual com todos os avatares presentes na área.
    /// </summary>
    public void Sincronizar()
    {
        var msg = _clima.Mensagem();
        foreach (var avatar in _presentes())
        {
            _tempoReal.Enviar(avatar.Sessao, MundoMessageIds.ClimaDoMundo, msg);
        }
    }

    /// <summary>
    /// Avalia os avatares presentes e aplica lentidão aos que estão sob nevasca no bioma de neve.
    /// </summary>
    public void AplicarEfeitosDeMovimento()
    {
        var lista = _presentes();
        var climaAtual = _clima.ClimaAtual;
        var presentesIds = new HashSet<long>(lista.Count);

        lock (_trava)
        {
            foreach (var avatar in lista)
            {
                presentesIds.Add(avatar.PersonagemId);
                var bioma = MapaDoMundo.BiomaEm(avatar.Posicao.X, avatar.Posicao.Z);
                var sobNevasca = ConfigClima.AfetaMovimento(climaAtual, bioma);

                if (sobNevasca)
                {
                    _jogadoresSobNevasca.Add(avatar.PersonagemId);
                    _limiteDeVelocidade.LimitarVelocidade(
                        avatar.PersonagemId,
                        ConfigClima.FatorVelocidadeNevasca,
                        2.0
                    );
                }
                else if (_jogadoresSobNevasca.Remove(avatar.PersonagemId))
                {
                    _limiteDeVelocidade.LimitarVelocidade(avatar.PersonagemId, 1.0, 1.0);
                }
            }

            _jogadoresSobNevasca.RemoveWhere(id => !presentesIds.Contains(id));
        }
    }

    /// <summary>
    /// Avaliação imediata ao mover o personagem (responsividade na travessia de bioma sob nevasca).
    /// </summary>
    public void VerificarPasso(long personagemId, float x, float z)
    {
        var climaAtual = _clima.ClimaAtual;
        var bioma = MapaDoMundo.BiomaEm(x, z);
        var sobNevasca = ConfigClima.AfetaMovimento(climaAtual, bioma);

        lock (_trava)
        {
            if (sobNevasca)
            {
                _jogadoresSobNevasca.Add(personagemId);
                _limiteDeVelocidade.LimitarVelocidade(
                    personagemId,
                    ConfigClima.FatorVelocidadeNevasca,
                    2.0
                );
            }
            else if (_jogadoresSobNevasca.Remove(personagemId))
            {
                _limiteDeVelocidade.LimitarVelocidade(personagemId, 1.0, 1.0);
            }
        }
    }

    /// <summary>
    /// Limpeza de estado de lentidão de clima ao desconectar.
    /// </summary>
    public void RemoverJogador(long personagemId)
    {
        lock (_trava)
        {
            _jogadoresSobNevasca.Remove(personagemId);
        }
    }
}
