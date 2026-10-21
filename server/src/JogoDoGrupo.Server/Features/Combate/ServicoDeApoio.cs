using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// O apoio de grupo de Combate (#170): aplica escudo e buffs a pedido de Skills
/// (<see cref="IApoioDeCombate"/>), avisa o dono e a área quando o escudo muda e tira os escudos
/// que expiram. Quem decide quem recebe é quem pede; aqui só quem está caído fica de fora.
/// </summary>
public sealed class ServicoDeApoio : IHostedService, IApoioDeCombate, IDisposable
{
    private const int IntervaloDoTickMs = 200;

    private readonly GerenciadorDeEscudos _escudos;
    private readonly GerenciadorDeEfeitosDeStatus _efeitos;
    private readonly ServidorTempoReal _tempoReal;
    private readonly IServiceScopeFactory _escopos;
    private readonly ILogger<ServicoDeApoio> _log;
    private readonly IPosicoesDoMundo? _posicoes;
    private readonly AvisosNaArea? _avisos;
    private readonly ServicoDeQueda? _queda;
    private readonly CancellationTokenSource _parada = new();
    private Task _laco = Task.CompletedTask;

    public ServicoDeApoio(
        GerenciadorDeEscudos escudos,
        GerenciadorDeEfeitosDeStatus efeitos,
        ServidorTempoReal tempoReal,
        IServiceScopeFactory escopos,
        ILogger<ServicoDeApoio> log,
        IPosicoesDoMundo? posicoes = null,
        ServicoDeQueda? queda = null,
        IVizinhanca? vizinhanca = null
    )
    {
        _escudos = escudos;
        _efeitos = efeitos;
        _tempoReal = tempoReal;
        _escopos = escopos;
        _log = log;
        _posicoes = posicoes;
        _avisos = posicoes != null ? new AvisosNaArea(tempoReal, posicoes, vizinhanca) : null;
        _queda = queda;

        _escudos.Mudou += AoMudar;
        _tempoReal.SessaoAutenticada += AoAutenticar;
        _tempoReal.SessaoEncerrada += AoEncerrar;
    }

    public Task StartAsync(CancellationToken cancelamento)
    {
        _laco = Task.Run(ExecutarLacoAsync, CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancelamento)
    {
        try
        {
            _parada.Cancel();
        }
        catch (ObjectDisposedException) { }

        try
        {
            await _laco;
        }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        _escudos.Mudou -= AoMudar;
        _tempoReal.SessaoAutenticada -= AoAutenticar;
        _tempoReal.SessaoEncerrada -= AoEncerrar;
        _parada.Dispose();
    }

    /// <inheritdoc />
    public async Task<bool> AplicarEscudoAsync(
        long personagemId,
        double fracaoDaVidaMaxima,
        double duracaoSegundos,
        CancellationToken cancelamento
    )
    {
        if (_queda?.EstaCaido(personagemId) == true)
        {
            return false;
        }

        // A vida máxima vem de quem é dono da vida; não muda nada, só lê.
        var recursos = await RecursosDoAtaque
            .Recursos(_escopos)
            .AtualizarAsync(personagemId, (_, _) => { }, cancelamento);
        var absorcao = RegrasEscudo.Absorcao(recursos.VidaMaxima, fracaoDaVidaMaxima);
        if (absorcao <= 0)
        {
            return false;
        }

        // Quem caiu durante a leitura da vida não recebe: confere de novo antes de aplicar.
        if (_queda?.EstaCaido(personagemId) == true)
        {
            return false;
        }

        _escudos.Aplicar(personagemId, absorcao, duracaoSegundos);
        return true;
    }

    /// <inheritdoc />
    public bool AplicarBuff(long personagemId, TipoEfeitoDeStatus tipo, double duracaoSegundos)
    {
        // Só os buffs de grupo passam por aqui; debuff e DoT não são apoio.
        var ehBuff = tipo == TipoEfeitoDeStatus.Fortalecido || tipo == TipoEfeitoDeStatus.Protegido;
        if (!ehBuff || _queda?.EstaCaido(personagemId) == true)
        {
            return false;
        }

        _efeitos.Aplicar(personagemId, tipo, duracaoSegundos);
        return true;
    }

    private void AoAutenticar(SessaoTempoReal sessao)
    {
        if (_escudos.ObterRetrato(sessao.PersonagemId, out var retrato))
        {
            _tempoReal.Enviar(sessao, CombateMessageIds.EscudoMudou, retrato);
        }
    }

    private void AoEncerrar(SessaoTempoReal sessao) => _escudos.Limpar(sessao.PersonagemId);

    private void AoMudar(long entidadeId, EscudoMudou aviso)
    {
        if (_tempoReal.TentarObterSessao(entidadeId, out var sessao) && sessao != null)
        {
            _tempoReal.Enviar(sessao, CombateMessageIds.EscudoMudou, aviso);
        }

        if (_posicoes?.TentarObter(entidadeId, out var pos) == true && _avisos != null)
        {
            _avisos.Enviar(
                pos.Area,
                entidadeId,
                CombateMessageIds.EscudoMudou,
                aviso,
                incluirDono: false
            );
        }
    }

    private async Task ExecutarLacoAsync()
    {
        while (!_parada.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(IntervaloDoTickMs, _parada.Token);
                _escudos.Atualizar();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Erro no laço de escudos.");
            }
        }
    }
}
