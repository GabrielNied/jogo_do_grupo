namespace JogoDoGrupo.Server.Features.Mundo;

using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Aplica os perigos de terreno do mundo aberto aos jogadores (issue #901):
/// dano por segundo na lava (via contrato de Combate) e redução de velocidade
/// na água do pântano (via ILimiteDeVelocidadeDoMundo).
/// Frio e calor não têm efeito.
/// </summary>
public sealed class ServicoDePerigosDoMundo : BackgroundService
{
    private readonly object _trava = new();
    private readonly Func<IReadOnlyList<AvatarNaCidade<SessaoTempoReal>>> _presentes;
    private readonly ILimiteDeVelocidadeDoMundo _limiteDeVelocidade;
    private readonly IDanoAoPersonagem? _danoAoPersonagem;
    private readonly EventosDoServidor? _eventos;
    private readonly Func<long> _relogioMs;
    private readonly ILogger<ServicoDePerigosDoMundo>? _log;

    private readonly Dictionary<long, long> _ultimoDanoLavaMs = [];
    private readonly HashSet<long> _jogadoresNoPantano = [];

    public ServicoDePerigosDoMundo(
        Func<IReadOnlyList<AvatarNaCidade<SessaoTempoReal>>> presentes,
        ILimiteDeVelocidadeDoMundo limiteDeVelocidade,
        IDanoAoPersonagem? danoAoPersonagem = null,
        EventosDoServidor? eventos = null,
        Func<long>? relogioMs = null,
        ILogger<ServicoDePerigosDoMundo>? log = null
    )
    {
        _presentes = presentes;
        _limiteDeVelocidade = limiteDeVelocidade;
        _danoAoPersonagem = danoAoPersonagem;
        _eventos = eventos;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
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
                    _log?.LogError(erro, "Erro ao processar perigos do mundo.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // O servidor está parando.
        }
    }

    /// <summary>
    /// Avalia a posição de todos os presentes na cidade/mundo e aplica perigos.
    /// O laço em segundo plano chama periodicamente; os testes chamam diretamente.
    /// </summary>
    public void Passo(long agoraMs = 0)
    {
        var agora = agoraMs > 0 ? agoraMs : _relogioMs();
        var presentes = _presentes();
        var presentesIds = new HashSet<long>(presentes.Count);

        lock (_trava)
        {
            foreach (var avatar in presentes)
            {
                presentesIds.Add(avatar.PersonagemId);
                var (x, z) = (avatar.Posicao.X, avatar.Posicao.Z);

                // 1. Pântano (lentidão)
                var noPantano = ConfigPerigosDoMundo.EstaNoPantano(x, z);
                if (noPantano)
                {
                    _jogadoresNoPantano.Add(avatar.PersonagemId);
                    _limiteDeVelocidade.LimitarVelocidade(
                        avatar.PersonagemId,
                        ConfigPerigosDoMundo.FatorVelocidadePantano,
                        ConfigPerigosDoMundo.DuracaoTetoPantanoSegundos
                    );
                }
                else if (_jogadoresNoPantano.Remove(avatar.PersonagemId))
                {
                    _limiteDeVelocidade.LimitarVelocidade(avatar.PersonagemId, 1.0, 1.0);
                }

                // 2. Lava (dano por segundo)
                var naLava = ConfigPerigosDoMundo.EstaNaLava(x, z);
                if (naLava)
                {
                    if (
                        !_ultimoDanoLavaMs.TryGetValue(avatar.PersonagemId, out var ultimoDano)
                        || agora - ultimoDano >= ConfigPerigosDoMundo.IntervaloDanoLavaMs
                    )
                    {
                        _ultimoDanoLavaMs[avatar.PersonagemId] = agora;
                        _danoAoPersonagem?.AplicarDanoDeAmbiente(
                            avatar.PersonagemId,
                            ConfigPerigosDoMundo.DanoPorSegundoLava,
                            "Lava",
                            AreasDoMundo.Cidade
                        );
                        _eventos?.Publicar(
                            new DanoDeAmbienteAcertou(
                                avatar.PersonagemId,
                                ConfigPerigosDoMundo.DanoPorSegundoLava,
                                "Lava",
                                AreasDoMundo.Cidade
                            )
                        );
                    }
                }
                else
                {
                    _ultimoDanoLavaMs.Remove(avatar.PersonagemId);
                }
            }

            // Limpa jogadores que saíram da área ou desconectaram
            var desconectadosLava = new List<long>();
            foreach (var id in _ultimoDanoLavaMs.Keys)
            {
                if (!presentesIds.Contains(id))
                {
                    desconectadosLava.Add(id);
                }
            }
            foreach (var id in desconectadosLava)
            {
                _ultimoDanoLavaMs.Remove(id);
            }

            _jogadoresNoPantano.RemoveWhere(id => !presentesIds.Contains(id));
        }
    }

    /// <summary>
    /// Verifica imediatamente a transição de pântano no passo de movimento do jogador.
    /// </summary>
    public void VerificarPasso(long personagemId, float x, float z)
    {
        lock (_trava)
        {
            var noPantano = ConfigPerigosDoMundo.EstaNoPantano(x, z);
            if (noPantano)
            {
                _jogadoresNoPantano.Add(personagemId);
                _limiteDeVelocidade.LimitarVelocidade(
                    personagemId,
                    ConfigPerigosDoMundo.FatorVelocidadePantano,
                    ConfigPerigosDoMundo.DuracaoTetoPantanoSegundos
                );
            }
            else if (_jogadoresNoPantano.Remove(personagemId))
            {
                _limiteDeVelocidade.LimitarVelocidade(personagemId, 1.0, 1.0);
            }
        }
    }

    /// <summary>Remove o rastreamento do jogador quando sua sessão é encerrada.</summary>
    public void RemoverJogador(long personagemId)
    {
        lock (_trava)
        {
            _ultimoDanoLavaMs.Remove(personagemId);
            _jogadoresNoPantano.Remove(personagemId);
        }
    }
}
